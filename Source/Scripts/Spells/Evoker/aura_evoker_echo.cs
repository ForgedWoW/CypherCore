// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.ECHO_AURA)]
public class aura_evoker_echo : AuraScript, IAuraOnProc
{
	public void OnProc(ProcEventInfo info)
	{
		if (info.SpellInfo.Id != EvokerSpells.ECHO
            && Caster.TryGetAura(EvokerSpells.ECHO, out var echoAura))
		{
			var healInfo = info.HealInfo;
			if (healInfo == null)
				return;

			HealInfo newHeal = new(healInfo.GetHealer(), 
									healInfo.GetTarget(),
									healInfo.GetHeal() * (echoAura.SpellInfo.GetEffect(1).BasePoints * 0.01), 
									healInfo.GetSpellInfo(),
									healInfo.GetSchoolMask());

			Unit.DealHeal(healInfo);
			echoAura.Remove();
		}
	}
}