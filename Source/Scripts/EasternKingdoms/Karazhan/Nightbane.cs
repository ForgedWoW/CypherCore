// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Karazhan.Nightbane;

internal struct SpellIds
{
    public const uint BELLOWING_ROAR = 36922;
    public const uint CHARRED_EARTH = 30129;
    public const uint CLEAVE = 30131;
    public const uint DISTRACTING_ASH = 30130;
    public const uint RAIN_OF_BONES = 37098;
    public const uint SMOKING_BLAST = 30128;
    public const uint SMOKING_BLAST_T = 37057;
    public const uint SMOLDERING_BREATH = 30210;
    public const uint SUMMON_SKELETON = 30170;
    public const uint TAIL_SWEEP = 25653;
}

internal struct TextIds
{
    public const uint EMOTE_SUMMON = 0;
    public const uint YELL_AGGRO = 1;
    public const uint YELL_FLY_PHASE = 2;
    public const uint YELL_LAND_PHASE = 3;
    public const uint EMOTE_BREATH = 4;
}

internal struct PointIds
{
    public const uint INTRO_START = 0;
    public const uint INTRO_END = 1;
    public const uint INTRO_LANDING = 2;
    public const uint PHASE_TWO_FLY = 3;
    public const uint PHASE_TWO_PRE_FLY = 4;
    public const uint PHASE_TWO_LANDING = 5;
    public const uint PHASE_TWO_END = 6;
}

internal struct SplineChainIds
{
    public const uint INTRO_START = 1;
    public const uint INTRO_END = 2;
    public const uint INTRO_LANDING = 3;
    public const uint SECOND_LANDING = 4;
    public const uint PHASE_TWO = 5;
}

internal enum NightbanePhases
{
    Intro = 0,
    Ground,
    Fly
}

internal struct MiscConst
{
    public const int ACTION_SUMMON = 0;
    public const uint PATH_PHASE_TWO = 13547500;

    public const uint GROUP_GROUND = 1;
    public const uint GROUP_FLY = 2;

    public static Position FlyPosition = new(-11160.13f, -1870.683f, 97.73876f, 0.0f);
    public static Position FlyPositionLeft = new(-11094.42f, -1866.992f, 107.8375f, 0.0f);
    public static Position FlyPositionRight = new(-11193.77f, -1921.983f, 107.9845f, 0.0f);
}

[Script]
internal class BossNightbane : BossAI
{
    private byte _flyCount;
    private NightbanePhases _phase;

    public BossNightbane(Creature creature) : base(creature, DataTypes.NIGHTBANE) { }

    public override void Reset()
    {
        _Reset();
        _flyCount = 0;
        Me.SetDisableGravity(true);
        HandleTerraceDoors(true);
        var urn = ObjectAccessor.GetGameObject(Me, Instance.GetGuidData(DataTypes.GO_BLACKENED_URN));

        if (urn)
            urn.RemoveFlag(GameObjectFlags.InUse);
    }

    public override void EnterEvadeMode(EvadeReason why)
    {
        Me.SetDisableGravity(true);
        base.EnterEvadeMode(why);
    }

    public override void JustReachedHome()
    {
        _DespawnAtEvade();
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        HandleTerraceDoors(true);
    }

    public override void DoAction(int action)
    {
        if (action == MiscConst.ACTION_SUMMON)
        {
            Talk(TextIds.EMOTE_SUMMON);
            _phase = NightbanePhases.Intro;
            Me.SetActive(true);
            Me.SetFarVisible(true);
            Me.RemoveUnitFlag(UnitFlags.Uninteractible);
            Me.MotionMaster.MoveAlongSplineChain(PointIds.INTRO_START, SplineChainIds.INTRO_START, false);
            HandleTerraceDoors(false);
        }
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.YELL_AGGRO);
        SetupGroundPhase();
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (_phase == NightbanePhases.Fly)
        {
            if (damage >= Me.Health)
                damage = (uint)(Me.Health - 1);

            return;
        }

