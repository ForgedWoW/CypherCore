// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.m_Events.Midsummer;

internal struct SpellIds
{
    //Brazierhit
    public const uint TORCH_TOSSING_TRAINING = 45716;
    public const uint TORCH_TOSSING_PRACTICE = 46630;
    public const uint TORCH_TOSSING_TRAINING_SUCCESS_ALLIANCE = 45719;
    public const uint TORCH_TOSSING_TRAINING_SUCCESS_HORDE = 46651;
    public const uint TARGET_INDICATOR_COSMETIC = 46901;
    public const uint TARGET_INDICATOR = 45723;
    public const uint BRAZIERS_HIT = 45724;

    //RibbonPoleData
    public const uint HAS_FULL_MIDSUMMER_SET = 58933;
    public const uint BURNING_HOT_POLE_DANCE = 58934;
    public const uint RIBBON_POLE_PERIODIC_VISUAL = 45406;
    public const uint RIBBON_DANCE = 29175;
    public const uint TEST_RIBBON_POLE1 = 29705;
    public const uint TEST_RIBBON_POLE2 = 29726;
    public const uint TEST_RIBBON_POLE3 = 29727;

    //Jugglingtorch
    public const uint JUGGLE_TORCH_SLOW = 45792;
    public const uint JUGGLE_TORCH_MEDIUM = 45806;
    public const uint JUGGLE_TORCH_FAST = 45816;
    public const uint JUGGLE_TORCH_SELF = 45638;
    public const uint JUGGLE_TORCH_SHADOW_SLOW = 46120;
    public const uint JUGGLE_TORCH_SHADOW_MEDIUM = 46118;
    public const uint JUGGLE_TORCH_SHADOW_FAST = 46117;
    public const uint JUGGLE_TORCH_SHADOW_SELF = 46121;
    public const uint GIVE_TORCH = 45280;

    //Flingtorch
    public const uint FLING_TORCH_TRIGGERED = 45669;
    public const uint FLING_TORCH_SHADOW = 46105;
    public const uint JUGGLE_TORCH_MISSED = 45676;
    public const uint TORCHES_CAUGHT = 45693;
    public const uint TORCH_CATCHING_SUCCESS_ALLIANCE = 46081;
    public const uint TORCH_CATCHING_SUCCESS_HORDE = 46654;
    public const uint TORCH_CATCHING_REMOVE_TORCHES = 46084;
}

internal struct QuestIds
{
    //JugglingTorch
    public const uint TORCH_CATCHING_A = 11657;
    public const uint TORCH_CATCHING_H = 11923;
    public const uint MORE_TORCH_CATCHING_A = 11924;
    public const uint MORE_TORCH_CATCHING_H = 11925;
}

[Script] // 45724 - Braziers Hit!
internal class SpellMidsummerBraziersHit : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Reapply, AuraScriptHookType.EffectAfterApply));
    }

    private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var player = Target.AsPlayer;

        if (!player)
            return;

        if ((player.HasAura(SpellIds.TORCH_TOSSING_TRAINING) && StackAmount == 8) ||
            (player.HasAura(SpellIds.TORCH_TOSSING_PRACTICE) && StackAmount == 20))
        {
            if (player.Team == TeamFaction.Alliance)
                player.SpellFactory.CastSpell(player, SpellIds.TORCH_TOSSING_TRAINING_SUCCESS_ALLIANCE, true);
            else if (player.Team == TeamFaction.Horde)
                player.SpellFactory.CastSpell(player, SpellIds.TORCH_TOSSING_TRAINING_SUCCESS_HORDE, true);

            Remove();
        }
    }
}

[Script] // 45907 - Torch Target Picker
internal class SpellMidsummerTorchTargetPicker : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var target = HitUnit;
        target.SpellFactory.CastSpell(target, SpellIds.TARGET_INDICATOR_COSMETIC, true);
        target.SpellFactory.CastSpell(target, SpellIds.TARGET_INDICATOR, true);
    }
}

[Script] // 46054 - Torch Toss (land)
internal class SpellMidsummerTorchTossLand : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        HitUnit.SpellFactory.CastSpell(Caster, SpellIds.BRAZIERS_HIT, true);
    }
}

[Script] // 29705, 29726, 29727 - Test Ribbon Pole Channel
internal class SpellMidsummerTestRibbonPoleChannel : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
    }

    private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(SpellIds.RIBBON_POLE_PERIODIC_VISUAL);
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        var target = Target;
        target.SpellFactory.CastSpell(target, SpellIds.RIBBON_POLE_PERIODIC_VISUAL, true);

        var aur = target.GetAura(SpellIds.RIBBON_DANCE);

        if (aur != null)
        {
            aur.SetMaxDuration(Math.Min(3600000, aur.MaxDuration + 180000));
            aur.RefreshDuration();

            if (aur.MaxDuration == 3600000 &&
                target.HasAura(SpellIds.HAS_FULL_MIDSUMMER_SET))
                target.SpellFactory.CastSpell(target, SpellIds.BURNING_HOT_POLE_DANCE, true);
        }
        else
            target.SpellFactory.CastSpell(target, SpellIds.RIBBON_DANCE, true);
    }
}

[Script] // 45406 - Holiday - Midsummer, Ribbon Pole Periodic Visual
internal class SpellMidsummerRibbonPolePeriodicVisual : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDummy));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        var target = Target;

        if (!target.HasAura(SpellIds.TEST_RIBBON_POLE1) &&
            !target.HasAura(SpellIds.TEST_RIBBON_POLE2) &&
            !target.HasAura(SpellIds.TEST_RIBBON_POLE3))
            Remove();
    }
}

