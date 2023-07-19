// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.m_Events.Brewfest;

internal struct SpellIds
{
    //Ramblabla
    public const uint GIDDYUP = 42924;
    public const uint RENTAL_RACING_RAM = 43883;
    public const uint SWIFT_WORK_RAM = 43880;
    public const uint RENTAL_RACING_RAM_AURA = 42146;
    public const uint RAM_LEVEL_NEUTRAL = 43310;
    public const uint RAM_TROT = 42992;
    public const uint RAM_CANTER = 42993;
    public const uint RAM_GALLOP = 42994;
    public const uint RAM_FATIGUE = 43052;
    public const uint EXHAUSTED_RAM = 43332;
    public const uint RELAY_RACE_TURN_IN = 44501;

    //Brewfestmounttransformation
    public const uint MOUNT_RAM100 = 43900;
    public const uint MOUNT_RAM60 = 43899;
    public const uint MOUNT_KODO100 = 49379;
    public const uint MOUNT_KODO60 = 49378;
    public const uint BREWFEST_MOUNT_TRANSFORM = 49357;
    public const uint BREWFEST_MOUNT_TRANSFORM_REVERSE = 52845;
}

internal struct QuestIds
{
    //Ramblabla
    public const uint BREWFEST_SPEED_BUNNY_GREEN = 43345;
    public const uint BREWFEST_SPEED_BUNNY_YELLOW = 43346;
    public const uint BREWFEST_SPEED_BUNNY_RED = 43347;

    //Barkerbunny
    // Horde
    public const uint BARK_FOR_DROHNS_DISTILLERY = 11407;
    public const uint BARK_FOR_TCHALIS_VOODOO_BREWERY = 11408;

    // Alliance
    public const uint BARK_BARLEYBREW = 11293;
    public const uint BARK_FOR_THUNDERBREWS = 11294;
}

internal struct TextIds
{
    // Bark For Drohn'S Distillery!
    public const uint DROHN_DISTILLERY1 = 23520;
    public const uint DROHN_DISTILLERY2 = 23521;
    public const uint DROHN_DISTILLERY3 = 23522;
    public const uint DROHN_DISTILLERY4 = 23523;

    // Bark For T'Chali'S Voodoo Brewery!
    public const uint CHALIS_VOODOO1 = 23524;
    public const uint CHALIS_VOODOO2 = 23525;
    public const uint CHALIS_VOODOO3 = 23526;
    public const uint CHALIS_VOODOO4 = 23527;

    // Bark For The Barleybrews!
    public const uint BARLEYBREW1 = 23464;
    public const uint BARLEYBREW2 = 23465;
    public const uint BARLEYBREW3 = 23466;
    public const uint BARLEYBREW4 = 22941;

    // Bark For The Thunderbrews!
    public const uint THUNDERBREWS1 = 23467;
    public const uint THUNDERBREWS2 = 23468;
    public const uint THUNDERBREWS3 = 23469;
    public const uint THUNDERBREWS4 = 22942;
}

[Script] // 42924 - Giddyup!
internal class SpellBrewfestGiddyup : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnChange, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.ChangeAmountMask, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnChange, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.ChangeAmountMask, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void OnChange(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        if (!target.HasAura(SpellIds.RENTAL_RACING_RAM) &&
            !target.HasAura(SpellIds.SWIFT_WORK_RAM))
        {
            target.RemoveAura(Id);

            return;
        }

        if (target.HasAura(SpellIds.EXHAUSTED_RAM))
            return;

        switch (StackAmount)
        {
            case 1: // green
                target.RemoveAura(SpellIds.RAM_LEVEL_NEUTRAL);
                target.RemoveAura(SpellIds.RAM_CANTER);
                target.SpellFactory.CastSpell(target, SpellIds.RAM_TROT, true);

                break;
            case 6: // yellow
                target.RemoveAura(SpellIds.RAM_TROT);
                target.RemoveAura(SpellIds.RAM_GALLOP);
                target.SpellFactory.CastSpell(target, SpellIds.RAM_CANTER, true);

                break;
            case 11: // red
                target.RemoveAura(SpellIds.RAM_CANTER);
                target.SpellFactory.CastSpell(target, SpellIds.RAM_GALLOP, true);

                break;
        }

        if (TargetApplication.RemoveMode == AuraRemoveMode.Default)
        {
            target.RemoveAura(SpellIds.RAM_TROT);
            target.SpellFactory.CastSpell(target, SpellIds.RAM_LEVEL_NEUTRAL, true);
        }
    }

    private void OnPeriodic(AuraEffect aurEff)
    {
        Target.RemoveAuraFromStack(Id);
    }
}

