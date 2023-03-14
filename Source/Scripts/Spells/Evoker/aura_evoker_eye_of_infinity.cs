// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.EYE_OF_INFINITY)]
public class aura_evoker_eye_of_infinity : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo info)
	{
		return info.ProcSpell.SpellInfo.Id == EvokerSpells.ETERNITY_SURGE_CHARGED &&
				info.DamageInfo != null &&
				info.DamageInfo.IsCritical;
	}
}