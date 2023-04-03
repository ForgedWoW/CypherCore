// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Arenas.Zones;

public class NagrandArena : Arena
{
    public NagrandArena(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
    {
    }

    public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        if (GetStatus() != BattlegroundStatus.InProgress)
            return;

        switch (trigger)
        {
            case 4536: // buff trigger?
            case 4537: // buff trigger?
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

        TaskScheduler.Update(diff);
    }

    public override bool SetupBattleground()
    {
        var result = true;
        result &= AddObject(NagrandArenaObjectTypes.DOOR1, NagrandArenaObjects.DOOR1, 4031.854f, 2966.833f, 12.6462f, -2.648788f, 0, 0, 0.9697962f, -0.2439165f);
        result &= AddObject(NagrandArenaObjectTypes.DOOR2, NagrandArenaObjects.DOOR2, 4081.179f, 2874.97f, 12.39171f, 0.4928045f, 0, 0, 0.2439165f, 0.9697962f);
        result &= AddObject(NagrandArenaObjectTypes.DOOR3, NagrandArenaObjects.DOOR3, 4023.709f, 2981.777f, 10.70117f, -2.648788f, 0, 0, 0.9697962f, -0.2439165f);
        result &= AddObject(NagrandArenaObjectTypes.DOOR4, NagrandArenaObjects.DOOR4, 4090.064f, 2858.438f, 10.23631f, 0.4928045f, 0, 0, 0.2439165f, 0.9697962f);

        if (!result)
        {
            Log.Logger.Error("NagrandArena: Failed to spawn door object!");

            return false;
        }

        result &= AddObject(NagrandArenaObjectTypes.BUFF1, NagrandArenaObjects.BUFF1, 4009.189941f, 2895.250000f, 13.052700f, -1.448624f, 0, 0, 0.6626201f, -0.7489557f, 120);
        result &= AddObject(NagrandArenaObjectTypes.BUFF2, NagrandArenaObjects.BUFF2, 4103.330078f, 2946.350098f, 13.051300f, -0.06981307f, 0, 0, 0.03489945f, -0.9993908f, 120);

        if (!result)
        {
            Log.Logger.Error("NagrandArena: Failed to spawn buff object!");

            return false;
        }

        return true;
    }

    public override void StartingEventCloseDoors()
    {
        for (var i = NagrandArenaObjectTypes.DOOR1; i <= NagrandArenaObjectTypes.DOOR4; ++i)
            SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
    }

    public override void StartingEventOpenDoors()
    {
        for (var i = NagrandArenaObjectTypes.DOOR1; i <= NagrandArenaObjectTypes.DOOR4; ++i)
            DoorOpen(i);

        TaskScheduler.Schedule(TimeSpan.FromSeconds(5),
                               task =>
                               {
                                   for (var i = NagrandArenaObjectTypes.DOOR1; i <= NagrandArenaObjectTypes.DOOR2; ++i)
                                       DelObject(i);
                               });

        for (var i = NagrandArenaObjectTypes.BUFF1; i <= NagrandArenaObjectTypes.BUFF2; ++i)
            SpawnBGObject(i, 60);
    }
}