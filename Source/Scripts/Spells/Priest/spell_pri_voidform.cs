// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(194249)]
public class SpellPriVoidform : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.AddPctModifier));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
    }

    private void HandleApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
            caster.RemoveAura(PriestSpells.LINGERING_INSANITY);
    }

    private void HandlePeriodic(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster == null)
            return;

        // This spell must end when insanity hit 0
        if (caster.GetPower(PowerType.Insanity) == 0)
        {
            caster.RemoveAura(aurEff.Base);

            return;
        }

        var tick = Aura.StackAmount - 1;

        switch (tick)
        {
            case 0:
                caster.SpellFactory.CastSpell(caster, PriestSpells.VOIDFORM_TENTACLES, true);

                break;
            case 3:
                caster.SpellFactory.CastSpell(caster, PriestSpells.VOIDFORM_TENTACLES + 1, true);

                break;
            case 6:
                caster.SpellFactory.CastSpell(caster, PriestSpells.VOIDFORM_TENTACLES + 2, true);

                break;
            case 9:
                caster.SpellFactory.CastSpell(caster, PriestSpells.VOIDFORM_TENTACLES + 3, true);

                break;
        }

        caster.SpellFactory.CastSpell(caster, PriestSpells.VOIDFORM_BUFFS, true);
    }

    private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes unnamedParameter)
    {
        var caster = Caster;

        if (caster == null)
            return;

        for (uint i = 0; i < 4; ++i)
            caster.RemoveAura(PriestSpells.VOIDFORM_TENTACLES + i);

        var haste = aurEff.Amount;
        var mod = new CastSpellExtraArgs();
        mod.AddSpellMod(SpellValueMod.BasePoint0, haste);

        var aEff = caster.GetAuraEffectOfRankedSpell(PriestSpells.VOIDFORM_BUFFS, 3, caster.GUID);

        if (aEff != null)
            mod.AddSpellMod(SpellValueMod.BasePoint1, aEff.Amount);

        mod.TriggerFlags = TriggerCastFlags.FullMask;
        caster.SpellFactory.CastSpell(caster, PriestSpells.LINGERING_INSANITY, mod);
    }
}