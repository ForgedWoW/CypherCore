﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;
using Game.Entities;

namespace Scripts.Spells.DemonHunter;

public class event_dh_infernal_strike : BasicEvent
{
	private readonly Unit _caster;

	public event_dh_infernal_strike(Unit caster)
	{
		_caster = caster;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		if (_caster != null)
		{
			_caster.CastSpell(_caster, DemonHunterSpells.INFERNAL_STRIKE_DAMAGE, true);

			if (_caster.HasAura(DemonHunterSpells.RAIN_OF_CHAOS))
				_caster.CastSpell(_caster, DemonHunterSpells.RAIN_OF_CHAOS_SLOW, true);

			if (_caster.HasAura(DemonHunterSpells.ABYSSAL_STRIKE))
				_caster.CastSpell(_caster, DemonHunterSpells.SIGIL_OF_FLAME_NO_DEST, true);
		}

		return true;
	}
}