// 43310 - Ram Level - Neutral
// 42992 - Ram - Trot
// 42993 - Ram - Canter
// 42994 - Ram - Gallop
[Script]
internal class SpellBrewfestRam : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 1, AuraType.PeriodicDummy));
    }

    private void OnPeriodic(AuraEffect aurEff)
    {
        var target = Target;

        if (target.HasAura(SpellIds.EXHAUSTED_RAM))
            return;

        switch (Id)
        {
            case SpellIds.RAM_LEVEL_NEUTRAL:
            {
                var aura = target.GetAura(SpellIds.RAM_FATIGUE);

                aura?.ModStackAmount(-4);
            }

                break;
            case SpellIds.RAM_TROT: // green
            {
                var aura = target.GetAura(SpellIds.RAM_FATIGUE);

                aura?.ModStackAmount(-2);

                if (aurEff.TickNumber == 4)
                    target.SpellFactory.CastSpell(target, QuestIds.BREWFEST_SPEED_BUNNY_GREEN, true);
            }

                break;
            case SpellIds.RAM_CANTER:
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.AuraStack, 1);
                target.SpellFactory.CastSpell(target, SpellIds.RAM_FATIGUE, args);

                if (aurEff.TickNumber == 8)
                    target.SpellFactory.CastSpell(target, QuestIds.BREWFEST_SPEED_BUNNY_YELLOW, true);

                break;
            }
            case SpellIds.RAM_GALLOP:
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.AuraStack, target.HasAura(SpellIds.RAM_FATIGUE) ? 4 : 5 /*Hack*/);
                target.SpellFactory.CastSpell(target, SpellIds.RAM_FATIGUE, args);

                if (aurEff.TickNumber == 8)
                    target.SpellFactory.CastSpell(target, QuestIds.BREWFEST_SPEED_BUNNY_RED, true);

                break;
            }
        }
    }
}

[Script] // 43052 - Ram Fatigue
internal class SpellBrewfestRamFatigue : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        if (StackAmount == 101)
        {
            target.RemoveAura(SpellIds.RAM_LEVEL_NEUTRAL);
            target.RemoveAura(SpellIds.RAM_TROT);
            target.RemoveAura(SpellIds.RAM_CANTER);
            target.RemoveAura(SpellIds.RAM_GALLOP);
            target.RemoveAura(SpellIds.GIDDYUP);

            target.SpellFactory.CastSpell(target, SpellIds.EXHAUSTED_RAM, true);
        }
    }
}

[Script] // 43450 - Brewfest - apple trap - friendly DND
internal class SpellBrewfestAppleTrap : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ForceReaction, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura(SpellIds.RAM_FATIGUE);
    }
}

[Script] // 43332 - Exhausted Ram
internal class SpellBrewfestExhaustedRam : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.SpellFactory.CastSpell(target, SpellIds.RAM_LEVEL_NEUTRAL, true);
    }
}

[Script] // 43714 - Brewfest - Relay Race - Intro - Force - Player to throw- DND
internal class SpellBrewfestRelayRaceIntroForcePlayerToThrow : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleForceCast, 0, SpellEffectName.ForceCast, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleForceCast(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        // All this spells trigger a spell that requires reagents; if the
        // triggered spell is cast as "triggered", reagents are not consumed
        HitUnit.SpellFactory.CastSpell((Unit)null, EffectInfo.TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask & ~TriggerCastFlags.IgnorePowerAndReagentCost));
    }
}

[Script] // 43755 - Brewfest - Daily - Relay Race - Player - Increase Mount Duration - DND
internal class SpellBrewfestRelayRaceTurnIn : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        var aura = HitUnit.GetAura(SpellIds.SWIFT_WORK_RAM);

        if (aura != null)
        {
            aura.SetDuration(aura.Duration + 30 * Time.IN_MILLISECONDS);
            Caster.SpellFactory.CastSpell(HitUnit, SpellIds.RELAY_RACE_TURN_IN, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
        }
    }
}

