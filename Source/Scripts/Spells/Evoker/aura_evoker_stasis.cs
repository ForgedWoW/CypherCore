// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using System;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.STASIS)]
internal class aura_evoker_stasis : AuraScript, IAuraCheckProc, IAuraOnProc, IAuraScriptValues
{
    public Dictionary<string, object> ScriptValues { get; } = new();
    
    public bool CheckProc(ProcEventInfo info)
    {
        return info.HealInfo != null && info.ProcSpell != null
                && info.SpellInfo.Id.EqualsAny(EvokerSpells.ECHO, 
                                                                    EvokerSpells.RED_LIVING_FLAME_HEAL,
                                                                    EvokerSpells.GREEN_DREAM_BREATH_CHARGED,
                                                                    EvokerSpells.SPIRITBLOOM_CHARGED,
                                                                    EvokerSpells.BRONZE_REVERSION,
                                                                    EvokerSpells.GREEN_VERDANT_EMBRACE_HEAL,
                                                                    EvokerSpells.GREEN_NATURALIZE,
                                                                    EvokerSpells.RED_CAUTERIZING_FLAME);
    }

    public void OnProc(ProcEventInfo info)
    {
        if (ScriptValues.Count == 0)
            Caster.AddAura(EvokerSpells.STASIS_OVERRIDE_AURA);

        List<HealInfo> heals = new List<HealInfo>();

        string id = info.ProcSpell.CastId.ToString();

        if (ScriptValues.TryGetValue(id, out var healsObj))
        {
            heals = (List<HealInfo>)healsObj;
            heals.Add(info.HealInfo);
            ScriptValues[id] = heals;
        }
        else if (ScriptValues.Count < 3)
        {
            heals.Add(info.HealInfo);
            ScriptValues[id] = heals;
        }
    }
}