// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(116680)]
public class BfaSpellFocusedThunderTalentThunderFocusTea : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.AddFlatModifier, AuraEffectHandleModes.Real));
    }

    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        Unit caster = Caster.AsPlayer;

        if (caster == null)
            return;

        if (caster.HasAura(MonkSpells.FOCUSED_THUNDER_TALENT))
        {
            var thunder = caster.GetAura(MonkSpells.THUNDER_FOCUS_TEA);

            if (thunder != null)
                thunder.SetStackAmount(2);
        }
    }
}