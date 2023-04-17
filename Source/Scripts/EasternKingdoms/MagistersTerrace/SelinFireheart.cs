// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.EasternKingdoms.MagistersTerrace.SelinFireheart;

internal struct TextIds
{
    public const uint SAY_AGGRO = 0;
    public const uint SAY_ENERGY = 1;
    public const uint SAY_EMPOWERED = 2;
    public const uint SAY_KILL = 3;
    public const uint SAY_DEATH = 4;
    public const uint EMOTE_CRYSTAL = 5;
}

internal struct SpellIds
{
    // Crystal effect spells
    public const uint FEL_CRYSTAL_DUMMY = 44329;
    public const uint MANA_RAGE = 44320; // This spell triggers 44321, which changes scale and regens mana Requires an entry in spell_script_target

    // Selin's spells
    public const uint DRAIN_LIFE = 44294;
    public const uint FEL_EXPLOSION = 44314;

    public const uint DRAIN_MANA = 46153; // Heroic only
}

internal struct PhaseIds
{
    public const byte NORMAL = 1;
    public const byte DRAIN = 2;
}

internal struct EventIds
{
    public const uint FEL_EXPLOSION = 1;
    public const uint DRAIN_CRYSTAL = 2;
    public const uint DRAIN_MANA = 3;
    public const uint DRAIN_LIFE = 4;
    public const uint EMPOWER = 5;
}

internal struct MiscConst
{
    public const int ACTION_SWITCH_PHASE = 1;
}

[Script] // @todo crystals should really be a Db creature summon group, having them in `creature` like this will cause tons of despawn/respawn bugs
internal class BossSelinFireheart : BossAI
{
    private bool _scheduledEvents;
    private ObjectGuid _crystalGUID;

    public BossSelinFireheart(Creature creature) : base(creature, DataTypes.SELIN_FIREHEART) { }

    public override void Reset()
    {
        var crystals = Me.GetCreatureListWithEntryInGrid(CreatureIds.FEL_CRYSTAL, 250.0f);

        foreach (var creature in crystals)
            creature.Respawn(true);

        _Reset();
        _crystalGUID.Clear();
        _scheduledEvents = false;
    }

    public override void DoAction(int action)
    {
        switch (action)
        {
            case MiscConst.ACTION_SWITCH_PHASE:
                Events.SetPhase(PhaseIds.NORMAL);
                Events.ScheduleEvent(EventIds.FEL_EXPLOSION, TimeSpan.FromSeconds(2), 0, PhaseIds.NORMAL);
                AttackStart(Me.Victim);
                Me.MotionMaster.MoveChase(Me.Victim);

                break;
        }
    }

    public override void JustEngagedWith(Unit who)
    {
        Talk(TextIds.SAY_AGGRO);
        base.JustEngagedWith(who);

        Events.SetPhase(PhaseIds.NORMAL);
        Events.ScheduleEvent(EventIds.FEL_EXPLOSION, TimeSpan.FromMilliseconds(2100), 0, PhaseIds.NORMAL);
    }

    public override void KilledUnit(Unit victim)
    {
        if (victim.IsPlayer)
            Talk(TextIds.SAY_KILL);
    }

    public override void MovementInform(MovementGeneratorType type, uint id)
    {
        if (type == MovementGeneratorType.Point &&
            id == 1)
        {
            var crystalChosen = Global.ObjAccessor.GetUnit(Me, _crystalGUID);

            if (crystalChosen != null &&
                crystalChosen.IsAlive)
            {
                crystalChosen.RemoveUnitFlag(UnitFlags.Uninteractible);
                crystalChosen.SpellFactory.CastSpell(Me, SpellIds.MANA_RAGE, true);
                Events.ScheduleEvent(EventIds.EMPOWER, TimeSpan.FromSeconds(10), PhaseIds.DRAIN);
            }
        }
    }