[Script] // 45819 - Throw Torch
internal class SpellMidsummerJuggleTorch : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        if (ExplTargetDest == null)
            return;

        Position spellDest = ExplTargetDest;
        double distance = Caster.Location.GetExactDist2d(spellDest.X, spellDest.Y);

        uint torchSpellID = 0;
        uint torchShadowSpellID = 0;

        if (distance <= 1.5f)
        {
            torchSpellID = SpellIds.JUGGLE_TORCH_SELF;
            torchShadowSpellID = SpellIds.JUGGLE_TORCH_SHADOW_SELF;
            spellDest = Caster.Location;
        }
        else if (distance <= 10.0f)
        {
            torchSpellID = SpellIds.JUGGLE_TORCH_SLOW;
            torchShadowSpellID = SpellIds.JUGGLE_TORCH_SHADOW_SLOW;
        }
        else if (distance <= 20.0f)
        {
            torchSpellID = SpellIds.JUGGLE_TORCH_MEDIUM;
            torchShadowSpellID = SpellIds.JUGGLE_TORCH_SHADOW_MEDIUM;
        }
        else
        {
            torchSpellID = SpellIds.JUGGLE_TORCH_FAST;
            torchShadowSpellID = SpellIds.JUGGLE_TORCH_SHADOW_FAST;
        }

        Caster.SpellFactory.CastSpell(spellDest, torchSpellID, new CastSpellExtraArgs(false));
        Caster.SpellFactory.CastSpell(spellDest, torchShadowSpellID, new CastSpellExtraArgs(false));
    }
}

[Script] // 45644 - Juggle Torch (Catch)
internal class SpellMidsummerTorchCatch : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var player = HitPlayer;

        if (!player)
            return;

        if (player.GetQuestStatus(QuestIds.TORCH_CATCHING_A) == QuestStatus.Rewarded ||
            player.GetQuestStatus(QuestIds.TORCH_CATCHING_H) == QuestStatus.Rewarded)
            player.SpellFactory.CastSpell(player, SpellIds.GIVE_TORCH);
    }
}

[Script] // 46747 - Fling torch
internal class SpellMidsummerFlingTorch : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleDummy(int effIndex)
    {
        var dest = Caster.GetFirstCollisionPosition(30.0f, (float)RandomHelper.NextDouble() * (2 * MathF.PI));
        Caster.SpellFactory.CastSpell(dest, SpellIds.FLING_TORCH_TRIGGERED, new CastSpellExtraArgs(true));
        Caster.SpellFactory.CastSpell(dest, SpellIds.FLING_TORCH_SHADOW, new CastSpellExtraArgs(false));
    }
}

[Script] // 45669 - Fling Torch
internal class SpellMidsummerFlingTorchTriggered : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleTriggerMissile, 0, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHit));
    }

    private void HandleTriggerMissile(int effIndex)
    {
        Position pos = HitDest;

        if (pos != null)
            if (Caster.Location.GetExactDist2d(pos) > 3.0f)
            {
                PreventHitEffect(effIndex);
                Caster.SpellFactory.CastSpell(ExplTargetDest, SpellIds.JUGGLE_TORCH_MISSED, new CastSpellExtraArgs(false));
                Caster.RemoveAura(SpellIds.TORCHES_CAUGHT);
            }
    }
}

[Script] // 45671 - Juggle Torch (Catch, Quest)
internal class SpellMidsummerFlingTorchCatch : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        var player = HitPlayer;

        if (!player)
            return;

        if (ExplTargetDest == null)
            return;

        // Only the caster can catch the torch
        if (player.GUID != Caster.GUID)
            return;

        byte requiredCatches = 0;

        // Number of required catches depends on quest - 4 for the normal quest, 10 for the daily version
        if (player.GetQuestStatus(QuestIds.TORCH_CATCHING_A) == QuestStatus.Incomplete ||
            player.GetQuestStatus(QuestIds.TORCH_CATCHING_H) == QuestStatus.Incomplete)
            requiredCatches = 3;
        else if (player.GetQuestStatus(QuestIds.MORE_TORCH_CATCHING_A) == QuestStatus.Incomplete ||
                 player.GetQuestStatus(QuestIds.MORE_TORCH_CATCHING_H) == QuestStatus.Incomplete)
            requiredCatches = 9;

        // Used quest Item without being on quest - do nothing
        if (requiredCatches == 0)
            return;

        if (player.GetAuraCount(SpellIds.TORCHES_CAUGHT) >= requiredCatches)
        {
            player.SpellFactory.CastSpell(player, (player.Team == TeamFaction.Alliance) ? SpellIds.TORCH_CATCHING_SUCCESS_ALLIANCE : SpellIds.TORCH_CATCHING_SUCCESS_HORDE);
            player.SpellFactory.CastSpell(player, SpellIds.TORCH_CATCHING_REMOVE_TORCHES);
            player.RemoveAura(SpellIds.TORCHES_CAUGHT);
        }
        else
        {
            var dest = player.GetFirstCollisionPosition(15.0f, (float)RandomHelper.NextDouble() * (2 * MathF.PI));
            player.SpellFactory.CastSpell(player, SpellIds.TORCHES_CAUGHT);
            player.SpellFactory.CastSpell(dest, SpellIds.FLING_TORCH_TRIGGERED, new CastSpellExtraArgs(true));
            player.SpellFactory.CastSpell(dest, SpellIds.FLING_TORCH_SHADOW, new CastSpellExtraArgs(false));
        }
    }
}

[Script] // 45676 - Juggle Torch (Quest, Missed)
internal class SpellMidsummerFlingTorchMissed : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEntry));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 2, Targets.UnitDestAreaEntry));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        // This spell only hits the caster
        targets.RemoveAll(obj => obj.GUID != Caster.GUID);
    }
}