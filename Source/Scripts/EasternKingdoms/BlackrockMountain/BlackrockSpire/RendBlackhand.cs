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

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockSpire.RendBlackhand;

internal struct SpellIds
{
    public const uint WHIRLWIND = 13736; // sniffed
    public const uint CLEAVE = 15284;
    public const uint MORTAL_STRIKE = 16856;
    public const uint FRENZY = 8269;
    public const uint KNOCKDOWN = 13360; // On spawn during Gyth fight
}

internal struct TextIds
{
    // Rend Blackhand
    public const uint SAY_BLACKHAND1 = 0;
    public const uint SAY_BLACKHAND2 = 1;

    public const uint EMOTE_BLACKHAND_DISMOUNT = 2;

    // Victor Nefarius
    public const uint SAY_NEFARIUS0 = 0;
    public const uint SAY_NEFARIUS1 = 1;
    public const uint SAY_NEFARIUS2 = 2;
    public const uint SAY_NEFARIUS3 = 3;
    public const uint SAY_NEFARIUS4 = 4;
    public const uint SAY_NEFARIUS5 = 5;
    public const uint SAY_NEFARIUS6 = 6;
    public const uint SAY_NEFARIUS7 = 7;
    public const uint SAY_NEFARIUS8 = 8;
    public const uint SAY_NEFARIUS9 = 9;
}

internal struct AddIds
{
    public const uint CHROMATIC_WHELP = 10442;
    public const uint CHROMATIC_DRAGONSPAWN = 10447;
    public const uint BLACKHAND_DRAGON_HANDLER = 10742;
}

internal struct MiscConst
{
    public const uint NEFARIUS_PATH1 = 1379670;
    public const uint NEFARIUS_PATH2 = 1379671;
    public const uint NEFARIUS_PATH3 = 1379672;
    public const uint REND_PATH1 = 1379680;
    public const uint REND_PATH2 = 1379681;

    public static Wave[] Wave2 = // 22 sec
    {
        new(10447, 209.8637f, -428.2729f, 110.9877f, 0.6632251f), new(10442, 209.3122f, -430.8724f, 110.9814f, 2.9147f), new(10442, 211.3309f, -425.9111f, 111.0006f, 1.727876f)
    };

    public static Wave[] Wave3 = // 60 sec
    {
        new(10742, 208.6493f, -424.5787f, 110.9872f, 5.8294f), new(10447, 203.9482f, -428.9446f, 110.982f, 4.677482f), new(10442, 203.3441f, -426.8668f, 110.9772f, 4.712389f), new(10442, 206.3079f, -424.7509f, 110.9943f, 4.08407f)
    };

    public static Wave[] Wave4 = // 49 sec
    {
        new(10742, 212.3541f, -412.6826f, 111.0352f, 5.88176f), new(10447, 212.5754f, -410.2841f, 111.0296f, 2.740167f), new(10442, 212.3449f, -414.8659f, 111.0348f, 2.356194f), new(10442, 210.6568f, -412.1552f, 111.0124f, 0.9773844f)
    };

    public static Wave[] Wave5 = // 60 sec
    {
        new(10742, 210.2188f, -410.6686f, 111.0211f, 5.8294f), new(10447, 209.4078f, -414.13f, 111.0264f, 4.677482f), new(10442, 208.0858f, -409.3145f, 111.0118f, 4.642576f), new(10442, 207.9811f, -413.0728f, 111.0098f, 5.288348f), new(10442, 208.0854f, -412.1505f, 111.0057f, 4.08407f)
    };

    public static Wave[] Wave6 = // 27 sec
    {
        new(10742, 213.9138f, -426.512f, 111.0013f, 3.316126f), new(10447, 213.7121f, -429.8102f, 110.9888f, 1.413717f), new(10447, 213.7157f, -424.4268f, 111.009f, 3.001966f), new(10442, 210.8935f, -423.913f, 111.0125f, 5.969026f), new(10442, 212.2642f, -430.7648f, 110.9807f, 5.934119f)
    };

