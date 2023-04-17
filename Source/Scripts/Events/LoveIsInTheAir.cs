// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.m_Events.LoveIsInTheAir;

internal struct SpellIds
{
    //Romantic Picnic
    public const uint BASKET_CHECK = 45119;  // Holiday - Valentine - Romantic Picnic Near Basket Check
    public const uint MEAL_PERIODIC = 45103; // Holiday - Valentine - Romantic Picnic Meal Periodic - Effect Dummy

    public const uint MEAL_EAT_VISUAL = 45120; // Holiday - Valentine - Romantic Picnic Meal Eat Visual

    //public const uint MealParticle = 45114; // Holiday - Valentine - Romantic Picnic Meal Particle - Unused
    public const uint DRINK_VISUAL = 45121;           // Holiday - Valentine - Romantic Picnic Drink Visual
    public const uint ROMANTIC_PICNIC_ACHIEV = 45123; // Romantic Picnic Periodic = 5000

    //CreateHeartCandy
    public const uint CREATE_HEART_CANDY1 = 26668;
    public const uint CREATE_HEART_CANDY2 = 26670;
    public const uint CREATE_HEART_CANDY3 = 26671;
    public const uint CREATE_HEART_CANDY4 = 26672;
    public const uint CREATE_HEART_CANDY5 = 26673;
    public const uint CREATE_HEART_CANDY6 = 26674;
    public const uint CREATE_HEART_CANDY7 = 26675;
    public const uint CREATE_HEART_CANDY8 = 26676;

    //SomethingStinks
    public const uint HEAVILY_PERFUMED = 71507;

    //PilferingPerfume
    public const uint SERVICE_UNIFORM = 71450;
}

internal struct ModelIds
{
    //PilferingPerfume
    public const uint GOBLIN_MALE = 31002;
    public const uint GOBLIN_FEMALE = 31003;
}

[Script] // 45102 Romantic Picnic
internal class SpellLoveIsInTheAirRomanticPicnic : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.SetStandState(UnitStandStateType.Sit);
        target.SpellFactory.CastSpell(target, SpellIds.MEAL_PERIODIC);
    }

    private void OnPeriodic(AuraEffect aurEff)
    {
        // Every 5 seconds
        var target = Target;

        // If our player is no longer sit, Remove all Auras
        if (target.StandState != UnitStandStateType.Sit)
        {
            target.RemoveAura(SpellIds.ROMANTIC_PICNIC_ACHIEV);
            target.RemoveAura(Aura);

            return;
        }

        target.SpellFactory.CastSpell(target, SpellIds.BASKET_CHECK); // unknown use, it targets Romantic Basket
        target.SpellFactory.CastSpell(target, RandomHelper.RAND(SpellIds.MEAL_EAT_VISUAL, SpellIds.DRINK_VISUAL));

        var foundSomeone = false;
        // For nearby players, check if they have the same aura. If so, cast Romantic Picnic (45123)
        // required by Achievement and "hearts" visual
        List<Unit> playerList = new();
        AnyPlayerInObjectRangeCheck checker = new(target, SharedConst.InteractionDistance * 2);
        var searcher = new PlayerListSearcher(target, playerList, checker);
        Cell.VisitGrid(target, searcher, SharedConst.InteractionDistance * 2);

        foreach (Player playerFound in playerList)
            if (target != playerFound &&
                playerFound.HasAura(Id))
            {
                playerFound.SpellFactory.CastSpell(playerFound, SpellIds.ROMANTIC_PICNIC_ACHIEV, true);
                target.SpellFactory.CastSpell(target, SpellIds.ROMANTIC_PICNIC_ACHIEV, true);
                foundSomeone = true;

                break;
            }

        if (!foundSomeone &&
            target.HasAura(SpellIds.ROMANTIC_PICNIC_ACHIEV))
            target.RemoveAura(SpellIds.ROMANTIC_PICNIC_ACHIEV);
    }
}

[Script] // 26678 - Create Heart Candy
internal class SpellLoveIsInTheAirCreateHeartCandy : SpellScript, IHasSpellEffects
{
    private readonly uint[] _createHeartCandySpells =
    {
        SpellIds.CREATE_HEART_CANDY1, SpellIds.CREATE_HEART_CANDY2, SpellIds.CREATE_HEART_CANDY3, SpellIds.CREATE_HEART_CANDY4, SpellIds.CREATE_HEART_CANDY5, SpellIds.CREATE_HEART_CANDY6, SpellIds.CREATE_HEART_CANDY7, SpellIds.CREATE_HEART_CANDY8
    };

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        var target = HitPlayer;

        target?.SpellFactory.CastSpell(target, _createHeartCandySpells.SelectRandom(), true);
    }
}

[Script] // 70192 - Fragrant Air Analysis
internal class SpellLoveIsInTheAirFragrantAirAnalysis : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        HitUnit.RemoveAura((uint)EffectValue);
    }
}

[Script] // 71507 - Heavily Perfumed
internal class SpellLoveIsInTheAirHeavilyPerfumed : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.SpellFactory.CastSpell(Target, (uint)GetEffectInfo(0).CalcValue());
    }
}

[Script] // 71508 - Recently Analyzed
internal class SpellLoveIsInTheAirRecentlyAnalyzed : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        if (TargetApplication != null && TargetApplication.RemoveMode == AuraRemoveMode.Expire)
            Target.SpellFactory.CastSpell(Target, SpellIds.HEAVILY_PERFUMED);
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.SpellFactory.CastSpell(Target, SpellIds.HEAVILY_PERFUMED);
    }
}

[Script] // 69438 - Sample Satisfaction
internal class SpellLoveIsInTheAirSampleSatisfaction : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicDummy));
    }

    private void OnPeriodic(AuraEffect aurEff)
    {
        if (RandomHelper.randChance(30))
            Remove();
    }
}

[Script] // 71450 - Crown Parcel Service Uniform
internal class SpellLoveIsInTheAirServiceUniform : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Transform, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        if (target.IsPlayer)
        {
            if (target.NativeGender == Gender.Male)
                target.SetDisplayId(ModelIds.GOBLIN_MALE);
            else
                target.SetDisplayId(ModelIds.GOBLIN_FEMALE);
        }
    }

    private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        Target.RemoveAura((uint)GetEffectInfo(0).CalcValue());
    }
}

// 71522 - Crown Chemical Co. Supplies
[Script] // 71539 - Crown Chemical Co. Supplies
internal class SpellLoveIsInTheAirCancelServiceUniform : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        HitUnit.RemoveAura(SpellIds.SERVICE_UNIFORM);
    }
}

// 68529 - Perfume Immune
[Script] // 68530 - Cologne Immune
internal class SpellLoveIsInTheAirPerfumeCologneImmune : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
        SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHit));
    }

    private void HandleScript(int effIndex)
    {
        Caster.RemoveAura((uint)EffectValue);
    }
}