// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Framework.Dynamic;
using Serilog;

namespace Forged.MapServer.Arenas.Zones;

internal class RingofValorArena : Arena
{
    public RingofValorArena(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
    {
        _events = new EventMap();
    }

    public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        if (GetStatus() != BattlegroundStatus.InProgress)
            return;

        switch (trigger)
        {
            case 5224:
            case 5226:
            // fire was removed in 3.2.0
            case 5473:
            case 5474:
                break;

            default:
                base.HandleAreaTrigger(player, trigger, entered);

                break;
        }
    }

    public override void PostUpdateImpl(uint diff)
    {
        if (GetStatus() != BattlegroundStatus.InProgress)
            return;

        _events.Update(diff);

        _events.ExecuteEvents(eventId =>
        {
            switch (eventId)
            {
                case RingofValorEvents.OPEN_FENCES:
                    // Open fire (only at GameInfo start)
                    for (byte i = RingofValorObjectTypes.FIRE1; i <= RingofValorObjectTypes.FIREDOOR2; ++i)
                        DoorOpen(i);

                    _events.ScheduleEvent(RingofValorEvents.CLOSE_FIRE, TimeSpan.FromSeconds(5));

                    break;

                case RingofValorEvents.CLOSE_FIRE:
                    for (byte i = RingofValorObjectTypes.FIRE1; i <= RingofValorObjectTypes.FIREDOOR2; ++i)
                        DoorClose(i);

                    // Fire got closed after five seconds, leaves twenty seconds before toggling pillars
                    _events.ScheduleEvent(RingofValorEvents.SWITCH_PILLARS, TimeSpan.FromSeconds(20));

                    break;

                case RingofValorEvents.SWITCH_PILLARS:
                    TogglePillarCollision(true);
                    _events.Repeat(TimeSpan.FromSeconds(25));

                    break;
            }
        });
    }

    public override bool SetupBattleground()
    {
        var result = true;
        result &= AddObject(RingofValorObjectTypes.ELEVATOR1, RingofValorGameObjects.ELEVATOR1, 763.536377f, -294.535767f, 0.505383f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.ELEVATOR2, RingofValorGameObjects.ELEVATOR2, 763.506348f, -273.873352f, 0.505383f, 0.000000f, 0, 0, 0, 0);

        if (!result)
        {
            Log.Logger.Error("RingofValorArena: Failed to spawn elevator object!");

            return false;
        }

        result &= AddObject(RingofValorObjectTypes.BUFF1, RingofValorGameObjects.BUFF1, 735.551819f, -284.794678f, 28.276682f, 0.034906f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.BUFF2, RingofValorGameObjects.BUFF2, 791.224487f, -284.794464f, 28.276682f, 2.600535f, 0, 0, 0, 0);

        if (!result)
        {
            Log.Logger.Error("RingofValorArena: Failed to spawn buff object!");

            return false;
        }

        result &= AddObject(RingofValorObjectTypes.FIRE1, RingofValorGameObjects.FIRE1, 743.543457f, -283.799469f, 28.286655f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.FIRE2, RingofValorGameObjects.FIRE2, 782.971802f, -283.799469f, 28.286655f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.FIREDOOR1, RingofValorGameObjects.FIREDOOR1, 743.711060f, -284.099609f, 27.542587f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.FIREDOOR2, RingofValorGameObjects.FIREDOOR2, 783.221252f, -284.133362f, 27.535686f, 0.000000f, 0, 0, 0, 0);

        if (!result)
        {
            Log.Logger.Error("RingofValorArena: Failed to spawn fire/firedoor object!");

            return false;
        }

        result &= AddObject(RingofValorObjectTypes.GEAR1, RingofValorGameObjects.GEAR1, 763.664551f, -261.872986f, 26.686588f, 0.000000f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.GEAR2, RingofValorGameObjects.GEAR2, 763.578979f, -306.146149f, 26.665222f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PULLEY1, RingofValorGameObjects.PULLEY1, 700.722290f, -283.990662f, 39.517582f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PULLEY2, RingofValorGameObjects.PULLEY2, 826.303833f, -283.996429f, 39.517582f, 0.000000f, 0, 0, 0, 0);

        if (!result)
        {
            Log.Logger.Error("RingofValorArena: Failed to spawn gear/pully object!");

            return false;
        }

        result &= AddObject(RingofValorObjectTypes.PILAR1, RingofValorGameObjects.PILAR1, 763.632385f, -306.162384f, 25.909504f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PILAR2, RingofValorGameObjects.PILAR2, 723.644287f, -284.493256f, 24.648525f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PILAR3, RingofValorGameObjects.PILAR3, 763.611145f, -261.856750f, 25.909504f, 0.000000f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PILAR4, RingofValorGameObjects.PILAR4, 802.211609f, -284.493256f, 24.648525f, 0.000000f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PILAR_COLLISION1, RingofValorGameObjects.PILAR_COLLISION1, 763.632385f, -306.162384f, 30.639660f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PILAR_COLLISION2, RingofValorGameObjects.PILAR_COLLISION2, 723.644287f, -284.493256f, 32.382710f, 0.000000f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PILAR_COLLISION3, RingofValorGameObjects.PILAR_COLLISION3, 763.611145f, -261.856750f, 30.639660f, 0.000000f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PILAR_COLLISION4, RingofValorGameObjects.PILAR_COLLISION4, 802.211609f, -284.493256f, 32.382710f, 3.141593f, 0, 0, 0, 0);

        if (!result)
        {
            Log.Logger.Error("RingofValorArena: Failed to spawn pilar object!");

            return false;
        }

        return true;
    }

    public override void StartingEventOpenDoors()
    {
        // Buff respawn
        SpawnBGObject(RingofValorObjectTypes.BUFF1, 90);
        SpawnBGObject(RingofValorObjectTypes.BUFF2, 90);
        // Elevators
        DoorOpen(RingofValorObjectTypes.ELEVATOR1);
        DoorOpen(RingofValorObjectTypes.ELEVATOR2);

        _events.ScheduleEvent(RingofValorEvents.OPEN_FENCES, TimeSpan.FromSeconds(20));

        // Should be false at first, TogglePillarCollision will do it.
        TogglePillarCollision(true);
    }

    private void TogglePillarCollision(bool enable)
    {
        // Toggle visual pillars, pulley, gear, and collision based on previous state
        for (var i = RingofValorObjectTypes.PILAR1; i <= RingofValorObjectTypes.GEAR2; ++i)
            if (enable)
                DoorOpen(i);
            else
                DoorClose(i);

        for (byte i = RingofValorObjectTypes.PILAR2; i <= RingofValorObjectTypes.PULLEY2; ++i)
            if (enable)
                DoorClose(i);
            else
                DoorOpen(i);

        for (byte i = RingofValorObjectTypes.PILAR1; i <= RingofValorObjectTypes.PILAR_COLLISION4; ++i)
        {
            var go = GetBGObject(i);

            if (go)
            {
                if (i >= RingofValorObjectTypes.PILAR_COLLISION1)
                {
                    var state = go.Template.Door.startOpen != 0 == enable ? GameObjectState.Active : GameObjectState.Ready;
                    go.SetGoState(state);
                }

                foreach (var guid in GetPlayers().Keys)
                {
                    var player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        go.SendUpdateToPlayer(player);
                }
            }
        }
    }
}