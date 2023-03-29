// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CHARGED_BLAST)]
public class aura_evoker_charged_blast : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        return info.SpellInfo.Labels.Contains(EvokerLabels.BLUE) && info.DamageInfo != null && info.DamageInfo.Damage > 0;
    }
}