    public static Position GythLoc = new(211.762f, -397.5885f, 111.1817f, 4.747295f);
    public static Position Teleport1Loc = new(194.2993f, -474.0814f, 121.4505f, -0.01225555f);
    public static Position Teleport2Loc = new(216.485f, -434.93f, 110.888f, -0.01225555f);
}

internal class Wave
{
    public uint Entry;
    public double OPos;
    public double XPos;
    public double YPos;
    public double ZPos;

    public Wave(uint entry, float x, float y, float z, float o)
    {
        Entry = entry;
        XPos = x;
        YPos = y;
        ZPos = z;
        OPos = o;
    }
}

internal struct EventIds
{
    public const uint START1 = 1;
    public const uint START2 = 2;
    public const uint START3 = 3;
    public const uint START4 = 4;
    public const uint TURN_TO_REND = 5;
    public const uint TURN_TO_PLAYER = 6;
    public const uint TURN_TO_FACING1 = 7;
    public const uint TURN_TO_FACING2 = 8;
    public const uint TURN_TO_FACING3 = 9;
    public const uint WAVE1 = 10;
    public const uint WAVE2 = 11;
    public const uint WAVE3 = 12;
    public const uint WAVE4 = 13;
    public const uint WAVE5 = 14;
    public const uint WAVE6 = 15;
    public const uint WAVES_TEXT1 = 16;
    public const uint WAVES_TEXT2 = 17;
    public const uint WAVES_TEXT3 = 18;
    public const uint WAVES_TEXT4 = 19;
    public const uint WAVES_TEXT5 = 20;
    public const uint WAVES_COMPLETE_TEXT1 = 21;
    public const uint WAVES_COMPLETE_TEXT2 = 22;
    public const uint WAVES_COMPLETE_TEXT3 = 23;
    public const uint WAVES_EMOTE1 = 24;
    public const uint WAVES_EMOTE2 = 25;
    public const uint PATH_REND = 26;
    public const uint PATH_NEFARIUS = 27;
    public const uint TELEPORT1 = 28;
    public const uint TELEPORT2 = 29;
    public const uint WHIRLWIND = 30;
    public const uint CLEAVE = 31;
    public const uint MORTAL_STRIKE = 32;
}

[Script]
internal class BossRendBlackhand : BossAI
{
    private bool _gythEvent;
    private ObjectGuid _portcullisGUID;
    private ObjectGuid _victorGUID;

    public BossRendBlackhand(Creature creature) : base(creature, DataTypes.WARCHIEF_REND_BLACKHAND) { }

    public override void Reset()
    {
        _Reset();
        _gythEvent = false;
        _victorGUID.Clear();
        _portcullisGUID.Clear();
    }

    public override void JustEngagedWith(Unit who)
    {
        base.JustEngagedWith(who);
        Events.ScheduleEvent(EventIds.WHIRLWIND, TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(15));
        Events.ScheduleEvent(EventIds.CLEAVE, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(17));
        Events.ScheduleEvent(EventIds.MORTAL_STRIKE, TimeSpan.FromSeconds(17), TimeSpan.FromSeconds(19));
    }

    public override void IsSummonedBy(WorldObject summoner)
    {
        Me.SetImmuneToPC(false);
        DoZoneInCombat();
    }

    public override void JustDied(Unit killer)
    {
        _JustDied();
        var victor = Me.FindNearestCreature(CreaturesIds.LORD_VICTOR_NEFARIUS, 75.0f, true);

        victor?.AI.SetData(1, 2);
    }

