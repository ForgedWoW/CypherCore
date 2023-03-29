// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// 199471 - Soul Flame
[SpellScript(199471)]
public class spell_warlock_artifact_soul_flame : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
    {
        var target = eventInfo.ActionTarget;
        var caster = Caster;

        if (caster == null || target == null)
            return;

        var p = target.Location;
        caster.Events.AddEvent(() => { caster.CastSpell(p, WarlockSpells.SOUL_FLAME_PROC, true); }, TimeSpan.FromMilliseconds(300));
    }
}