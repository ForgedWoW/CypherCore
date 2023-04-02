﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Arenas.Zones;

internal struct BladeEdgeObjectTypes
{
    public const int Buff1 = 4;
    public const int Buff2 = 5;
    public const int Door1 = 0;
    public const int Door2 = 1;
    public const int Door3 = 2;
    public const int Door4 = 3;
    public const int Max = 6;
}

public class BladesEdgeArena : Arena
{
    public BladesEdgeArena(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate) { }

    public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        if (GetStatus() != BattlegroundStatus.InProgress)
            return;

        switch (trigger)
        {
            case 4538: // buff trigger?
            case 4539: // buff trigger?
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

        taskScheduler.Update(diff);
    }

    public override bool SetupBattleground()
    {
        var result = true;
        result &= AddObject(BladeEdgeObjectTypes.Door1, BladeEfgeGameObjects.Door1, 6287.277f, 282.1877f, 3.810925f, -2.260201f, 0, 0, 0.9044551f, -0.4265689f);
        result &= AddObject(BladeEdgeObjectTypes.Door2, BladeEfgeGameObjects.Door2, 6189.546f, 241.7099f, 3.101481f, 0.8813917f, 0, 0, 0.4265689f, 0.9044551f);
        result &= AddObject(BladeEdgeObjectTypes.Door3, BladeEfgeGameObjects.Door3, 6299.116f, 296.5494f, 3.308032f, 0.8813917f, 0, 0, 0.4265689f, 0.9044551f);
        result &= AddObject(BladeEdgeObjectTypes.Door4, BladeEfgeGameObjects.Door4, 6177.708f, 227.3481f, 3.604374f, -2.260201f, 0, 0, 0.9044551f, -0.4265689f);

        if (!result)
        {
            Log.Logger.Error("BatteGroundBE: Failed to spawn door object!");

            return false;
        }

        result &= AddObject(BladeEdgeObjectTypes.Buff1, BladeEfgeGameObjects.Buff1, 6249.042f, 275.3239f, 11.22033f, -1.448624f, 0, 0, 0.6626201f, -0.7489557f, 120);
        result &= AddObject(BladeEdgeObjectTypes.Buff2, BladeEfgeGameObjects.Buff2, 6228.26f, 249.566f, 11.21812f, -0.06981307f, 0, 0, 0.03489945f, -0.9993908f, 120);

        if (!result)
        {
            Log.Logger.Error("BladesEdgeArena: Failed to spawn buff object!");

            return false;
        }

        return true;
    }

    public override void StartingEventCloseDoors()
    {
        for (var i = BladeEdgeObjectTypes.Door1; i <= BladeEdgeObjectTypes.Door4; ++i)
            SpawnBGObject(i, BattlegroundConst.RespawnImmediately);

        for (var i = BladeEdgeObjectTypes.Buff1; i <= BladeEdgeObjectTypes.Buff2; ++i)
            SpawnBGObject(i, BattlegroundConst.RespawnOneDay);
    }

    public override void StartingEventOpenDoors()
    {
        for (var i = BladeEdgeObjectTypes.Door1; i <= BladeEdgeObjectTypes.Door4; ++i)
            DoorOpen(i);

        taskScheduler.Schedule(TimeSpan.FromSeconds(5),
                               task =>
                               {
                                   for (var i = BladeEdgeObjectTypes.Door1; i <= BladeEdgeObjectTypes.Door2; ++i)
                                       DelObject(i);
                               });

        for (var i = BladeEdgeObjectTypes.Buff1; i <= BladeEdgeObjectTypes.Buff2; ++i)
            SpawnBGObject(i, 60);
    }
}
internal struct BladeEfgeGameObjects
{
    public const uint Buff1 = 184663;
    public const uint Buff2 = 184664;
    public const uint Door1 = 183971;
    public const uint Door2 = 183973;
    public const uint Door3 = 183970;
    public const uint Door4 = 183972;
}