    public override void SetData(uint type, uint data)
    {
        if (type == BrsMiscConst.AREATRIGGER &&
            data == BrsMiscConst.AREATRIGGER_BLACKROCK_STADIUM)
            if (!_gythEvent)
            {
                _gythEvent = true;

                var victor = Me.FindNearestCreature(CreaturesIds.LORD_VICTOR_NEFARIUS, 5.0f, true);

                if (victor != null)
                    _victorGUID = victor.GUID;

                var portcullis = Me.FindNearestGameObject(GameObjectsIds.DR_PORTCULLIS, 50.0f);

                if (portcullis != null)
                    _portcullisGUID = portcullis.GUID;

                Events.ScheduleEvent(EventIds.TURN_TO_PLAYER, TimeSpan.FromSeconds(0));
                Events.ScheduleEvent(EventIds.START1, TimeSpan.FromSeconds(1));
            }
    }

    public override void MovementInform(MovementGeneratorType type, uint id)
    {
        if (type == MovementGeneratorType.Waypoint)
            switch (id)
            {
                case 5:
                    Events.ScheduleEvent(EventIds.TELEPORT1, TimeSpan.FromSeconds(2));

                    break;
                case 11:
                    var gyth = Me.FindNearestCreature(CreaturesIds.GYTH, 10.0f, true);

                    if (gyth)
                        gyth.AI.SetData(1, 1);

                    Me.DespawnOrUnsummon(TimeSpan.FromSeconds(1), TimeSpan.FromDays(7));

                    break;
            }
    }

