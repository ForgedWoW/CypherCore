// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Framework.Dynamic;

namespace Forged.MapServer.Spells;

public class SpellEvent : BasicEvent
{
    private readonly Spell _spell;

	public override bool IsDeletable => _spell.IsDeletable;

	public Spell Spell => _spell;

	public SpellEvent(Spell spell)
	{
		_spell = spell;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		// update spell if it is not finished
		if (_spell.State != SpellState.Finished)
			_spell.Update(pTime);

		// check spell state to process
		switch (_spell.State)
		{
			case SpellState.Finished:
			{
				// spell was finished, check deletable state
				if (_spell.IsDeletable)
					// check, if we do have unfinished triggered spells
					return true; // spell is deletable, finish event

				// event will be re-added automatically at the end of routine)
				break;
			}
			case SpellState.Delayed:
			{
				// first, check, if we have just started
				if (_spell.DelayStart != 0)
				{
					// run the spell handler and think about what we can do next
					var tOffset = etime - _spell.DelayStart;
					var nOffset = _spell.HandleDelayed(tOffset);

					if (nOffset != 0)
					{
						// re-add us to the queue
						_spell.Caster.Events.AddEvent(this, TimeSpan.FromMilliseconds(_spell.DelayStart + nOffset), false);

						return false; // event not complete
					}
					// event complete
					// finish update event will be re-added automatically at the end of routine)
				}
				else
				{
					// delaying had just started, record the moment
					_spell.DelayStart = etime;
					// handle effects on caster if the spell has travel time but also affects the caster in some way
					var nOffset = _spell.HandleDelayed(0);

					// re-plan the event for the delay moment
					_spell.Caster.Events.AddEvent(this, TimeSpan.FromMilliseconds(etime + nOffset), false);

					return false; // event not complete
				}

				break;
			}
		}

		// spell processing not complete, plan event on the next update interval
		_spell.Caster.Events.AddEvent(this, TimeSpan.FromMilliseconds(etime + 1), false);

		return false; // event not complete
	}

	public override void Abort(ulong eTime)
	{
		// oops, the spell we try to do is aborted
		if (_spell.State != SpellState.Finished)
			_spell.Cancel();
	}
}