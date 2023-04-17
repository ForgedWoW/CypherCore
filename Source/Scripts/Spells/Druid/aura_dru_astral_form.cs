// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(24858, 102560, 197625)]
public class AuraDruAstralForm : AuraScript, IHasAuraEffects
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

    private void AfterApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var target = Target;

        if (target.HasAura(DruidSpells.GlyphOfStars))
        {
            target.SetDisplayId(target.NativeDisplayId);
            target.AddAura(DruidSpells.BlueColor, target);
            target.AddAura(DruidSpells.ShadowyGhost, target);
            target.SpellFactory.CastSpell(target, (uint)Global.SpellMgr.GetSpellInfo(DruidSpells.GlyphOfStars, Difficulty.None).GetEffect(0).BasePoints, true);
        }
    }

    private void AfterRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var target = Target;

        if (target.HasAura(ShapeshiftFormSpells.MoonkinForm) || target.HasAura(DruidSpells.ChosenOfElune))
            return;

        target.RemoveAura((uint)Global.SpellMgr.GetSpellInfo(DruidSpells.GlyphOfStars, Difficulty.None).GetEffect(0).BasePoints);
        target.RemoveAura(DruidSpells.BlueColor);
        target.RemoveAura(DruidSpells.ShadowyGhost);
    }
}