    public override void JustDied(Unit killer)
    {
        Talk(TextIds.SAY_DEATH);
        _JustDied();

        ShatterRemainingCrystals();
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
                case EventIds.FEL_EXPLOSION:
                    DoCastAOE(SpellIds.FEL_EXPLOSION);
                    Events.ScheduleEvent(EventIds.FEL_EXPLOSION, TimeSpan.FromSeconds(2), 0, PhaseIds.NORMAL);

                    break;
                case EventIds.DRAIN_CRYSTAL:
                    SelectNearestCrystal();
                    _scheduledEvents = false;

                    break;
                case EventIds.DRAIN_MANA:
                {
                    var target = SelectTarget(SelectTargetMethod.Random, 0, 45.0f, true);

                    if (target != null)
                        DoCast(target, SpellIds.DRAIN_MANA);

                    Events.ScheduleEvent(EventIds.DRAIN_MANA, TimeSpan.FromSeconds(10), 0, PhaseIds.NORMAL);

                    break;
                }
                case EventIds.DRAIN_LIFE:
                {
                    var target = SelectTarget(SelectTargetMethod.Random, 0, 20.0f, true);

                    if (target != null)
                        DoCast(target, SpellIds.DRAIN_LIFE);

                    Events.ScheduleEvent(EventIds.DRAIN_LIFE, TimeSpan.FromSeconds(10), 0, PhaseIds.NORMAL);

                    break;
                }
                case EventIds.EMPOWER:
                {
                    Talk(TextIds.SAY_EMPOWERED);

                    var crystalChosen = ObjectAccessor.GetCreature(Me, _crystalGUID);

                    if (crystalChosen && crystalChosen.IsAlive)
                        crystalChosen.KillSelf();

                    _crystalGUID.Clear();

                    Me.MotionMaster.Clear();
                    Me.MotionMaster.MoveChase(Me.Victim);

                    break;
                }
            }

            if (Me.HasUnitState(UnitState.Casting))
                return;
        });

        if (Me.GetPowerPct(PowerType.Mana) < 10.0f)
            if (Events.IsInPhase(PhaseIds.NORMAL) &&
                !_scheduledEvents)
            {
                _scheduledEvents = true;
                var timer = RandomHelper.RandTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(7));
                Events.ScheduleEvent(EventIds.DRAIN_LIFE, timer, 0, PhaseIds.NORMAL);

                if (IsHeroic())
                {
                    Events.ScheduleEvent(EventIds.DRAIN_CRYSTAL, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15), 0, PhaseIds.NORMAL);
                    Events.ScheduleEvent(EventIds.DRAIN_MANA, timer + TimeSpan.FromSeconds(5), 0, PhaseIds.NORMAL);
                }
                else
                    Events.ScheduleEvent(EventIds.DRAIN_CRYSTAL, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25), 0, PhaseIds.NORMAL);
            }

        DoMeleeAttackIfReady();
    }

    private void SelectNearestCrystal()
    {
        var crystal = Me.FindNearestCreature(CreatureIds.FEL_CRYSTAL, 250.0f);

        if (crystal)
        {
            Talk(TextIds.SAY_ENERGY);
            Talk(TextIds.EMOTE_CRYSTAL);

            DoCast(crystal, SpellIds.FEL_CRYSTAL_DUMMY);
            _crystalGUID = crystal.GUID;
            var pos = new Position();
            crystal.GetClosePoint(pos, Me.CombatReach, SharedConst.ContactDistance);

            Events.SetPhase(PhaseIds.DRAIN);
            Me.SetWalk(false);
            Me.MotionMaster.MovePoint(1, pos);
        }
    }

    private void ShatterRemainingCrystals()
    {
        var crystals = Me.GetCreatureListWithEntryInGrid(CreatureIds.FEL_CRYSTAL, 250.0f);

        foreach (var crystal in crystals)
            crystal.KillSelf();
    }
}

[Script]
internal class NPCFelCrystal : ScriptedAI
{
    public NPCFelCrystal(Creature creature) : base(creature) { }

    public override void JustDied(Unit killer)
    {
        var instance = Me.InstanceScript;

        if (instance != null)
        {
            var selin = instance.GetCreature(DataTypes.SELIN_FIREHEART);

            if (selin && selin.IsAlive)
                selin.AI.DoAction(MiscConst.ACTION_SWITCH_PHASE);
        }
    }
}