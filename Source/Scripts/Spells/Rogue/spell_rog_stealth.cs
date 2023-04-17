// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[Script] // 1784 - Stealth
internal class SpellRogStealth : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        // Master of Subtlety
        if (target.HasAura(RogueSpells.MasterOfSubtletyPassive))
            target.SpellFactory.CastSpell(target, RogueSpells.MasterOfSubtletyDamagePercent, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

        // Shadow Focus
        if (target.HasAura(RogueSpells.ShadowFocus))
            target.SpellFactory.CastSpell(target, RogueSpells.ShadowFocusEffect, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

        // Premeditation
        if (target.HasAura(RogueSpells.PremeditationPassive))
            target.SpellFactory.CastSpell(target, RogueSpells.PremeditationAura, true);

        target.SpellFactory.CastSpell(target, RogueSpells.Sanctuary, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        target.SpellFactory.CastSpell(target, RogueSpells.StealthStealthAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        target.SpellFactory.CastSpell(target, RogueSpells.StealthShapeshiftAura, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }

    private void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        // Master of Subtlety
        var masterOfSubtletyPassive = Target.GetAuraEffect(RogueSpells.MasterOfSubtletyPassive, 0);

        if (masterOfSubtletyPassive != null)
        {
            var masterOfSubtletyAura = Target.GetAura(RogueSpells.MasterOfSubtletyDamagePercent);

            if (masterOfSubtletyAura != null)
            {
                masterOfSubtletyAura.SetMaxDuration(masterOfSubtletyPassive.Amount);
                masterOfSubtletyAura.RefreshDuration();
            }
        }

        // Premeditation
        target.RemoveAura(RogueSpells.PremeditationAura);

        target.RemoveAura(RogueSpells.ShadowFocusEffect);
        target.RemoveAura(RogueSpells.StealthStealthAura);
        target.RemoveAura(RogueSpells.StealthShapeshiftAura);
    }
}