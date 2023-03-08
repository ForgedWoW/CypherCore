// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CALL_OF_YSERA_AURA)]
public class aura_evoker_charged_blast : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo info)
	{
		return info.ProcSpell.SpellInfo.Id.EqualsAny(EvokerSpells.AZURE_STRIKE,
													EvokerSpells.DISINTEGRATE,
													EvokerSpells.ETERNITY_SURGE_CHARGED,
													EvokerSpells.SHATTERING_STAR);
	}
}