    public override void UpdateAI(uint diff)
    {
        if (_gythEvent)
        {
            Events.Update(diff);

            Events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.START1:
                    {
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.AI.Talk(TextIds.SAY_NEFARIUS0);

                        Events.ScheduleEvent(EventIds.START2, TimeSpan.FromSeconds(4));

                        break;
                    }
                    case EventIds.START2:
                    {
                        Events.ScheduleEvent(EventIds.TURN_TO_PLAYER, TimeSpan.FromSeconds(0));
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.HandleEmoteCommand(Emote.OneshotPoint);

                        Events.ScheduleEvent(EventIds.START3, TimeSpan.FromSeconds(4));

                        break;
                    }
                    case EventIds.START3:
                    {
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.AI.Talk(TextIds.SAY_NEFARIUS1);

                        Events.ScheduleEvent(EventIds.WAVE1, TimeSpan.FromSeconds(2));
                        Events.ScheduleEvent(EventIds.TURN_TO_REND, TimeSpan.FromSeconds(4));
                        Events.ScheduleEvent(EventIds.WAVES_TEXT1, TimeSpan.FromSeconds(20));

                        break;
                    }
                    case EventIds.TURN_TO_REND:
                    {
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        if (victor != null)
                        {
                            victor.SetFacingToObject(Me);
                            victor.HandleEmoteCommand(Emote.OneshotTalk);
                        }

                        break;
                    }
                    case EventIds.TURN_TO_PLAYER:
                    {
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        if (victor != null)
                        {
                            Unit player = victor.SelectNearestPlayer(60.0f);

                            if (player != null)
                                victor.SetFacingToObject(player);
                        }

                        break;
                    }
                    case EventIds.TURN_TO_FACING1:
                    {
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.SetFacingTo(1.518436f);

                        break;
                    }
                    case EventIds.TURN_TO_FACING2:
                        Me.SetFacingTo(1.658063f);

                        break;
                    case EventIds.TURN_TO_FACING3:
                        Me.SetFacingTo(1.500983f);

                        break;
                    case EventIds.WAVES_EMOTE1:
                    {
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.HandleEmoteCommand(Emote.OneshotQuestion);

                        break;
                    }
                    case EventIds.WAVES_EMOTE2:
                        Me.HandleEmoteCommand(Emote.OneshotRoar);

                        break;
                    case EventIds.WAVES_TEXT1:
                    {
                        Events.ScheduleEvent(EventIds.TURN_TO_PLAYER, TimeSpan.FromSeconds(0));
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.AI.Talk(TextIds.SAY_NEFARIUS2);

                        Me.HandleEmoteCommand(Emote.OneshotTalk);
                        Events.ScheduleEvent(EventIds.TURN_TO_FACING1, TimeSpan.FromSeconds(4));
                        Events.ScheduleEvent(EventIds.WAVES_EMOTE1, TimeSpan.FromSeconds(5));
                        Events.ScheduleEvent(EventIds.WAVE2, TimeSpan.FromSeconds(2));
                        Events.ScheduleEvent(EventIds.WAVES_TEXT2, TimeSpan.FromSeconds(20));

                        break;
                    }
                    case EventIds.WAVES_TEXT2:
                    {
                        Events.ScheduleEvent(EventIds.TURN_TO_PLAYER, TimeSpan.FromSeconds(0));
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.AI.Talk(TextIds.SAY_NEFARIUS3);

                        Events.ScheduleEvent(EventIds.TURN_TO_FACING1, TimeSpan.FromSeconds(4));
                        Events.ScheduleEvent(EventIds.WAVE3, TimeSpan.FromSeconds(2));
                        Events.ScheduleEvent(EventIds.WAVES_TEXT3, TimeSpan.FromSeconds(20));

                        break;
                    }
                    case EventIds.WAVES_TEXT3:
                    {
                        Events.ScheduleEvent(EventIds.TURN_TO_PLAYER, TimeSpan.FromSeconds(0));
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.AI.Talk(TextIds.SAY_NEFARIUS4);

                        Events.ScheduleEvent(EventIds.TURN_TO_FACING1, TimeSpan.FromSeconds(4));
                        Events.ScheduleEvent(EventIds.WAVE4, TimeSpan.FromSeconds(2));
                        Events.ScheduleEvent(EventIds.WAVES_TEXT4, TimeSpan.FromSeconds(20));

                        break;
                    }
                    case EventIds.WAVES_TEXT4:
                        Talk(TextIds.SAY_BLACKHAND1);
                        Events.ScheduleEvent(EventIds.WAVES_EMOTE2, TimeSpan.FromSeconds(4));
                        Events.ScheduleEvent(EventIds.TURN_TO_FACING3, TimeSpan.FromSeconds(8));
                        Events.ScheduleEvent(EventIds.WAVE5, TimeSpan.FromSeconds(2));
                        Events.ScheduleEvent(EventIds.WAVES_TEXT5, TimeSpan.FromSeconds(20));

                        break;
                    case EventIds.WAVES_TEXT5:
                    {
                        Events.ScheduleEvent(EventIds.TURN_TO_PLAYER, TimeSpan.FromSeconds(0));
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.AI.Talk(TextIds.SAY_NEFARIUS5);

                        Events.ScheduleEvent(EventIds.TURN_TO_FACING1, TimeSpan.FromSeconds(4));
                        Events.ScheduleEvent(EventIds.WAVE6, TimeSpan.FromSeconds(2));
                        Events.ScheduleEvent(EventIds.WAVES_COMPLETE_TEXT1, TimeSpan.FromSeconds(20));

                        break;
                    }
                    case EventIds.WAVES_COMPLETE_TEXT1:
                    {
                        Events.ScheduleEvent(EventIds.TURN_TO_PLAYER, TimeSpan.FromSeconds(0));
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.AI.Talk(TextIds.SAY_NEFARIUS6);

                        Events.ScheduleEvent(EventIds.TURN_TO_FACING1, TimeSpan.FromSeconds(4));
                        Events.ScheduleEvent(EventIds.WAVES_COMPLETE_TEXT2, TimeSpan.FromSeconds(13));

                        break;
                    }
                    case EventIds.WAVES_COMPLETE_TEXT2:
                    {
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.AI.Talk(TextIds.SAY_NEFARIUS7);

                        Talk(TextIds.SAY_BLACKHAND2);
                        Events.ScheduleEvent(EventIds.PATH_REND, TimeSpan.FromSeconds(1));
                        Events.ScheduleEvent(EventIds.WAVES_COMPLETE_TEXT3, TimeSpan.FromSeconds(4));

                        break;
                    }
                    case EventIds.WAVES_COMPLETE_TEXT3:
                    {
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.AI.Talk(TextIds.SAY_NEFARIUS8);

                        Events.ScheduleEvent(EventIds.PATH_NEFARIUS, TimeSpan.FromSeconds(1));
                        Events.ScheduleEvent(EventIds.PATH_REND, TimeSpan.FromSeconds(1));

                        break;
                    }
                    case EventIds.PATH_NEFARIUS:
                    {
                        var victor = ObjectAccessor.GetCreature(Me, _victorGUID);

                        victor?.MotionMaster.MovePath(MiscConst.NEFARIUS_PATH1, true);

                        break;
                    }
                    case EventIds.PATH_REND:
                        Me.MotionMaster.MovePath(MiscConst.REND_PATH1, false);

                        break;
                    case EventIds.TELEPORT1:
                        Me.NearTeleportTo(194.2993f, -474.0814f, 121.4505f, -0.01225555f);
                        Events.ScheduleEvent(EventIds.TELEPORT2, TimeSpan.FromSeconds(50));

                        break;
                    case EventIds.TELEPORT2:
                        Me.NearTeleportTo(216.485f, -434.93f, 110.888f, -0.01225555f);
                        Me.SummonCreature(CreaturesIds.GYTH, 211.762f, -397.5885f, 111.1817f, 4.747295f);

                        break;
                    case EventIds.WAVE1:
                    {
                        var portcullis = ObjectAccessor.GetGameObject(Me, _portcullisGUID);

                        portcullis?.UseDoorOrButton();

                        // move wave
                        break;
                    }
                    case EventIds.WAVE2:
                    {
                        // spawn wave
                        var portcullis = ObjectAccessor.GetGameObject(Me, _portcullisGUID);

                        portcullis?.UseDoorOrButton();

                        // move wave
                        break;
                    }
                    case EventIds.WAVE3:
                    {
                        // spawn wave
                        var portcullis = ObjectAccessor.GetGameObject(Me, _portcullisGUID);

                        portcullis?.UseDoorOrButton();

                        // move wave
                        break;
                    }
                    case EventIds.WAVE4:
                    {
                        // spawn wave
                        var portcullis = ObjectAccessor.GetGameObject(Me, _portcullisGUID);

                        portcullis?.UseDoorOrButton();

                        // move wave
                        break;
                    }
                    case EventIds.WAVE5:
                    {
                        // spawn wave
                        var portcullis = ObjectAccessor.GetGameObject(Me, _portcullisGUID);

                        portcullis?.UseDoorOrButton();

                        // move wave
                        break;
                    }
                    case EventIds.WAVE6:
                    {
                        // spawn wave
                        var portcullis = ObjectAccessor.GetGameObject(Me, _portcullisGUID);

                        portcullis?.UseDoorOrButton();

                        // move wave
                        break;
                    }
                }
            });
        }

        if (!UpdateVictim())
            return;

        Events.Update(diff);

        if (Me.HasUnitState(UnitState.Casting))
            return;

        Events.ExecuteEvents(eventId =>
        {
            switch (eventId)
            {
                case EventIds.WHIRLWIND:
                    DoCast(SpellIds.WHIRLWIND);
                    Events.ScheduleEvent(EventIds.WHIRLWIND, TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(18));

                    break;
                case EventIds.CLEAVE:
                    DoCastVictim(SpellIds.CLEAVE);
                    Events.ScheduleEvent(EventIds.CLEAVE, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(14));

                    break;
                case EventIds.MORTAL_STRIKE:
                    DoCastVictim(SpellIds.MORTAL_STRIKE);
                    Events.ScheduleEvent(EventIds.MORTAL_STRIKE, TimeSpan.FromSeconds(14), TimeSpan.FromSeconds(16));

                    break;
            }

            if (Me.HasUnitState(UnitState.Casting))
                return;
        });

        DoMeleeAttackIfReady();
    }
}