// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

// 1066 - Aquatic Form
// 33943 - Flight Form
// 40120 - Swift Flight Form
[Script] // 165961 - Stag Form
internal class SpellDruTravelFormAuraScript : AuraScript, IHasAuraEffects
{
    private uint _triggeredSpellId;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    public static uint GetFormSpellId(Player player, Difficulty difficulty, bool requiresOutdoor)
    {
        // Check what form is appropriate
        if (player.HasSpell(DruidSpellIds.FormAquaticPassive) &&
            player.IsInWater) // Aquatic form
            return DruidSpellIds.FormAquatic;

        if (!player.IsInCombat &&
            player.GetSkillValue(SkillType.Riding) >= 225 &&
            CheckLocationForForm(player, difficulty, requiresOutdoor, DruidSpellIds.FormFlight) == SpellCastResult.SpellCastOk) // Flight form
            return player.GetSkillValue(SkillType.Riding) >= 300 ? DruidSpellIds.FormSwiftFlight : DruidSpellIds.FormFlight;

        if (!player.IsInWater &&
            CheckLocationForForm(player, difficulty, requiresOutdoor, DruidSpellIds.FormStag) == SpellCastResult.SpellCastOk) // Stag form
            return DruidSpellIds.FormStag;

        return 0;
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // If it stays 0, it removes Travel Form dummy in AfterRemove.
        _triggeredSpellId = 0;

        // We should only handle aura interrupts.
        if (TargetApplication.RemoveMode != AuraRemoveMode.Interrupt)
            return;

        // Check what form is appropriate
        _triggeredSpellId = GetFormSpellId(Target.AsPlayer, CastDifficulty, true);

        // If chosen form is current aura, just don't remove it.
        if (_triggeredSpellId == ScriptSpellId)
            PreventDefaultAction();
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (_triggeredSpellId == ScriptSpellId)
            return;

        var player = Target.AsPlayer;

        if (_triggeredSpellId != 0) // Apply new form
            player.SpellFactory.CastSpell(player, _triggeredSpellId, new CastSpellExtraArgs(aurEff));
        else // If not set, simply remove Travel Form dummy
            player.RemoveAura(DruidSpellIds.TravelForm);
    }

    private static SpellCastResult CheckLocationForForm(Player targetPlayer, Difficulty difficulty, bool requireOutdoors, uint spellID)
    {
        var spellInfo = Global.SpellMgr.GetSpellInfo(spellID, difficulty);

        if (requireOutdoors && !targetPlayer.IsOutdoors)
            return SpellCastResult.OnlyOutdoors;

        return spellInfo.CheckLocation(targetPlayer.Location.MapId, targetPlayer.Zone, targetPlayer.Area, targetPlayer);
    }
}