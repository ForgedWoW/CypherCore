// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps.Checks;
using Forged.MapServer.Movement;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Deadmines.Bosses;

[CreatureScript(47162)]
public class BossGlubtok : BossAI
{
    public static readonly Position Phase2Pos = new(-193.368f, -441.828f, 53.5931f, 1.692970f);
    private readonly Creature _platter;

    private bool _phase2;
    private bool _dying;
    private bool _transitionDone;
    private bool _lastElement;

    public BossGlubtok(Creature creature) : base(creature, DmData.DATA_GLUBTOK)
    {
        Me.SetCanDualWield(true);
        //me.DisableMovementFlagUpdate(true);
        _platter = Me.SummonCreature(Creatures.NPC_GLUBTOK_MAIN_PLATTER, Phase2Pos.X, Phase2Pos.Y, Phase2Pos.Z + 2.0f);
        _platter.SetActive(true);
    }

    public override void Reset()
    {
        _Reset();
        _lastElement = true;
        _phase2 = false;
        _dying = false;
        _transitionDone = false;
        Me.ReactState = ReactStates.Aggressive;
        Me.SetDisableGravity(false);
        Me.SetCanFly(false);

        Me.RemoveUnitFlag(UnitFlags.Uninteractible | UnitFlags.ImmuneToPc);
        // me.RemoveFlag(UNIT_DYNAMIC_FLAGS, UNIT_DYNFLAG_DEAD);
        Me.RemoveUnitFlag2(UnitFlags2.FeignDeath);
        Me.ClearUnitState(UnitState.CannotTurn);

        _platter.AI.DoAction(Actions.ACTION_STOP_FIREWALL);
    }

    public override void JustEnteredCombat(Unit who)
    {
        base.JustEnteredCombat(who);

        Talk((uint)Texts.SAY_AGGRO);
        Events.ScheduleEvent(BossEvents.EVENT_ELEMENTAL_FISTS, TimeSpan.FromMilliseconds(5000));
    }

    public override void JustDied(Unit killer)
    {
        base.JustDied(killer);
        _platter.AI.DoAction(Actions.ACTION_STOP_FIREWALL);
    }

    public override void KilledUnit(Unit victim)
    {
        if (victim != Me)
            Talk(Texts.SAY_KILL);
    }

    public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
    {
        Me.ClearUnitState(UnitState.CannotTurn);
        _platter.AI.DoAction(Actions.ACTION_STOP_FIREWALL);
        base.EnterEvadeMode(why);
    }

    public override void JustSummoned(Creature summon)
    {
        if (!Me.IsInCombat)
        {
            summon.DespawnOrUnsummon();

            return;
        }

        base.JustSummoned(summon);
        summon.AI.AttackStart(Me.Victim);
    }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (!_phase2 && Me.HealthBelowPctDamaged(50, damage) && !Me.HasUnitState(UnitState.Casting))
        {
            _phase2 = true;
            Events.Reset();
            Me.ReactState = ReactStates.Passive;
            Me.AttackStop();
            Me.MotionMaster.Clear();
            Me.NearTeleportTo(Phase2Pos);
            DoCast(Spells.TELEPORT_VISUAL);
            ResetThreatList();
            Events.ScheduleEvent(BossEvents.EVENT_TRANSITION_SAY_1, TimeSpan.FromMilliseconds(4000));
            Events.ScheduleEvent(BossEvents.EVENT_TRANSITION_SAY_2, TimeSpan.FromMilliseconds(6000));
            Events.ScheduleEvent(BossEvents.EVENT_TRANSITION_CAST, TimeSpan.FromMilliseconds(8000));
        }

