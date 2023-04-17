// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[Script]
internal class SpellDruTravelFormDummyAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var player = Target.AsPlayer;

        // Outdoor check already passed - Travel Form (dummy) has ATTR0_OUTDOORS_ONLY attribute.
        var triggeredSpellId = SpellDruTravelFormAuraScript.GetFormSpellId(player, CastDifficulty, false);

        player.SpellFactory.CastSpell(player, triggeredSpellId, new CastSpellExtraArgs(aurEff));
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // No need to check remove mode, it's safe for Auras to remove each other in AfterRemove hook.
        Target.RemoveAura(DruidSpellIds.FormStag);
        Target.RemoveAura(DruidSpellIds.FormAquatic);
        Target.RemoveAura(DruidSpellIds.FormFlight);
        Target.RemoveAura(DruidSpellIds.FormSwiftFlight);
    }
}