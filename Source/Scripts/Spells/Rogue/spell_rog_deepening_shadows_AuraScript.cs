// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(185314)]
public class spell_rog_deepening_shadows_AuraScript : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    private int _cp;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var caster = Caster;

        if (caster != null)
        {
            var maxcp = caster.HasAura(RogueSpells.DEEPER_STRATAGEM) ? 6 : 5;
            _cp = Math.Min(caster.GetPower(PowerType.ComboPoints) + 1, maxcp);
        }

        if (eventInfo.SpellInfo.Id == 196819)
            return true;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (Caster.HasAura(RogueSpells.DEEPENING_SHADOWS))
            Caster.SpellHistory.ModifyCooldown(RogueSpells.SHADOW_DANCE, TimeSpan.FromMilliseconds(_cp * -3000));
    }
}