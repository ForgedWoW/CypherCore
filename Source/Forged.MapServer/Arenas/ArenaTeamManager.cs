// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Arenas;

public class ArenaTeamManager
{
    private readonly Dictionary<uint, ArenaTeam> _arenaTeamStorage = new();
    private readonly CharacterDatabase _characterDatabase;
    private uint _nextArenaTeamId;

    public ArenaTeamManager(CharacterDatabase characterDatabase)
    {
        _characterDatabase = characterDatabase;
        _nextArenaTeamId = 1;
    }

    public void AddArenaTeam(ArenaTeam arenaTeam)
    {
        _arenaTeamStorage.TryAdd(arenaTeam.GetId(), arenaTeam);
    }

    public uint GenerateArenaTeamId()
    {
        return _nextArenaTeamId++;
    }

    public ArenaTeam GetArenaTeamByCaptain(ObjectGuid guid)
    {
        foreach (var (_, team) in _arenaTeamStorage)
            if (team.GetCaptain() == guid)
                return team;

        return null;
    }

    public ArenaTeam GetArenaTeamById(uint arenaTeamId)
    {
        return _arenaTeamStorage.LookupByKey(arenaTeamId);
    }

    public ArenaTeam GetArenaTeamByName(string arenaTeamName)
    {
        var search = arenaTeamName.ToLower();

        foreach (var (_, team) in _arenaTeamStorage)
            if (search == team.GetName().ToLower())
                return team;

        return null;
    }
    public Dictionary<uint, ArenaTeam> GetArenaTeamMap()
    {
        return _arenaTeamStorage;
    }

    public void LoadArenaTeams()
    {
        var oldMSTime = Time.MSTime;

        // Clean out the trash before loading anything
        _characterDatabase.DirectExecute("DELETE FROM arena_team_member WHERE arenaTeamId NOT IN (SELECT arenaTeamId FROM arena_team)"); // One-time query

        //                                                        0        1         2         3          4              5            6            7           8
        var result = _characterDatabase.Query("SELECT arenaTeamId, name, captainGuid, type, backgroundColor, emblemStyle, emblemColor, borderStyle, borderColor, " +
                                              //      9        10        11         12           13       14
                                              "rating, weekGames, weekWins, seasonGames, seasonWins, `rank` FROM arena_team ORDER BY arenaTeamId ASC");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 arena teams. DB table `arena_team` is empty!");

            return;
        }

        var result2 = _characterDatabase.Query(
                                               //              0              1           2             3              4                 5          6     7          8                  9
                                               "SELECT arenaTeamId, atm.guid, atm.weekGames, atm.weekWins, atm.seasonGames, atm.seasonWins, c.name, class, personalRating, matchMakerRating FROM arena_team_member atm" +
                                               " INNER JOIN arena_team ate USING (arenaTeamId) LEFT JOIN characters AS c ON atm.guid = c.guid" +
                                               " LEFT JOIN character_arena_stats AS cas ON c.guid = cas.guid AND (cas.slot = 0 AND ate.type = 2 OR cas.slot = 1 AND ate.type = 3 OR cas.slot = 2 AND ate.type = 5)" +
                                               " ORDER BY atm.arenateamid ASC");

        uint count = 0;

        do
        {
            ArenaTeam newArenaTeam = new();

            if (!newArenaTeam.LoadArenaTeamFromDB(result) || !newArenaTeam.LoadMembersFromDB(result2))
            {
                newArenaTeam.Disband(null);

                continue;
            }

            AddArenaTeam(newArenaTeam);

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} arena teams in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void RemoveArenaTeam(uint arenaTeamId)
    {
        _arenaTeamStorage.Remove(arenaTeamId);
    }
    public void SetNextArenaTeamId(uint id)
    {
        _nextArenaTeamId = id;
    }
}