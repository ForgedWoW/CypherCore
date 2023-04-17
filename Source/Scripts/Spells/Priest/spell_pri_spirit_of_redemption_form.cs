// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[SpellScript(27827)]
public class SpellPriSpiritOfRedemptionForm : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.WaterBreathing, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void AfterRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var lTarget = Target;

        lTarget.RemoveAura(ESpells.SPIRIT_OF_REDEMPTION_FORM);
        lTarget.RemoveAura(ESpells.SPIRIT_OF_REDEMPTION_IMMUNITY);
    }

    private struct ESpells
    {
        public const uint SPIRIT_OF_REDEMPTION_IMMUNITY = 62371;
        public const uint SPIRIT_OF_REDEMPTION_FORM = 27795;
    }
}