        if ((_flyCount == 0 && HealthBelowPct(75)) ||
            (_flyCount == 1 && HealthBelowPct(50)) ||
            (_flyCount == 2 && HealthBelowPct(25)))
        {
            _phase = NightbanePhases.Fly;
            StartPhaseFly();
        }
    }

    public override void MovementInform(MovementGeneratorType type, uint pointId)
    {
        if (type == MovementGeneratorType.SplineChain)
            switch (pointId)
            {
                case PointIds.INTRO_START:
                    Me.SetStandState(UnitStandStateType.Stand);
                    SchedulerProtected.Schedule(TimeSpan.FromMilliseconds(1), task => { Me.MotionMaster.MoveAlongSplineChain(PointIds.INTRO_END, SplineChainIds.INTRO_END, false); });

                    break;
                case PointIds.INTRO_END:
                    SchedulerProtected.Schedule(TimeSpan.FromSeconds(2), task => { Me.MotionMaster.MoveAlongSplineChain(PointIds.INTRO_LANDING, SplineChainIds.INTRO_LANDING, false); });

                    break;
                case PointIds.INTRO_LANDING:
                    Me.SetDisableGravity(false);
                    Me.HandleEmoteCommand(Emote.OneshotLand);

                    SchedulerProtected.Schedule(TimeSpan.FromSeconds(3),
                                                task =>
                                                {
                                                    Me.SetImmuneToPC(false);
                                                    DoZoneInCombat();
                                                });

                    break;
                case PointIds.PHASE_TWO_LANDING:
                    _phase = NightbanePhases.Ground;
                    Me.SetDisableGravity(false);
                    Me.HandleEmoteCommand(Emote.OneshotLand);

                    SchedulerProtected.Schedule(TimeSpan.FromSeconds(3),
                                                task =>
                                                {
                                                    SetupGroundPhase();
                                                    Me.ReactState = ReactStates.Aggressive;
                                                });

                    break;
                case PointIds.PHASE_TWO_END:
                    SchedulerProtected.Schedule(TimeSpan.FromMilliseconds(1), task => { Me.MotionMaster.MoveAlongSplineChain(PointIds.PHASE_TWO_LANDING, SplineChainIds.SECOND_LANDING, false); });

                    break;
            }
        else if (type == MovementGeneratorType.Point)
        {
            if (pointId == PointIds.PHASE_TWO_FLY)
            {
                SchedulerProtected.Schedule(TimeSpan.FromSeconds(33),
                                            MiscConst.GROUP_FLY,
                                            task =>
                                            {
                                                SchedulerProtected.CancelGroup(MiscConst.GROUP_FLY);

                                                SchedulerProtected.Schedule(TimeSpan.FromSeconds(2),
                                                                            MiscConst.GROUP_GROUND,
                                                                            landTask =>
                                                                            {
                                                                                Talk(TextIds.YELL_LAND_PHASE);
                                                                                Me.SetDisableGravity(true);
                                                                                Me.MotionMaster.MoveAlongSplineChain(PointIds.PHASE_TWO_END, SplineChainIds.PHASE_TWO, false);
                                                                            });
                                            });

                SchedulerProtected.Schedule(TimeSpan.FromSeconds(2),
                                            MiscConst.GROUP_FLY,
                                            task =>
                                            {
                                                Talk(TextIds.EMOTE_BREATH);

                                                task.Schedule(TimeSpan.FromSeconds(3),
                                                              MiscConst.GROUP_FLY,
                                                              somethingTask =>
                                                              {
                                                                  ResetThreatList();
                                                                  var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

                                                                  if (target)
                                                                  {
                                                                      Me.SetFacingToObject(target);
                                                                      DoCast(target, SpellIds.RAIN_OF_BONES);
                                                                  }
                                                              });
                                            });

                SchedulerProtected.Schedule(TimeSpan.FromSeconds(21),
                                            MiscConst.GROUP_FLY,
                                            task =>
                                            {
                                                var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

                                                if (target)
                                                    DoCast(target, SpellIds.SMOKING_BLAST_T);

                                                task.Repeat(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7));
                                            });

                SchedulerProtected.Schedule(TimeSpan.FromSeconds(17),
                                            MiscConst.GROUP_FLY,
                                            task =>
                                            {
                                                var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

                                                if (target)
                                                    DoCast(target, SpellIds.SMOKING_BLAST);

                                                task.Repeat(TimeSpan.FromMilliseconds(1400));
                                            });
            }
            else if (pointId == PointIds.PHASE_TWO_PRE_FLY)
                SchedulerProtected.Schedule(TimeSpan.FromMilliseconds(1), task => { Me.MotionMaster.MovePoint(PointIds.PHASE_TWO_FLY, MiscConst.FlyPosition, true); });
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim() &&
            _phase != NightbanePhases.Intro)
            return;

        SchedulerProtected.Update(diff, () => DoMeleeAttackIfReady());
    }

    private void SetupGroundPhase()
    {
        _phase = NightbanePhases.Ground;

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(0),
                                    TimeSpan.FromSeconds(15),
                                    MiscConst.GROUP_GROUND,
                                    task =>
                                    {
                                        DoCastVictim(SpellIds.CLEAVE);
                                        task.Repeat(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(15));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(4),
                                    TimeSpan.FromSeconds(23),
                                    MiscConst.GROUP_GROUND,
                                    task =>
                                    {
                                        var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

                                        if (target)
                                            if (!Me.Location.HasInArc(MathF.PI, target.Location))
                                                DoCast(target, SpellIds.TAIL_SWEEP);

                                        task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(30));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(48), MiscConst.GROUP_GROUND, task => { DoCastAOE(SpellIds.BELLOWING_ROAR); });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(12),
                                    TimeSpan.FromSeconds(18),
                                    MiscConst.GROUP_GROUND,
                                    task =>
                                    {
                                        var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

                                        if (target)
                                            DoCast(target, SpellIds.CHARRED_EARTH);

                                        task.Repeat(TimeSpan.FromSeconds(18), TimeSpan.FromSeconds(21));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(26),
                                    TimeSpan.FromSeconds(30),
                                    MiscConst.GROUP_GROUND,
                                    task =>
                                    {
                                        DoCastVictim(SpellIds.SMOLDERING_BREATH);
                                        task.Repeat(TimeSpan.FromSeconds(28), TimeSpan.FromSeconds(40));
                                    });

        SchedulerProtected.Schedule(TimeSpan.FromSeconds(82),
                                    MiscConst.GROUP_GROUND,
                                    task =>
                                    {
                                        var target = SelectTarget(SelectTargetMethod.Random, 0, 0.0f, true);

                                        if (target)
                                            DoCast(target, SpellIds.DISTRACTING_ASH);
                                    });
    }

    private void HandleTerraceDoors(bool open)
    {
        Instance.HandleGameObject(Instance.GetGuidData(DataTypes.MASTERS_TERRACE_DOOR1), open);
        Instance.HandleGameObject(Instance.GetGuidData(DataTypes.MASTERS_TERRACE_DOOR2), open);
    }

    private void StartPhaseFly()
    {
        ++_flyCount;
        Talk(TextIds.YELL_FLY_PHASE);
        SchedulerProtected.CancelGroup(MiscConst.GROUP_GROUND);
        Me.InterruptNonMeleeSpells(false);
        Me.HandleEmoteCommand(Emote.OneshotLiftoff);
        Me.SetDisableGravity(true);
        Me.ReactState = ReactStates.Passive;
        Me.AttackStop();

        if (Me.GetDistance(MiscConst.FlyPositionLeft) < Me.GetDistance(MiscConst.FlyPosition))
            Me.MotionMaster.MovePoint(PointIds.PHASE_TWO_PRE_FLY, MiscConst.FlyPositionLeft, true);
        else if (Me.GetDistance(MiscConst.FlyPositionRight) < Me.GetDistance(MiscConst.FlyPosition))
            Me.MotionMaster.MovePoint(PointIds.PHASE_TWO_PRE_FLY, MiscConst.FlyPositionRight, true);
        else
            Me.MotionMaster.MovePoint(PointIds.PHASE_TWO_FLY, MiscConst.FlyPosition, true);
    }
}

