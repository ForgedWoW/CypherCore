// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Guilds;
using Forged.MapServer.Miscellaneous;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Arenas.Zones;

public class BladesEdgeArena : Arena
{
    public BladesEdgeArena(BattlegroundTemplate battlegroundTemplate, WorldManager worldManager, BattlegroundManager battlegroundManager, ObjectAccessor objectAccessor, GameObjectManager objectManager,
                           CreatureFactory creatureFactory, GameObjectFactory gameObjectFactory, ClassFactory classFactory, IConfiguration configuration, CharacterDatabase characterDatabase,
                           GuildManager guildManager, Formulas formulas, PlayerComputators playerComputators, DB6Storage<FactionRecord> factionStorage, DB6Storage<BroadcastTextRecord> broadcastTextRecords,
                           CreatureTextManager creatureTextManager, WorldStateManager worldStateManager, ArenaTeamManager arenaTeamManager) :
        base(battlegroundTemplate, worldManager, battlegroundManager, objectAccessor, objectManager, creatureFactory, gameObjectFactory, classFactory, configuration, characterDatabase,
             guildManager, formulas, playerComputators, factionStorage, broadcastTextRecords, creatureTextManager, worldStateManager, arenaTeamManager)
    { }

    public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        if (Status != BattlegroundStatus.InProgress)
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
        if (Status != BattlegroundStatus.InProgress)
            return;

        TaskScheduler.Update(diff);
    }

    public override bool SetupBattleground()
    {
        var result = true;
        result &= AddObject(BladeEdgeObjectTypes.DOOR1, BladeEfgeGameObjects.DOOR1, 6287.277f, 282.1877f, 3.810925f, -2.260201f, 0, 0, 0.9044551f, -0.4265689f);
        result &= AddObject(BladeEdgeObjectTypes.DOOR2, BladeEfgeGameObjects.DOOR2, 6189.546f, 241.7099f, 3.101481f, 0.8813917f, 0, 0, 0.4265689f, 0.9044551f);
        result &= AddObject(BladeEdgeObjectTypes.DOOR3, BladeEfgeGameObjects.DOOR3, 6299.116f, 296.5494f, 3.308032f, 0.8813917f, 0, 0, 0.4265689f, 0.9044551f);
        result &= AddObject(BladeEdgeObjectTypes.DOOR4, BladeEfgeGameObjects.DOOR4, 6177.708f, 227.3481f, 3.604374f, -2.260201f, 0, 0, 0.9044551f, -0.4265689f);

        if (!result)
        {
            Log.Logger.Error("BatteGroundBE: Failed to spawn door object!");

            return false;
        }

        result &= AddObject(BladeEdgeObjectTypes.BUFF1, BladeEfgeGameObjects.BUFF1, 6249.042f, 275.3239f, 11.22033f, -1.448624f, 0, 0, 0.6626201f, -0.7489557f, 120);
        result &= AddObject(BladeEdgeObjectTypes.BUFF2, BladeEfgeGameObjects.BUFF2, 6228.26f, 249.566f, 11.21812f, -0.06981307f, 0, 0, 0.03489945f, -0.9993908f, 120);

        if (!result)
        {
            Log.Logger.Error("BladesEdgeArena: Failed to spawn buff object!");

            return false;
        }

        return true;
    }

    public override void StartingEventCloseDoors()
    {
        for (var i = BladeEdgeObjectTypes.DOOR1; i <= BladeEdgeObjectTypes.DOOR4; ++i)
            SpawnBGObject(i, BattlegroundConst.RESPAWN_IMMEDIATELY);

        for (var i = BladeEdgeObjectTypes.BUFF1; i <= BladeEdgeObjectTypes.BUFF2; ++i)
            SpawnBGObject(i, BattlegroundConst.RESPAWN_ONE_DAY);
    }

    public override void StartingEventOpenDoors()
    {
        for (var i = BladeEdgeObjectTypes.DOOR1; i <= BladeEdgeObjectTypes.DOOR4; ++i)
            DoorOpen(i);

        TaskScheduler.Schedule(TimeSpan.FromSeconds(5),
                               task =>
                               {
                                   for (var i = BladeEdgeObjectTypes.DOOR1; i <= BladeEdgeObjectTypes.DOOR2; ++i)
                                       DelObject(i);
                               });

        for (var i = BladeEdgeObjectTypes.BUFF1; i <= BladeEdgeObjectTypes.BUFF2; ++i)
            SpawnBGObject(i, 60);
    }
}