// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BaradinHold.Occuthar;

internal struct SpellIds
{
    public const uint SEARING_SHADOWS = 96913;
    public const uint FOCUSED_FIRE_FIRST_DAMAGE = 97212;
    public const uint FOCUSED_FIRE_TRIGGER = 96872;
    public const uint FOCUSED_FIRE_VISUAL = 96886;
    public const uint FOCUSED_FIRE = 96884;
    public const uint EYES_OF_OCCUTHAR = 96920;
    public const uint GAZE_OF_OCCUTHAR = 96942;
    public const uint OCCUTHARS_DESTUCTION = 96968;
    public const uint BERSERK = 47008;
}

internal struct EventIds
{
    public const uint SEARING_SHADOWS = 1;
    public const uint FOCUSED_FIRE = 2;
    public const uint EYES_OF_OCCUTHAR = 3;
    public const uint BERSERK = 4;

    public const uint FOCUSED_FIRE_FIRST_DAMAGE = 1;
}

internal struct MiscConst
{
    public const uint MAX_OCCUTHAR_VEHICLE_SEATS = 7;
}

[Script]
internal class BossOccuthar : BossAI
{
    private readonly Vehicle _vehicle;

    public BossOccuthar(Creature creature) : base(creature, DataTypes.OCCUTHAR)
    {
        _vehicle = Me.VehicleKit1;
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Instance.SendEncounterUnit(EncounterFrameType.Engage, Me);
        Events.ScheduleEvent(EventIds.SEARING_SHADOWS, TimeSpan.FromSeconds(8));
        Events.ScheduleEvent(EventIds.FOCUSED_FIRE, TimeSpan.FromSeconds(15));
        Events.ScheduleEvent(EventIds.EYES_OF_OCCUTHAR, TimeSpan.FromSeconds(30));
        Events.ScheduleEvent(EventIds.BERSERK, TimeSpan.FromMinutes(5));
    }

    public override void EnterEvadeMode(EvadeReason why)
    {
        base.EnterEvadeMode(why);
        Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
        _DespawnAtEvade();
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
    }

    public override void JustSummoned(Creature summon)
    {
        Summons.Summon(summon);

        if (summon.Entry == CreatureIds.FOCUS_FIRE_DUMMY)
        {
            DoCast(summon, SpellIds.FOCUSED_FIRE);

            for (sbyte i = 0; i < MiscConst.MAX_OCCUTHAR_VEHICLE_SEATS; ++i)
            {
                var vehicle = _vehicle.GetPassenger(i);

                if (vehicle)
                    vehicle.SpellFactory.CastSpell(summon, SpellIds.FOCUSED_FIRE_VISUAL);
            }
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Events.Update(diff);

        if (Me.HasUnitState(UnitState.Casting))
            return;

        Events.ExecuteEvents(eventId =>
        {
            switch (eventId)
            {
                case EventIds.SEARING_SHADOWS:
                    DoCastAOE(SpellIds.SEARING_SHADOWS);
                    Events.ScheduleEvent(EventIds.SEARING_SHADOWS, TimeSpan.FromSeconds(25));

                    break;
                case EventIds.FOCUSED_FIRE:
                    DoCastAOE(SpellIds.FOCUSED_FIRE_TRIGGER, new CastSpellExtraArgs(true));
                    Events.ScheduleEvent(EventIds.FOCUSED_FIRE, TimeSpan.FromSeconds(15));

                    break;
                case EventIds.EYES_OF_OCCUTHAR:
                    DoCastAOE(SpellIds.EYES_OF_OCCUTHAR);
                    Events.RescheduleEvent(EventIds.FOCUSED_FIRE, TimeSpan.FromSeconds(15));
                    Events.ScheduleEvent(EventIds.EYES_OF_OCCUTHAR, TimeSpan.FromSeconds(60));

                    break;
                case EventIds.BERSERK:
                    DoCast(Me, SpellIds.BERSERK, new CastSpellExtraArgs(true));

                    break;
            }
        });

        DoMeleeAttackIfReady();
    }
}

[Script]
internal class NPCEyestalk : ScriptedAI
{
    private readonly InstanceScript _instance;
    private byte _damageCount;

    public NPCEyestalk(Creature creature) : base(creature)
    {
        _instance = creature.InstanceScript;
    }

    public override void IsSummonedBy(WorldObject summoner)
    {
        // player is the spellcaster so register summon manually
        var occuthar = ObjectAccessor.GetCreature(Me, _instance.GetGuidData(DataTypes.OCCUTHAR));

        occuthar?.AI.JustSummoned(Me);
    }

    public override void Reset()
    {
        Events.Reset();
        Events.ScheduleEvent(EventIds.FOCUSED_FIRE_FIRST_DAMAGE, TimeSpan.FromSeconds(0));
    }

    public override void UpdateAI(uint diff)
    {
        Events.Update(diff);

        if (Events.ExecuteEvent() == EventIds.FOCUSED_FIRE_FIRST_DAMAGE)
        {
            DoCastAOE(SpellIds.FOCUSED_FIRE_FIRST_DAMAGE);

            if (++_damageCount < 2)
                Events.ScheduleEvent(EventIds.FOCUSED_FIRE_FIRST_DAMAGE, TimeSpan.FromSeconds(1));
        }
    }

    public override void EnterEvadeMode(EvadeReason why) { } // Never evade
}

[Script] // 96872 - Focused Fire
internal class SpellOccutharFocusedFireSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        if (targets.Count < 2)
            return;

        targets.RemoveAll(target => Caster.Victim == target);

        if (targets.Count >= 2)
            targets.RandomResize(1);
    }
}

[Script] // Id - 96931 Eyes of Occu'thar
internal class SpellOccutharEyesOfOccutharSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override bool Load()
    {
        return Caster.IsPlayer;
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry));
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        if (targets.Empty())
            return;

        targets.RandomResize(1);
    }

    private void HandleScript(int effIndex)
    {
        HitUnit.SpellFactory.CastSpell(Caster, (uint)EffectValue, true);
    }
}

[Script] // Id - 96932 Eyes of Occu'thar
internal class SpellOccutharEyesOfOccutharVehicleSpellScript : SpellScript, ISpellAfterHit
{
    public override bool Load()
    {
        var instance = Caster.Map.ToInstanceMap;

        if (instance != null)
            return instance.GetScriptName() == nameof(InstanceBaradinHold);

        return false;
    }

    public void AfterHit()
    {
        Position pos = HitUnit.Location;

        var occuthar = ObjectAccessor.GetCreature(Caster, Caster.InstanceScript.GetGuidData(DataTypes.OCCUTHAR));

        if (occuthar != null)
        {
            Creature creature = occuthar.SummonCreature(CreatureIds.EYE_OF_OCCUTHAR, pos);

            creature?.SpellFactory.CastSpell(HitUnit, SpellIds.GAZE_OF_OCCUTHAR, false);
        }
    }
}

[Script] // 96942 / 101009 - Gaze of Occu'thar
internal class SpellOccutharOccutharsDestructionAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        return Caster && Caster.TypeId == TypeId.Unit;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 2, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;

        if (caster)
        {
            if (IsExpired)
                caster.SpellFactory.CastSpell((WorldObject)null, SpellIds.OCCUTHARS_DESTUCTION, new CastSpellExtraArgs(aurEff));

            caster.AsCreature.DespawnOrUnsummon(TimeSpan.FromMilliseconds(500));
        }
    }
}