// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.BaradinHold.Alizabal;

internal struct SpellIds
{
    public const uint BLADE_DANCE = 105784;
    public const uint BLADE_DANCE_DUMMY = 105828;
    public const uint SEETHING_HATE = 105067;
    public const uint SKEWER = 104936;
    public const uint BERSERK = 47008;
}

internal struct TextIds
{
    public const uint SAY_INTRO = 1;
    public const uint SAY_AGGRO = 2;
    public const uint SAY_HATE = 3;
    public const uint SAY_SKEWER = 4;
    public const uint SAY_SKEWER_ANNOUNCE = 5;
    public const uint SAY_BLADE_STORM = 6;
    public const uint SAY_SLAY = 10;
    public const uint SAY_DEATH = 12;
}

internal struct ActionIds
{
    public const int INTRO = 1;
}

internal struct PointIds
{
    public const uint STORM = 1;
}

internal struct EventIds
{
    public const uint RANDOM_CAST = 1;
    public const uint STOP_STORM = 2;
    public const uint MOVE_STORM = 3;
    public const uint CAST_STORM = 4;
}

[Script]
internal class AtAlizabalIntro : ScriptObjectAutoAddDBBound, IAreaTriggerOnTrigger
{
    public AtAlizabalIntro() : base("at_alizabal_intro") { }

    public bool OnTrigger(Player player, AreaTriggerRecord areaTrigger)
    {
        var instance = player.InstanceScript;

        if (instance != null)
        {
            var alizabal = ObjectAccessor.GetCreature(player, instance.GetGuidData(DataTypes.ALIZABAL));

            if (alizabal)
                alizabal.AI.DoAction(ActionIds.INTRO);
        }

        return true;
    }
}

[Script]
internal class BossAlizabal : BossAI
{
    private bool _hate;
    private bool _intro;
    private bool _skewer;

    public BossAlizabal(Creature creature) : base(creature, DataTypes.ALIZABAL) { }

    public override void Reset()
    {
        _Reset();
        _hate = false;
        _skewer = false;
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Talk(TextIds.SAY_AGGRO);
        Instance.SendEncounterUnit(EncounterFrameType.Engage, Me);
        Events.ScheduleEvent(EventIds.RANDOM_CAST, TimeSpan.FromSeconds(10));
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        Talk(TextIds.SAY_DEATH);
        Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
    }

    public override void KilledUnit(Unit who)
    {
        if (who.IsPlayer)
            Talk(TextIds.SAY_SLAY);
    }

    public override void EnterEvadeMode(EvadeReason why)
    {
        Instance.SendEncounterUnit(EncounterFrameType.Disengage, Me);
        Me.MotionMaster.MoveTargetedHome();
        _DespawnAtEvade();
    }

    public override void DoAction(int action)
    {
        switch (action)
        {
            case ActionIds.INTRO:
                if (!_intro)
                {
                    Talk(TextIds.SAY_INTRO);
                    _intro = true;
                }

                break;
        }
    }

    public override void MovementInform(MovementGeneratorType type, uint pointId)
    {
        switch (pointId)
        {
            case PointIds.STORM:
                Events.ScheduleEvent(EventIds.CAST_STORM, TimeSpan.FromMilliseconds(1));

                break;
        }
    }

