// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(14769)]
public class SpellPriImprovedPowerWordShield : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcSpellModHandler(HandleEffectCalcSpellMod, 0, AuraType.Dummy));
    }

    private void HandleEffectCalcSpellMod(AuraEffect aurEff, SpellModifier spellMod)
    {
        if (spellMod == null)
        {
            var mod = new SpellModifierByClassMask(Aura);
            spellMod.Op = (SpellModOp)aurEff.MiscValue;
            spellMod.Type = SpellModType.Pct;
            spellMod.SpellId = Id;
            mod.Mask = aurEff.GetSpellEffectInfo().SpellClassMask;
        }

        ((SpellModifierByClassMask)spellMod).Value = aurEff.Amount;
    }
}