        if (Me.HealthBelowPctDamaged(1, damage))
            if (!_dying && _transitionDone)
            {
                _platter.AI.DoAction(Actions.ACTION_STOP_FIREWALL);
                Events.Reset();
                DoCast(Me, Spells.ARCANE_OVERLOAD_INITIAL, new CastSpellExtraArgs(true));
                SummonBeams();
                Talk(Texts.SAY_DEATH);
                _dying = true;
                Events.ScheduleEvent(BossEvents.EVENT_FALL_GROUND, TimeSpan.FromMilliseconds(5000));
            }
    }

    public Vector3 GenerateTargetForBeamBunny(bool left)
    {
        float angle;
        var pos = new Vector3();

        for (byte i = 0; i < 8; ++i)
        {
            if (left)
                angle = Me.Location.Orientation - (float)Math.PI / 2 + RandomHelper.FRand((float)-Math.PI / 3.0f, (float)Math.PI / 6.0f);
            else
                angle = Me.Location.Orientation + (float)Math.PI / 2 + RandomHelper.FRand((float)-Math.PI / 6.0f, (float)Math.PI / 3.0f);

            pos.Z = Me.Location.Z + RandomHelper.FRand(4.0f, 23.0f);

            Me.GetNearPoint2D(Me, out pos.X, out pos.Y, 25.0f, angle);
            Global.VMapMgr.GetObjectHitPos(Me.Location.MapId, Me.Location.X, Me.Location.Y, Me.Location.Z + 0.5f, pos.X, pos.Y, pos.Z + 0.5f, out pos.X, out pos.Y, out pos.Z, -1.5f);

            if (Me.Location.GetExactDist2d(pos.X, pos.Y) >= 7.5f)
                break;
        }

        return pos;
    }

    public void SummonBeams()
    {
        var pos1 = new Vector3();
        var pos2 = new Vector3();
        var pos3 = new Vector3();
        uint spellID;

        for (var i = 0; i < 8; ++i)
        {
            if (i < 4)
            {
                pos1 = GenerateTargetForBeamBunny(true);
                pos2 = GenerateTargetForBeamBunny(true);
                pos3 = GenerateTargetForBeamBunny(true);
                spellID = Spells.ARCANE_FIRE_BEAM;
            }
            else
            {
                pos1 = GenerateTargetForBeamBunny(false);
                pos2 = GenerateTargetForBeamBunny(false);
                pos3 = GenerateTargetForBeamBunny(false);
                spellID = Spells.ARCANE_FROST_BEAM;
            }

            Creature dummy = Me.SummonCreature(Creatures.NPC_BEAM_BUNNY, pos1.X, pos1.Y, pos1.Z, 0.0f, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(7000));

            if (dummy != null)
            {
                dummy.SpellFactory.CastSpell(Me, spellID, true);
                dummy.ClearUnitState(UnitState.Casting);
                var init = new MoveSplineInit(dummy);
                init.Path().Add(pos1);
                init.Path().Add(pos2);
                init.Path().Add(pos3);
                init.SetVelocity(1.5f);
                init.SetFly();
                init.SetCyclic();
                init.Launch();
            }
        }
    }

    public override void MovementInform(MovementGeneratorType type, uint id)
    {
        if (type != MovementGeneratorType.Point)
            return;

        if (id == (uint)Points.POINT_FALL_GROUND)
        {
            Me.SetUnitFlag(UnitFlags.ImmuneToPc);
            DoCast(Spells.FEIGN_DEATH);
            Me.CastWithDelay(TimeSpan.FromMilliseconds(2000), Me, Spells.ARCANE_OVERLOAD_SUICIDE, true);
            Me.CastWithDelay(TimeSpan.FromMilliseconds(1000), Me, Spells.ARCANE_OVERLOAD_BOOM, true);
        }
    }


    public override void AttackStart(Unit victim)
    {
        if (Me.HasUnitState(UnitState.Casting))
            AttackStartNoMove(victim);
        else
            base.AttackStart(victim);
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Events.Update(diff);

        if (Me.HasUnitState(UnitState.Casting) && !Me.GetCurrentSpell(CurrentSpellTypes.Channeled))
            return;

        var eventId = Events.ExecuteEvent();

        if (eventId != 0)
            switch (eventId)
            {
                case BossEvents.EVENT_ELEMENTAL_FISTS:
                    if (_lastElement == false)
                    {
                        DoCast(Spells.FIST_OF_FROST);
                        Talk(Texts.SAY_FISTS_OF_FROST);
                    }
                    else
                    {
                        DoCast(Spells.FIST_OF_FLAME);
                        Talk(Texts.SAY_FISTS_OF_FLAME);
                    }

                    Me.ReactState = ReactStates.Aggressive;
                    AttackStart(Me.SelectVictim());
                    _lastElement = !_lastElement;
                    Events.ScheduleEvent(BossEvents.EVENT_BLINK, TimeSpan.FromMilliseconds(12000));

                    break;
                case BossEvents.EVENT_BLINK:
                    var random = SelectTarget(SelectTargetMethod.Random);

                    if (random != null)
                    {
                        DoCast(random, Spells.BLINK);
                        Me.ReactState = ReactStates.Passive;
                        Me.AttackStop();
                        Me.SetFacingToObject(random);

                        if (IsHeroic)
                            ResetThreatList();
                    }

                    Events.ScheduleEvent(BossEvents.EVENT_ELEMENTAL_FISTS, TimeSpan.FromMilliseconds(1000));

                    break;
                case BossEvents.EVENT_TRANSITION_SAY_1:
                    Talk(Texts.SAY_TRANSITION_1);

                    break;
                case BossEvents.EVENT_TRANSITION_SAY_2:
                    Talk(Texts.SAY_TRANSITION_2);

                    break;
                case BossEvents.EVENT_TRANSITION_CAST:
                {
                    Me.AddUnitState(UnitState.CannotTurn);
                    Talk(Texts.SAY_ARCANE_POWER);
                    DoCast(Spells.ARCANE_POWER);
                    SummonBeams();

                    var init = new MoveSplineInit(Me);
                    init.MoveTo(Me.Location.X, Me.Location.Y, Me.Location.Z + 2.0f);
                    init.SetVelocity(1.5f);
                    init.Launch();
                    Me.SetDisableGravity(true);

                    _transitionDone = true;

                    if (IsHeroic)
                    {
                        _platter.AI.DoAction(Actions.ACTION_STOP_FIREWALL);
                        _platter.AI.DoAction(Actions.ACTION_START_FIREWALL);
                        Events.ScheduleEvent(BossEvents.EVENT_SAY_FIREWALL, TimeSpan.FromMilliseconds(3500));
                    }

                    Events.ScheduleEvent(BossEvents.EVENT_SUMMON_BLOSSOM, TimeSpan.FromMilliseconds(4000));

                    break;
                }
                case BossEvents.EVENT_SUMMON_BLOSSOM:
                {
                    Events.ScheduleEvent(BossEvents.EVENT_SUMMON_BLOSSOM, TimeSpan.FromMilliseconds(RandomHelper.URand(0, 2500)));

                    uint targetEntry = 0;
                    uint targetSpellID = 0;
                    uint indicatorSpellID = 0;

                    if (RandomHelper.randChance(50))
                    {
                        targetEntry = (uint)Creatures.NPC_FROST_BLOSSOM_DUMMY;
                        targetSpellID = (uint)Spells.FROST_BLOSSOM;
                        indicatorSpellID = (uint)Spells.FROST_BLOSSOM_VISUAL;
                    }
                    else
                    {
                        targetEntry = (uint)Creatures.NPC_FIRE_BLOSSOM_DUMMY;
                        targetSpellID = (uint)Spells.FIRE_BLOSSOM;
                        indicatorSpellID = (uint)Spells.FIRE_BLOSSOM_VISUAL;
                    }

                    var cList = Me.GetCreatureListWithEntryInGrid(targetEntry, 100.0f);

                    cList.RemoveIf(new UnitAuraCheck<Creature>(true, indicatorSpellID));

                    if (cList.Count == 0)
                        break;


                    var target = cList.SelectRandom();

                    if (target == null)
                    {
                        target.SpellFactory.CastSpell(target, indicatorSpellID, true);
                        Me.SpellFactory.CastSpell(target, targetSpellID);
                    }

                    break;
                }
                case BossEvents.EVENT_SAY_FIREWALL:
                    Talk(Texts.SAY_SUMMON_FIRE_WALL);

                    break;
                case BossEvents.EVENT_FALL_GROUND:
                    Me.ClearUnitState(UnitState.CannotTurn);
                    Me.MotionMaster.MoveFall(Points.POINT_FALL_GROUND);

                    break;
            }

        DoMeleeAttackIfReady();
    }

    public struct BossEvents
    {
        public const uint EVENT_ELEMENTAL_FISTS = 1;
        public const uint EVENT_BLINK = 2;

        public const uint EVENT_TRANSITION_SAY_1 = 3;
        public const uint EVENT_TRANSITION_SAY_2 = 4;
        public const uint EVENT_TRANSITION_CAST = 5;
        public const uint EVENT_SAY_FIREWALL = 6;
        public const uint EVENT_SUMMON_BLOSSOM = 7;

        public const uint EVENT_FALL_GROUND = 8;
        public const uint EVENT_DIE = 9;

        //Main Platter
        public const uint EVENT_START_PART_1 = 10;
        public const uint EVENT_START_PART_2 = 11;
        public const uint EVENT_START_PART_3 = 12;
    }

    public struct Actions
    {
        public const int ACTION_START_FIREWALL = 1;
        public const int ACTION_STOP_FIREWALL = 2;
    }

    public struct Points
    {
        public const uint POINT_FALL_GROUND = 1;
    }

    public struct Texts
    {
        public const uint SAY_AGGRO = 0;
        public const uint SAY_DEATH = 1;
        public const uint SAY_KILL = 2;
        public const uint SAY_FISTS_OF_FROST = 3;
        public const uint SAY_FISTS_OF_FLAME = 4;
        public const uint SAY_TRANSITION_1 = 5;
        public const uint SAY_TRANSITION_2 = 6;
        public const uint SAY_ARCANE_POWER = 7;
        public const uint SAY_SUMMON_FIRE_WALL = 8;
    }

    public struct Spells
    {
        public const uint FIRE_BLOSSOM = 88129;
        public const uint FIRE_BLOSSOM_VISUAL = 88164;
        public const uint FROST_BLOSSOM = 88169;
        public const uint FROST_BLOSSOM_VISUAL = 88165;
        public const uint TELEPORT_VISUAL = 88002;
        public const uint ARCANE_POWER = 88009;
        public const uint FIST_OF_FLAME = 87859;
        public const uint FIST_OF_FROST = 87861;
        public const uint BLINK = 87925;

        public const uint BLOSSOM_TARGETTING = 88140;

        public const uint ARCANE_FIRE = 88007;
        public const uint ARCANE_FIRE_BEAM = 88072;
        public const uint ARCANE_FROST_BEAM = 88093;
        public const uint TRIGGER_FIRE_WALL = 91398;
        public const uint FIRE_WALL_TRIGGERED = 91397;

        public const uint ARCANE_OVERLOAD_INITIAL = 88183;
        public const uint FEIGN_DEATH = 70628;
        public const uint ARCANE_OVERLOAD_SUICIDE = 88185;
        public const uint ARCANE_OVERLOAD_BOOM = 90520;
    }

    public struct Creatures
    {
        public const uint NPC_GLUBTOK_MAIN_PLATTER = 48974;
        public const uint NPC_FROST_BLOSSOM_DUMMY = 47284;
        public const uint NPC_FIRE_BLOSSOM_DUMMY = 47282;

        public const uint NPC_BEAM_BUNNY = 47242;
    }
}