[Script] // 37098 - Rain of Bones
internal class SpellRainOfBonesAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnTrigger, 1, AuraType.PeriodicTriggerSpell));
    }

    private void OnTrigger(AuraEffect aurEff)
    {
        if (aurEff.TickNumber % 5 == 0)
            Target.SpellFactory.CastSpell(Target, SpellIds.SUMMON_SKELETON, true);
    }
}

[Script]
internal class GOBlackenedUrn : GameObjectAI
{
    private readonly InstanceScript _instance;

    public GOBlackenedUrn(GameObject go) : base(go)
    {
        _instance = go.InstanceScript;
    }

    public override bool OnGossipHello(Player player)
    {
        if (Me.HasFlag(GameObjectFlags.InUse))
            return false;

        if (_instance.GetBossState(DataTypes.NIGHTBANE) == EncounterState.Done ||
            _instance.GetBossState(DataTypes.NIGHTBANE) == EncounterState.InProgress)
            return false;

        var nightbane = ObjectAccessor.GetCreature(Me, _instance.GetGuidData(DataTypes.NIGHTBANE));

        if (nightbane)
        {
            Me.SetFlag(GameObjectFlags.InUse);
            nightbane.AI.DoAction(MiscConst.ACTION_SUMMON);
        }

        return false;
    }
}