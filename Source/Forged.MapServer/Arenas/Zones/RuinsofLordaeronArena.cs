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

internal class RuinsofLordaeronArena : Arena
{
    public RuinsofLordaeronArena(BattlegroundTemplate battlegroundTemplate, WorldManager worldManager, BattlegroundManager battlegroundManager, ObjectAccessor objectAccessor, GameObjectManager objectManager,
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
            case 4696: // buff trigger?
            case 4697: // buff trigger?
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
        result &= AddObject(RuinsofLordaeronObjectTypes.DOOR1, RuinsofLordaeronObjectTypes.DOOR1, 1293.561f, 1601.938f, 31.60557f, -1.457349f, 0, 0, -0.6658813f, 0.7460576f);
        result &= AddObject(RuinsofLordaeronObjectTypes.DOOR2, RuinsofLordaeronObjectTypes.DOOR2, 1278.648f, 1730.557f, 31.60557f, 1.684245f, 0, 0, 0.7460582f, 0.6658807f);

        if (!result)
        {
            Log.Logger.Error("RuinsofLordaeronArena: Failed to spawn door object!");

            return false;
        }

        result &= AddObject(RuinsofLordaeronObjectTypes.BUFF1, RuinsofLordaeronObjectTypes.BUFF1, 1328.719971f, 1632.719971f, 36.730400f, -1.448624f, 0, 0, 0.6626201f, -0.7489557f, 120);
        result &= AddObject(RuinsofLordaeronObjectTypes.BUFF2, RuinsofLordaeronObjectTypes.BUFF2, 1243.300049f, 1699.170044f, 34.872601f, -0.06981307f, 0, 0, 0.03489945f, -0.9993908f, 120);

        if (!result)
        {
            Log.Logger.Error("RuinsofLordaeronArena: Failed to spawn buff object!");

            return false;
        }

        return true;
    }

    public override void StartingEventCloseDoors()
    {
        for (var i = RuinsofLordaeronObjectTypes.DOOR1; i <= RuinsofLordaeronObjectTypes.DOOR2; ++i)
            SpawnBGObject(i, BattlegroundConst.RESPAWN_IMMEDIATELY);
    }

    public override void StartingEventOpenDoors()
    {
        for (var i = RuinsofLordaeronObjectTypes.DOOR1; i <= RuinsofLordaeronObjectTypes.DOOR2; ++i)
            DoorOpen(i);

        TaskScheduler.Schedule(TimeSpan.FromSeconds(5),
                               _ =>
                               {
                                   for (var i = RuinsofLordaeronObjectTypes.DOOR1; i <= RuinsofLordaeronObjectTypes.DOOR2; ++i)
                                       DelObject(i);
                               });

        for (var i = RuinsofLordaeronObjectTypes.BUFF1; i <= RuinsofLordaeronObjectTypes.BUFF2; ++i)
            SpawnBGObject(i, 60);
    }
}