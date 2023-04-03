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
                case RingofValorEvents.OpenFences:
                    // Open fire (only at GameInfo start)
                    for (byte i = RingofValorObjectTypes.Fire1; i <= RingofValorObjectTypes.Firedoor2; ++i)
                        DoorOpen(i);

                    _events.ScheduleEvent(RingofValorEvents.CloseFire, TimeSpan.FromSeconds(5));

                    break;

                case RingofValorEvents.CloseFire:
                    for (byte i = RingofValorObjectTypes.Fire1; i <= RingofValorObjectTypes.Firedoor2; ++i)
                        DoorClose(i);

                    // Fire got closed after five seconds, leaves twenty seconds before toggling pillars
                    _events.ScheduleEvent(RingofValorEvents.SwitchPillars, TimeSpan.FromSeconds(20));

                    break;

                case RingofValorEvents.SwitchPillars:
                    TogglePillarCollision(true);
                    _events.Repeat(TimeSpan.FromSeconds(25));

                    break;
            }
        });
    }

    public override bool SetupBattleground()
    {
        var result = true;
        result &= AddObject(RingofValorObjectTypes.Elevator1, RingofValorGameObjects.Elevator1, 763.536377f, -294.535767f, 0.505383f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Elevator2, RingofValorGameObjects.Elevator2, 763.506348f, -273.873352f, 0.505383f, 0.000000f, 0, 0, 0, 0);

        if (!result)
        {
            Log.Logger.Error("RingofValorArena: Failed to spawn elevator object!");

            return false;
        }

        result &= AddObject(RingofValorObjectTypes.Buff1, RingofValorGameObjects.Buff1, 735.551819f, -284.794678f, 28.276682f, 0.034906f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Buff2, RingofValorGameObjects.Buff2, 791.224487f, -284.794464f, 28.276682f, 2.600535f, 0, 0, 0, 0);

        if (!result)
        {
            Log.Logger.Error("RingofValorArena: Failed to spawn buff object!");

            return false;
        }

        result &= AddObject(RingofValorObjectTypes.Fire1, RingofValorGameObjects.Fire1, 743.543457f, -283.799469f, 28.286655f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Fire2, RingofValorGameObjects.Fire2, 782.971802f, -283.799469f, 28.286655f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Firedoor1, RingofValorGameObjects.Firedoor1, 743.711060f, -284.099609f, 27.542587f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Firedoor2, RingofValorGameObjects.Firedoor2, 783.221252f, -284.133362f, 27.535686f, 0.000000f, 0, 0, 0, 0);

        if (!result)
        {
            Log.Logger.Error("RingofValorArena: Failed to spawn fire/firedoor object!");

            return false;
        }

        result &= AddObject(RingofValorObjectTypes.Gear1, RingofValorGameObjects.Gear1, 763.664551f, -261.872986f, 26.686588f, 0.000000f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Gear2, RingofValorGameObjects.Gear2, 763.578979f, -306.146149f, 26.665222f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Pulley1, RingofValorGameObjects.Pulley1, 700.722290f, -283.990662f, 39.517582f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Pulley2, RingofValorGameObjects.Pulley2, 826.303833f, -283.996429f, 39.517582f, 0.000000f, 0, 0, 0, 0);

        if (!result)
        {
            Log.Logger.Error("RingofValorArena: Failed to spawn gear/pully object!");

            return false;
        }

        result &= AddObject(RingofValorObjectTypes.Pilar1, RingofValorGameObjects.Pilar1, 763.632385f, -306.162384f, 25.909504f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Pilar2, RingofValorGameObjects.Pilar2, 723.644287f, -284.493256f, 24.648525f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Pilar3, RingofValorGameObjects.Pilar3, 763.611145f, -261.856750f, 25.909504f, 0.000000f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.Pilar4, RingofValorGameObjects.Pilar4, 802.211609f, -284.493256f, 24.648525f, 0.000000f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PilarCollision1, RingofValorGameObjects.PilarCollision1, 763.632385f, -306.162384f, 30.639660f, 3.141593f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PilarCollision2, RingofValorGameObjects.PilarCollision2, 723.644287f, -284.493256f, 32.382710f, 0.000000f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PilarCollision3, RingofValorGameObjects.PilarCollision3, 763.611145f, -261.856750f, 30.639660f, 0.000000f, 0, 0, 0, 0);
        result &= AddObject(RingofValorObjectTypes.PilarCollision4, RingofValorGameObjects.PilarCollision4, 802.211609f, -284.493256f, 32.382710f, 3.141593f, 0, 0, 0, 0);

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
        SpawnBGObject(RingofValorObjectTypes.Buff1, 90);
        SpawnBGObject(RingofValorObjectTypes.Buff2, 90);
        // Elevators
        DoorOpen(RingofValorObjectTypes.Elevator1);
        DoorOpen(RingofValorObjectTypes.Elevator2);

        _events.ScheduleEvent(RingofValorEvents.OpenFences, TimeSpan.FromSeconds(20));

        // Should be false at first, TogglePillarCollision will do it.
        TogglePillarCollision(true);
    }

    private void TogglePillarCollision(bool enable)
    {
        // Toggle visual pillars, pulley, gear, and collision based on previous state
        for (var i = RingofValorObjectTypes.Pilar1; i <= RingofValorObjectTypes.Gear2; ++i)
            if (enable)
                DoorOpen(i);
            else
                DoorClose(i);

        for (byte i = RingofValorObjectTypes.Pilar2; i <= RingofValorObjectTypes.Pulley2; ++i)
            if (enable)
                DoorClose(i);
            else
                DoorOpen(i);

        for (byte i = RingofValorObjectTypes.Pilar1; i <= RingofValorObjectTypes.PilarCollision4; ++i)
        {
            var go = GetBGObject(i);

            if (go)
            {
                if (i >= RingofValorObjectTypes.PilarCollision1)
                {
                    var state = ((go.Template.Door.startOpen != 0) == enable) ? GameObjectState.Active : GameObjectState.Ready;
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