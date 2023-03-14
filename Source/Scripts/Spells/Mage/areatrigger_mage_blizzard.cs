// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 4658 - AreaTrigger Create Properties
internal class areatrigger_mage_blizzard : AreaTriggerScript, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
	private TimeSpan _tickTimer;

	public void OnCreate()
	{
		_tickTimer = TimeSpan.FromMilliseconds(1000);
	}

	public void OnUpdate(uint diff)
	{
		_tickTimer -= TimeSpan.FromMilliseconds(diff);

		while (_tickTimer <= TimeSpan.Zero)
		{
			var caster = At.GetCaster();

			caster?.CastSpell(At.Location, MageSpells.BlizzardDamage, new CastSpellExtraArgs());

			_tickTimer += TimeSpan.FromMilliseconds(1000);
		}
	}
}