    public override void UpdateAI(uint diff)
    {
        if (!UpdateVictim())
            return;

        Events.Update(diff);

        Events.ExecuteEvents(eventId =>
        {
            switch (eventId)
            {
                case EventIds.RANDOM_CAST:
                {
                    switch (RandomHelper.URand(0, 1))
                    {
                        case 0:
                            if (!_skewer)
                            {
                                var target = SelectTarget(SelectTargetMethod.MaxThreat, 0);

                                if (target)
                                {
                                    DoCast(target, SpellIds.SKEWER, new CastSpellExtraArgs(true));
                                    Talk(TextIds.SAY_SKEWER);
                                    Talk(TextIds.SAY_SKEWER_ANNOUNCE, target);
                                }

                                _skewer = true;
                                Events.ScheduleEvent(EventIds.RANDOM_CAST, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                            }
                            else if (!_hate)
                            {
                                var target = SelectTarget(SelectTargetMethod.Random, 0, new NonTankTargetSelector(Me));

                                if (target)
                                {
                                    DoCast(target, SpellIds.SEETHING_HATE, new CastSpellExtraArgs(true));
                                    Talk(TextIds.SAY_HATE);
                                }

                                _hate = true;
                                Events.ScheduleEvent(EventIds.RANDOM_CAST, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                            }
                            else if (_hate && _skewer)
                            {
                                Talk(TextIds.SAY_BLADE_STORM);
                                DoCastAOE(SpellIds.BLADE_DANCE_DUMMY);
                                DoCastAOE(SpellIds.BLADE_DANCE);
                                Events.ScheduleEvent(EventIds.RANDOM_CAST, TimeSpan.FromSeconds(21));
                                Events.ScheduleEvent(EventIds.MOVE_STORM, TimeSpan.FromMilliseconds(4050));
                                Events.ScheduleEvent(EventIds.STOP_STORM, TimeSpan.FromSeconds(13));
                            }

                            break;
                        case 1:
                            if (!_hate)
                            {
                                var target = SelectTarget(SelectTargetMethod.Random, 0, new NonTankTargetSelector(Me));

                                if (target)
                                {
                                    DoCast(target, SpellIds.SEETHING_HATE, new CastSpellExtraArgs(true));
                                    Talk(TextIds.SAY_HATE);
                                }

                                _hate = true;
                                Events.ScheduleEvent(EventIds.RANDOM_CAST, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                            }
                            else if (!_skewer)
                            {
                                var target = SelectTarget(SelectTargetMethod.MaxThreat, 0);

                                if (target)
                                {
                                    DoCast(target, SpellIds.SKEWER, new CastSpellExtraArgs(true));
                                    Talk(TextIds.SAY_SKEWER);
                                    Talk(TextIds.SAY_SKEWER_ANNOUNCE, target);
                                }

                                _skewer = true;
                                Events.ScheduleEvent(EventIds.RANDOM_CAST, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(10));
                            }
                            else if (_hate && _skewer)
                            {
                                Talk(TextIds.SAY_BLADE_STORM);
                                DoCastAOE(SpellIds.BLADE_DANCE_DUMMY);
                                DoCastAOE(SpellIds.BLADE_DANCE);
                                Events.ScheduleEvent(EventIds.RANDOM_CAST, TimeSpan.FromSeconds(21));
                                Events.ScheduleEvent(EventIds.MOVE_STORM, TimeSpan.FromMilliseconds(4050));
                                Events.ScheduleEvent(EventIds.STOP_STORM, TimeSpan.FromSeconds(13));
                            }

                            break;
                    }

                    break;
                }
                case EventIds.MOVE_STORM:
                {
                    Me.SetSpeedRate(UnitMoveType.Run, 4.0f);
                    Me.SetSpeedRate(UnitMoveType.Walk, 4.0f);
                    var target = SelectTarget(SelectTargetMethod.Random, 0, new NonTankTargetSelector(Me));

                    if (target)
                        Me.MotionMaster.MovePoint(PointIds.STORM, target.Location.X, target.Location.Y, target.Location.Z);

                    Events.ScheduleEvent(EventIds.MOVE_STORM, TimeSpan.FromMilliseconds(4050));

                    break;
                }
                case EventIds.STOP_STORM:
                    Me.RemoveAura(SpellIds.BLADE_DANCE);
                    Me.RemoveAura(SpellIds.BLADE_DANCE_DUMMY);
                    Me.SetSpeedRate(UnitMoveType.Walk, 1.0f);
                    Me.SetSpeedRate(UnitMoveType.Run, 1.14f);
                    Me.MotionMaster.MoveChase(Me.Victim);
                    _hate = false;
                    _skewer = false;

                    break;
                case EventIds.CAST_STORM:
                    DoCastAOE(SpellIds.BLADE_DANCE);

                    break;
            }
        });

        DoMeleeAttackIfReady();
    }
}