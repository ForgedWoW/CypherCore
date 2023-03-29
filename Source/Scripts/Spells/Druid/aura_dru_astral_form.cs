﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(24858, 102560, 197625)]
public class aura_dru_astral_form : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        switch (ScriptSpellId)
        {
            case 197625:
            case 24858:
                AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
                AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 1, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));

                break;
            case 102560:
                AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 1, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
                AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 1, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));

                break;
        }
    }

    private void AfterApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
    {
        var target = Target;

        if (target.HasAura(DruidSpells.GLYPH_OF_STARS))
        {
            target.SetDisplayId(target.NativeDisplayId);
            target.AddAura(DruidSpells.BLUE_COLOR, target);
            target.AddAura(DruidSpells.SHADOWY_GHOST, target);
            target.CastSpell(target, (uint)Global.SpellMgr.GetSpellInfo(DruidSpells.GLYPH_OF_STARS, Difficulty.None).GetEffect(0).BasePoints, true);
        }
    }

    private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
    {
        var target = Target;

        if (target.HasAura(ShapeshiftFormSpells.MOONKIN_FORM) || target.HasAura(DruidSpells.CHOSEN_OF_ELUNE))
            return;

        target.RemoveAura((uint)Global.SpellMgr.GetSpellInfo(DruidSpells.GLYPH_OF_STARS, Difficulty.None).GetEffect(0).BasePoints);
        target.RemoveAura(DruidSpells.BLUE_COLOR);
        target.RemoveAura(DruidSpells.SHADOWY_GHOST);
    }
}