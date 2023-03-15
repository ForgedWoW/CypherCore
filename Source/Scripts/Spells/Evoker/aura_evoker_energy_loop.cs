// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ENERGY_LOOP)]
public class aura_evoker_energy_loop : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo info)
	{
		return info.ProcSpell.SpellInfo.Id.EqualsAny(EvokerSpells.BLUE_DISINTEGRATE, EvokerSpells.BLUE_DISINTEGRATE_2);
	}
}