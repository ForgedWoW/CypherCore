// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Maps.Grids;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class GameTeleObjectCache : IObjectCache
{
    private readonly WorldDatabase _worldDatabase;
    private readonly GridDefines _gridDefines;
    public Dictionary<uint, GameTele> GameTeleStorage { get; set; } = new();

    public GameTeleObjectCache(WorldDatabase worldDatabase, GridDefines gridDefines)
    {
        _worldDatabase = worldDatabase;
        _gridDefines = gridDefines;
    }

    public bool AddGameTele(GameTele tele)
    {
        // find max id
        uint newId = 0;

        foreach (var itr in GameTeleStorage.Where(itr => itr.Key > newId))
            newId = itr.Key;

        // use next
        ++newId;

        GameTeleStorage[newId] = tele;

        var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.INS_GAME_TELE);

        stmt.AddValue(0, newId);
        stmt.AddValue(1, tele.PosX);
        stmt.AddValue(2, tele.PosY);
        stmt.AddValue(3, tele.PosZ);
        stmt.AddValue(4, tele.Orientation);
        stmt.AddValue(5, tele.MapId);
        stmt.AddValue(6, tele.Name);

        _worldDatabase.Execute(stmt);

        return true;
    }

    public bool DeleteGameTele(string name)
    {
        name = name.ToLowerInvariant();

        foreach (var pair in GameTeleStorage.ToList())
            if (pair.Value.NameLow == name)
            {
                var stmt = _worldDatabase.GetPreparedStatement(WorldStatements.DEL_GAME_TELE);
                stmt.AddValue(0, pair.Value.Name);
                _worldDatabase.Execute(stmt);

                GameTeleStorage.Remove(pair.Key);

                return true;
            }

        return false;
    }

    public GameTele GetGameTele(uint id)
    {
        return GameTeleStorage.LookupByKey(id);
    }

    public GameTele GetGameTele(string name)
    {
        name = name.ToLower();

        // Alternative first GameTele what contains wnameLow as substring in case no GameTele location found
        GameTele alt = null;

        foreach (var (_, tele) in GameTeleStorage)
            if (tele.NameLow == name)
                return tele;
            else if (alt == null && tele.NameLow.Contains(name))
                alt = tele;

        return alt;
    }

    public GameTele GetGameTeleExactName(string name)
    {
        name = name.ToLower();

        foreach (var (_, tele) in GameTeleStorage)
            if (tele.NameLow == name)
                return tele;

        return null;
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        GameTeleStorage.Clear();

        //                                          0       1           2           3           4        5     6
        var result = _worldDatabase.Query("SELECT id, position_x, position_y, position_z, orientation, map, name FROM game_tele");

        if (result.IsEmpty())
        {
            Log.Logger.Error("Loaded 0 GameTeleports. DB table `game_tele` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var id = result.Read<uint>(0);

            GameTele gt = new()
            {
                PosX = result.Read<float>(1),
                PosY = result.Read<float>(2),
                PosZ = result.Read<float>(3),
                Orientation = result.Read<float>(4),
                MapId = result.Read<uint>(5),
                Name = result.Read<string>(6)
            };

            gt.NameLow = gt.Name.ToLowerInvariant();

            if (!_gridDefines.IsValidMapCoord(gt.MapId, gt.PosX, gt.PosY, gt.PosZ, gt.Orientation))
            {
                Log.Logger.Error("Wrong position for id {0} (name: {1}) in `game_tele` table, ignoring.", id, gt.Name);

                continue;
            }

            GameTeleStorage.Add(id, gt);
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} GameTeleports in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
}