// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.Entities;
using Game.Movement;
using Game.Scripting;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 110744 - Divine Star
internal class areatrigger_pri_divine_star : AreaTriggerAI
{
	private readonly List<ObjectGuid> _affectedUnits = new();
	private readonly TaskScheduler Scheduler = new();
	private Position _casterCurrentPosition = new();

	public areatrigger_pri_divine_star(AreaTrigger areatrigger) : base(areatrigger) { }

	public override void OnInitialize()
	{
		var caster = At.GetCaster();

		if (caster != null)
		{
			_casterCurrentPosition = caster.Location;

			// Note: max. distance at which the Divine Star can travel to is 24 yards.
			var divineStarXOffSet = 24.0f;

			var destPos = _casterCurrentPosition;
			At.MovePositionToFirstCollision(destPos, divineStarXOffSet, 0.0f);

			PathGenerator firstPath = new(At);
			firstPath.CalculatePath(destPos, false);

			var endPoint = firstPath.GetPath().Last();

			// Note: it takes 1000ms to reach 24 yards, so it takes 41.67ms to run 1 yard.
			At.InitSplines(firstPath.GetPath().ToList(), (uint)(At.GetDistance(endPoint.X, endPoint.Y, endPoint.Z) * 41.67f));
		}
	}

	public override void OnUpdate(uint diff)
	{
		Scheduler.Update(diff);
	}

	public override void OnUnitEnter(Unit unit)
	{
		var caster = At.GetCaster();

		if (caster != null)
			if (!_affectedUnits.Contains(unit.GUID))
			{
				if (caster.IsValidAttackTarget(unit))
					caster.CastSpell(unit, PriestSpells.DIVINE_STAR_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
				else if (caster.IsValidAssistTarget(unit))
					caster.CastSpell(unit, PriestSpells.DIVINE_STAR_HEAL, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));

				_affectedUnits.Add(unit.GUID);
			}
	}

	public override void OnUnitExit(Unit unit)
	{
		// Note: this ensures any unit receives a second hit if they happen to be inside the AT when Divine Star starts its return path.
		var caster = At.GetCaster();

		if (caster != null)
			if (!_affectedUnits.Contains(unit.GUID))
			{
				if (caster.IsValidAttackTarget(unit))
					caster.CastSpell(unit, PriestSpells.DIVINE_STAR_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));
				else if (caster.IsValidAssistTarget(unit))
					caster.CastSpell(unit, PriestSpells.DIVINE_STAR_HEAL, new CastSpellExtraArgs(TriggerCastFlags.IgnoreGCD | TriggerCastFlags.IgnoreCastInProgress));

				_affectedUnits.Add(unit.GUID);
			}
	}

	public override void OnDestinationReached()
	{
		var caster = At.GetCaster();

		if (caster == null)
			return;

		if (At.GetDistance(_casterCurrentPosition) > 0.05f)
		{
			_affectedUnits.Clear();

			ReturnToCaster();
		}
		else
		{
			At.Remove();
		}
	}

	private void ReturnToCaster()
	{
		Scheduler.Schedule(TimeSpan.FromMilliseconds(0),
							task =>
							{
								var caster = At.GetCaster();

								if (caster != null)
								{
									_casterCurrentPosition = caster.Location;

									List<Vector3> returnSplinePoints = new();

									returnSplinePoints.Add(At.Location);
									returnSplinePoints.Add(At.Location);
									returnSplinePoints.Add(caster.Location);
									returnSplinePoints.Add(caster.Location);

									At.InitSplines(returnSplinePoints, (uint)At.GetDistance(caster) / 24 * 1000);

									task.Repeat(TimeSpan.FromMilliseconds(250));
								}
							});
	}
}