[Script] // 43876 - Dismount Ram
internal class SpellBrewfestDismountRam : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        Caster.RemoveAura(SpellIds.RENTAL_RACING_RAM);
    }
}

// 43259 Brewfest  - Barker Bunny 1
// 43260 Brewfest  - Barker Bunny 2
// 43261 Brewfest  - Barker Bunny 3
// 43262 Brewfest  - Barker Bunny 4
[Script]
internal class SpellBrewfestBarkerBunny : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        return OwnerAsUnit.IsTypeId(TypeId.Player);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target.AsPlayer;

        uint broadcastTextId = 0;

        if (target.GetQuestStatus(QuestIds.BARK_FOR_DROHNS_DISTILLERY) == QuestStatus.Incomplete ||
            target.GetQuestStatus(QuestIds.BARK_FOR_DROHNS_DISTILLERY) == QuestStatus.Complete)
            broadcastTextId = RandomHelper.RAND(TextIds.DROHN_DISTILLERY1, TextIds.DROHN_DISTILLERY2, TextIds.DROHN_DISTILLERY3, TextIds.DROHN_DISTILLERY4);

        if (target.GetQuestStatus(QuestIds.BARK_FOR_TCHALIS_VOODOO_BREWERY) == QuestStatus.Incomplete ||
            target.GetQuestStatus(QuestIds.BARK_FOR_TCHALIS_VOODOO_BREWERY) == QuestStatus.Complete)
            broadcastTextId = RandomHelper.RAND(TextIds.CHALIS_VOODOO1, TextIds.CHALIS_VOODOO2, TextIds.CHALIS_VOODOO3, TextIds.CHALIS_VOODOO4);

        if (target.GetQuestStatus(QuestIds.BARK_BARLEYBREW) == QuestStatus.Incomplete ||
            target.GetQuestStatus(QuestIds.BARK_BARLEYBREW) == QuestStatus.Complete)
            broadcastTextId = RandomHelper.RAND(TextIds.BARLEYBREW1, TextIds.BARLEYBREW2, TextIds.BARLEYBREW3, TextIds.BARLEYBREW4);

        if (target.GetQuestStatus(QuestIds.BARK_FOR_THUNDERBREWS) == QuestStatus.Incomplete ||
            target.GetQuestStatus(QuestIds.BARK_FOR_THUNDERBREWS) == QuestStatus.Complete)
            broadcastTextId = RandomHelper.RAND(TextIds.THUNDERBREWS1, TextIds.THUNDERBREWS2, TextIds.THUNDERBREWS3, TextIds.THUNDERBREWS4);

        if (broadcastTextId != 0)
            target.Talk(broadcastTextId, ChatMsg.Say, WorldConfig.GetFloatValue(WorldCfg.ListenRangeSay), target);
    }
}

[Script]
internal class SpellItemBrewfestMountTransformation : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster.AsPlayer;

        if (caster.HasAuraType(AuraType.Mounted))
        {
            caster.RemoveAurasByType(AuraType.Mounted);
            uint spellID;

            switch (SpellInfo.Id)
            {
                case SpellIds.BREWFEST_MOUNT_TRANSFORM:
                    if (caster.GetSpeedRate(UnitMoveType.Run) >= 2.0f)
                        spellID = caster.Team == TeamFaction.Alliance ? SpellIds.MOUNT_RAM100 : SpellIds.MOUNT_KODO100;
                    else
                        spellID = caster.Team == TeamFaction.Alliance ? SpellIds.MOUNT_RAM60 : SpellIds.MOUNT_KODO60;

                    break;
                case SpellIds.BREWFEST_MOUNT_TRANSFORM_REVERSE:
                    if (caster.GetSpeedRate(UnitMoveType.Run) >= 2.0f)
                        spellID = caster.Team == TeamFaction.Horde ? SpellIds.MOUNT_RAM100 : SpellIds.MOUNT_KODO100;
                    else
                        spellID = caster.Team == TeamFaction.Horde ? SpellIds.MOUNT_RAM60 : SpellIds.MOUNT_KODO60;

                    break;
                default:
                    return;
            }

            caster.SpellFactory.CastSpell(caster, spellID, true);
        }
    }
}