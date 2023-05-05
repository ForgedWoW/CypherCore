// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Cache;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Arenas;

public class ArenaTeam
{
    private readonly ArenaTeamManager _arenaTeamManager;
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    private readonly IConfiguration _configuration;
    private readonly List<ArenaTeamMember> _members = new();
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private uint _backgroundColor;
    private uint _borderColor;
    private byte _borderStyle;
    private ObjectGuid _captainGuid;

    private uint _emblemColor;

    // ARGB format
    private byte _emblemStyle;

    // icon id
    // ARGB format
    // border image id
    // ARGB format
    private ArenaTeamStats _stats;

    private uint _teamId;
    private string _teamName;
    private byte _type;

    public ArenaTeam(IConfiguration configuration, ObjectAccessor objectAccessor, CharacterCache characterCache, CharacterDatabase characterDatabase, ArenaTeamManager arenaTeamManager, GameObjectManager objectManager)
    {
        _configuration = configuration;
        _objectAccessor = objectAccessor;
        _characterCache = characterCache;
        _characterDatabase = characterDatabase;
        _arenaTeamManager = arenaTeamManager;
        _objectManager = objectManager;
        _stats.Rating = _configuration.GetDefaultValue<ushort>("Arena.ArenaStartRating", 0);
    }

    public static byte GetSlotByType(uint type)
    {
        switch ((ArenaTypes)type)
        {
            case ArenaTypes.Team2V2: return 0;
            case ArenaTypes.Team3V3: return 1;
            case ArenaTypes.Team5V5: return 2;
        }

        Log.Logger.Error("FATAL: Unknown arena team type {0} for some arena team", type);

        return 0xFF;
    }

    public static byte GetTypeBySlot(byte slot)
    {
        switch (slot)
        {
            case 0: return (byte)ArenaTypes.Team2V2;
            case 1: return (byte)ArenaTypes.Team3V3;
            case 2: return (byte)ArenaTypes.Team5V5;
        }

        Log.Logger.Error("FATAL: Unknown arena team slot {0} for some arena team", slot);

        return 0xFF;
    }

    public bool AddMember(ObjectGuid playerGuid)
    {
        string playerName;
        PlayerClass playerClass;

        // Check if arena team is full (Can't have more than type * 2 players)
        if (GetMembersSize() >= GetArenaType() * 2)
            return false;

        // Get player name and class either from db or character cache
        CharacterCacheEntry characterInfo;
        var player = _objectAccessor.FindPlayer(playerGuid);

        if (player != null)
        {
            playerClass = player.Class;
            playerName = player.GetName();
        }
        else if ((characterInfo = _characterCache.GetCharacterCacheByGuid(playerGuid)) != null)
        {
            playerName = characterInfo.Name;
            playerClass = characterInfo.ClassId;
        }
        else
            return false;

        // Check if player is already in a similar arena team
        if ((player != null && player.GetArenaTeamId(GetSlot()) != 0) || _characterCache.GetCharacterArenaTeamIdByGuid(playerGuid, GetArenaType()) != 0)
        {
            Log.Logger.Debug("Arena: {0} {1} already has an arena team of type {2}", playerGuid.ToString(), playerName, GetArenaType());

            return false;
        }

        // Set player's personal rating
        uint personalRating = 0;

        if (_configuration.GetDefaultValue("Arena:ArenaStartPersonalRating", 1000) > 0)
            personalRating = _configuration.GetDefaultValue("Arena:ArenaStartPersonalRating", 1000u);
        else if (GetRating() >= 1000)
            personalRating = 1000;

        // Try to get player's match maker rating from db and fall back to config setting if not found
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_MATCH_MAKER_RATING);
        stmt.AddValue(0, playerGuid.Counter);
        stmt.AddValue(1, GetSlot());
        var result = _characterDatabase.Query(stmt);

        uint matchMakerRating;

        if (!result.IsEmpty())
            matchMakerRating = result.Read<ushort>(0);
        else
            matchMakerRating = _configuration.GetDefaultValue("Arena:ArenaStartMatchmakerRating", 1500u);

        // Remove all player signatures from other petitions
        // This will prevent player from joining too many arena teams and corrupt arena team data integrity
        //Player.RemovePetitionsAndSigns(playerGuid, GetArenaType());

        // Feed data to the struct
        ArenaTeamMember newMember = new()
        {
            Name = playerName,
            Guid = playerGuid,
            Class = (byte)playerClass,
            SeasonGames = 0,
            WeekGames = 0,
            SeasonWins = 0,
            WeekWins = 0,
            PersonalRating = (ushort)personalRating,
            MatchMakerRating = (ushort)matchMakerRating
        };

        _members.Add(newMember);
        _characterCache.UpdateCharacterArenaTeamId(playerGuid, GetSlot(), GetId());

        // Save player's arena team membership to db
        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_ARENA_TEAM_MEMBER);
        stmt.AddValue(0, _teamId);
        stmt.AddValue(1, playerGuid.Counter);
        stmt.AddValue(2, (ushort)personalRating);
        _characterDatabase.Execute(stmt);

        // Inform player if online
        if (player != null)
        {
            player.SetInArenaTeam(_teamId, GetSlot(), GetArenaType());
            player.SetArenaTeamIdInvited(0);

            // Hide promote/remove buttons
            if (_captainGuid != playerGuid)
                player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.Member, 1);
        }

        Log.Logger.Debug("Player: {0} [{1}] joined arena team type: {2} [Id: {3}, Name: {4}].", playerName, playerGuid.ToString(), GetArenaType(), GetId(), GetName());

        return true;
    }

    public bool Create(ObjectGuid captainGuid, byte type, string arenaTeamName, uint backgroundColor, byte emblemStyle, uint emblemColor, byte borderStyle, uint borderColor)
    {
        // Check if captain exists
        if (_characterCache.GetCharacterCacheByGuid(captainGuid) == null)
            return false;

        // Check if arena team name is already taken
        if (_arenaTeamManager.GetArenaTeamByName(arenaTeamName) != null)
            return false;

        // Generate new arena team id
        _teamId = _arenaTeamManager.GenerateArenaTeamId();

        // Assign member variables
        _captainGuid = captainGuid;
        _type = type;
        _teamName = arenaTeamName;
        _backgroundColor = backgroundColor;
        _emblemStyle = emblemStyle;
        _emblemColor = emblemColor;
        _borderStyle = borderStyle;
        _borderColor = borderColor;
        var captainLowGuid = captainGuid.Counter;

        // Save arena team to db
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_ARENA_TEAM);
        stmt.AddValue(0, _teamId);
        stmt.AddValue(1, _teamName);
        stmt.AddValue(2, captainLowGuid);
        stmt.AddValue(3, _type);
        stmt.AddValue(4, _stats.Rating);
        stmt.AddValue(5, _backgroundColor);
        stmt.AddValue(6, _emblemStyle);
        stmt.AddValue(7, _emblemColor);
        stmt.AddValue(8, _borderStyle);
        stmt.AddValue(9, _borderColor);
        _characterDatabase.Execute(stmt);

        // Add captain as member
        AddMember(_captainGuid);

        Log.Logger.Debug("New ArenaTeam created Id: {0}, Name: {1} Type: {2} Captain low GUID: {3}", GetId(), GetName(), GetArenaType(), captainLowGuid);

        return true;
    }

    public void DelMember(ObjectGuid guid, bool cleanDb)
    {
        // Remove member from team
        foreach (var member in _members)
            if (member.Guid == guid)
            {
                _members.Remove(member);
                _characterCache.UpdateCharacterArenaTeamId(guid, GetSlot(), 0);

                break;
            }

        // Remove arena team info from player data
        var player = _objectAccessor.FindPlayer(guid);

        if (player != null)
        {
            // delete all info regarding this team
            for (uint i = 0; i < (int)ArenaTeamInfoType.End; ++i)
                player.SetArenaTeamInfoField(GetSlot(), (ArenaTeamInfoType)i, 0);

            Log.Logger.Debug("Player: {0} [GUID: {1}] left arena team type: {2} [Id: {3}, Name: {4}].", player.GetName(), player.GUID.ToString(), GetArenaType(), GetId(), GetName());
        }

        // Only used for single member deletion, for arena team disband we use a single query for more efficiency
        if (!cleanDb)
            return;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM_MEMBER);
        stmt.AddValue(0, GetId());
        stmt.AddValue(1, guid.Counter);
        _characterDatabase.Execute(stmt);
    }

    public void Disband(WorldSession session)
    {
        // Broadcast update
        var player = session?.Player;

        if (player != null)
            Log.Logger.Debug("Player: {0} [GUID: {1}] disbanded arena team type: {2} [Id: {3}, Name: {4}].", player.GetName(), player.GUID.ToString(), GetArenaType(), GetId(), GetName());

        // Remove all members from arena team
        while (!_members.Empty())
            DelMember(_members.First().Guid, false);

        // Update database
        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM);
        stmt.AddValue(0, _teamId);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM_MEMBERS);
        stmt.AddValue(0, _teamId);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);

        // Remove arena team from ArenaTeamMgr
        _arenaTeamManager.RemoveArenaTeam(_teamId);
    }

    public void Disband()
    {
        // Remove all members from arena team
        while (!_members.Empty())
            DelMember(_members.First().Guid, false);

        // Update database
        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM);
        stmt.AddValue(0, _teamId);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_ARENA_TEAM_MEMBERS);
        stmt.AddValue(0, _teamId);
        trans.Append(stmt);

        _characterDatabase.CommitTransaction(trans);

        // Remove arena team from ArenaTeamMgr
        _arenaTeamManager.RemoveArenaTeam(_teamId);
    }

    public void FinishGame(int mod)
    {
        // Rating can only drop to 0
        if (_stats.Rating + mod < 0)
            _stats.Rating = 0;
        else
        {
            _stats.Rating += (ushort)mod;

            // Check if rating related achivements are met
            foreach (var member in _members)
                _objectAccessor.FindPlayer(member.Guid)?.UpdateCriteria(CriteriaType.EarnTeamArenaRating, _stats.Rating, _type);
        }

        // Update number of games played per season or week
        _stats.WeekGames += 1;
        _stats.SeasonGames += 1;

        // Update team's rank, start with rank 1 and increase until no team with more rating was found
        _stats.Rank = 1;

        foreach (var (_, team) in _arenaTeamManager.GetArenaTeamMap())
            if (team.GetArenaType() == _type && team.GetStats().Rating > _stats.Rating)
                ++_stats.Rank;
    }

    public bool FinishWeek()
    {
        // No need to go further than this
        if (_stats.WeekGames == 0)
            return false;

        // Reset team stats
        _stats.WeekGames = 0;
        _stats.WeekWins = 0;

        // Reset member stats
        foreach (var member in _members)
        {
            member.WeekGames = 0;
            member.WeekWins = 0;
        }

        return true;
    }

    public byte GetArenaType()
    {
        return _type;
    }

    public uint GetAverageMmr(PlayerGroup group)
    {
        if (group == null)
            return 0;

        uint matchMakerRating = 0;
        uint playerDivider = 0;

        foreach (var member in _members)
        {
            // Skip if player is not online
            if (_objectAccessor.FindPlayer(member.Guid) == null)
                continue;

            // Skip if player is not a member of group
            if (!group.IsMember(member.Guid))
                continue;

            matchMakerRating += member.MatchMakerRating;
            ++playerDivider;
        }

        // x/0 = crash
        if (playerDivider == 0)
            playerDivider = 1;

        matchMakerRating /= playerDivider;

        return matchMakerRating;
    }

    public ObjectGuid GetCaptain()
    {
        return _captainGuid;
    }

    public uint GetId()
    {
        return _teamId;
    }

    public ArenaTeamMember GetMember(string name)
    {
        foreach (var member in _members)
            if (member.Name == name)
                return member;

        return null;
    }

    public ArenaTeamMember GetMember(ObjectGuid guid)
    {
        foreach (var member in _members)
            if (member.Guid == guid)
                return member;

        return null;
    }

    public List<ArenaTeamMember> GetMembers()
    {
        return _members;
    }

    public int GetMembersSize()
    {
        return _members.Count;
    }

    public string GetName()
    {
        return _teamName;
    }

    public uint GetRating()
    {
        return _stats.Rating;
    }

    public byte GetSlot()
    {
        return GetSlotByType(GetArenaType());
    }

    public ArenaTeamStats GetStats()
    {
        return _stats;
    }

    public bool IsFighting()
    {
        foreach (var member in _members)
        {
            var player = _objectAccessor.FindPlayer(member.Guid);

            if (player == null)
                continue;

            if (player.Location.Map.IsBattleArena)
                return true;
        }

        return false;
    }

    public bool IsMember(ObjectGuid guid)
    {
        return _members.Any(member => member.Guid == guid);
    }

    public bool LoadArenaTeamFromDB(SQLResult result)
    {
        if (result.IsEmpty())
            return false;

        _teamId = result.Read<uint>(0);
        _teamName = result.Read<string>(1);
        _captainGuid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2));
        _type = result.Read<byte>(3);
        _backgroundColor = result.Read<uint>(4);
        _emblemStyle = result.Read<byte>(5);
        _emblemColor = result.Read<uint>(6);
        _borderStyle = result.Read<byte>(7);
        _borderColor = result.Read<uint>(8);
        _stats.Rating = result.Read<ushort>(9);
        _stats.WeekGames = result.Read<ushort>(10);
        _stats.WeekWins = result.Read<ushort>(11);
        _stats.SeasonGames = result.Read<ushort>(12);
        _stats.SeasonWins = result.Read<ushort>(13);
        _stats.Rank = result.Read<uint>(14);

        return true;
    }

    public bool LoadMembersFromDB(SQLResult result)
    {
        if (result.IsEmpty())
            return false;

        var captainPresentInTeam = false;

        do
        {
            var arenaTeamId = result.Read<uint>(0);

            // We loaded all members for this arena_team already, break cycle
            if (arenaTeamId > _teamId)
                break;

            ArenaTeamMember newMember = new()
            {
                Guid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1)),
                WeekGames = result.Read<ushort>(2),
                WeekWins = result.Read<ushort>(3),
                SeasonGames = result.Read<ushort>(4),
                SeasonWins = result.Read<ushort>(5),
                Name = result.Read<string>(6),
                Class = result.Read<byte>(7),
                PersonalRating = result.Read<ushort>(8),
                MatchMakerRating = (ushort)(result.Read<ushort>(9) > 0 ? result.Read<ushort>(9) : 1500)
            };

            // Delete member if character information is missing
            if (string.IsNullOrEmpty(newMember.Name))
            {
                Log.Logger.Error("ArenaTeam {0} has member with empty name - probably {1} doesn't exist, deleting him from memberlist!", arenaTeamId, newMember.Guid.ToString());
                DelMember(newMember.Guid, true);

                continue;
            }

            // Check if team team has a valid captain
            if (newMember.Guid == GetCaptain())
                captainPresentInTeam = true;

            // Put the player in the team
            _members.Add(newMember);
            _characterCache.UpdateCharacterArenaTeamId(newMember.Guid, GetSlot(), GetId());
        } while (result.NextRow());

        if (!_members.Empty() && captainPresentInTeam)
            return true;

        // Arena team is empty or captain is not in team, delete from db
        Log.Logger.Debug("ArenaTeam {0} does not have any members or its captain is not in team, disbanding it...", _teamId);

        return false;
    }

    public int LostAgainst(uint ownMmRating, uint opponentMmRating, ref int ratingChange)
    {
        // Called when the team has lost
        // Change in Matchmaker Rating
        var mod = GetMatchmakerRatingMod(ownMmRating, opponentMmRating, false);

        // Change in Team Rating
        ratingChange = GetRatingMod(_stats.Rating, opponentMmRating, false);

        // Modify the team stats accordingly
        FinishGame(ratingChange);

        // return the rating change, used to display it on the results screen
        return mod;
    }

    public void MemberLost(Player player, uint againstMatchmakerRating, int matchmakerRatingChange = -12)
    {
        // Called for each participant of a match after losing
        foreach (var member in _members)
            if (member.Guid == player.GUID)
            {
                // Update personal rating
                var mod = GetRatingMod(member.PersonalRating, againstMatchmakerRating, false);
                member.ModifyPersonalRating(player, mod, GetArenaType());

                // Update matchmaker rating
                member.ModifyMatchmakerRating(matchmakerRatingChange, GetSlot());

                // Update personal played stats
                member.WeekGames += 1;
                member.SeasonGames += 1;

                // update the unit fields
                player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesWeek, member.WeekGames);
                player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesSeason, member.SeasonGames);

                return;
            }
    }

    public void MemberWon(Player player, uint againstMatchmakerRating, int matchmakerRatingChange)
    {
        // called for each participant after winning a match
        foreach (var member in _members)
            if (member.Guid == player.GUID)
            {
                // update personal rating
                var mod = GetRatingMod(member.PersonalRating, againstMatchmakerRating, true);
                member.ModifyPersonalRating(player, mod, GetArenaType());

                // update matchmaker rating
                member.ModifyMatchmakerRating(matchmakerRatingChange, GetSlot());

                // update personal stats
                member.WeekGames += 1;
                member.SeasonGames += 1;
                member.SeasonWins += 1;
                member.WeekWins += 1;
                // update unit fields
                player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesWeek, member.WeekGames);
                player.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.GamesSeason, member.SeasonGames);

                return;
            }
    }

    public void NotifyStatsChanged()
    {
        // This is called after a rated match ended
        // Updates arena team stats for every member of the team (not only the ones who participated!)
        foreach (var member in _members)
        {
            var player = _objectAccessor.FindPlayer(member.Guid);

            if (player != null)
                SendStats(player.Session);
        }
    }

    public void OfflineMemberLost(ObjectGuid guid, uint againstMatchmakerRating, int matchmakerRatingChange = -12)
    {
        // Called for offline player after ending rated arena match!
        foreach (var member in _members)
            if (member.Guid == guid)
            {
                // update personal rating
                var mod = GetRatingMod(member.PersonalRating, againstMatchmakerRating, false);
                member.ModifyPersonalRating(null, mod, GetArenaType());

                // update matchmaker rating
                member.ModifyMatchmakerRating(matchmakerRatingChange, GetSlot());

                // update personal played stats
                member.WeekGames += 1;
                member.SeasonGames += 1;

                return;
            }
    }

    public void SaveToDB()
    {
        // Save team and member stats to db
        // Called after a match has ended or when calculating arena_points

        SQLTransaction trans = new();

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ARENA_TEAM_STATS);
        stmt.AddValue(0, _stats.Rating);
        stmt.AddValue(1, _stats.WeekGames);
        stmt.AddValue(2, _stats.WeekWins);
        stmt.AddValue(3, _stats.SeasonGames);
        stmt.AddValue(4, _stats.SeasonWins);
        stmt.AddValue(5, _stats.Rank);
        stmt.AddValue(6, GetId());
        trans.Append(stmt);

        foreach (var member in _members)
        {
            // Save the effort and go
            if (member.WeekGames == 0)
                continue;

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ARENA_TEAM_MEMBER);
            stmt.AddValue(0, member.PersonalRating);
            stmt.AddValue(1, member.WeekGames);
            stmt.AddValue(2, member.WeekWins);
            stmt.AddValue(3, member.SeasonGames);
            stmt.AddValue(4, member.SeasonWins);
            stmt.AddValue(5, GetId());
            stmt.AddValue(6, member.Guid.Counter);
            trans.Append(stmt);

            stmt = _characterDatabase.GetPreparedStatement(CharStatements.REP_CHARACTER_ARENA_STATS);
            stmt.AddValue(0, member.Guid.Counter);
            stmt.AddValue(1, GetSlot());
            stmt.AddValue(2, member.MatchMakerRating);
            trans.Append(stmt);
        }

        _characterDatabase.CommitTransaction(trans);
    }

    public void SendStats(WorldSession session)
    {
        /*WorldPacket data = new WorldPacket(ServerOpcodes.ArenaTeamStats);
        data.WriteUInt32(GetId());                                // team id
        data.WriteUInt32(stats.Rating);                           // rating
        data.WriteUInt32(stats.WeekGames);                        // games this week
        data.WriteUInt32(stats.WeekWins);                         // wins this week
        data.WriteUInt32(stats.SeasonGames);                      // played this season
        data.WriteUInt32(stats.SeasonWins);                       // wins this season
        data.WriteUInt32(stats.Rank);                             // rank
        session.SendPacket(data);*/
    }

    public void SetCaptain(ObjectGuid guid)
    {
        // Disable remove/promote buttons
        var oldCaptain = _objectAccessor.FindPlayer(GetCaptain());

        oldCaptain?.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.Member, 1);

        // Set new captain
        _captainGuid = guid;

        // Update database
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ARENA_TEAM_CAPTAIN);
        stmt.AddValue(0, guid.Counter);
        stmt.AddValue(1, GetId());
        _characterDatabase.Execute(stmt);

        // Enable remove/promote buttons
        var newCaptain = _objectAccessor.FindPlayer(guid);

        if (newCaptain == null)
            return;

        newCaptain.SetArenaTeamInfoField(GetSlot(), ArenaTeamInfoType.Member, 0);

        if (oldCaptain != null)
            Log.Logger.Debug("Player: {0} [GUID: {1}] promoted player: {2} [GUID: {3}] to leader of arena team [Id: {4}, Name: {5}] [Type: {6}].",
                             oldCaptain.GetName(),
                             oldCaptain.GUID.ToString(),
                             newCaptain.GetName(),
                             newCaptain.GUID.ToString(),
                             GetId(),
                             GetName(),
                             GetArenaType());
    }

    public bool SetName(string name)
    {
        if (_teamName == name || string.IsNullOrEmpty(name) || name.Length > 24 || _objectManager.IsReservedName(name) || !_objectManager.IsValidCharterName(name))
            return false;

        _teamName = name;
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ARENA_TEAM_NAME);
        stmt.AddValue(0, _teamName);
        stmt.AddValue(1, GetId());
        _characterDatabase.Execute(stmt);

        return true;
    }

    public int WonAgainst(uint ownMmRating, uint opponentMmRating, ref int ratingChange)
    {
        // Called when the team has won
        // Change in Matchmaker rating
        var mod = GetMatchmakerRatingMod(ownMmRating, opponentMmRating, true);

        // Change in Team Rating
        ratingChange = GetRatingMod(_stats.Rating, opponentMmRating, true);

        // Modify the team stats accordingly
        FinishGame(ratingChange);

        // Update number of wins per season and week
        _stats.WeekWins += 1;
        _stats.SeasonWins += 1;

        // Return the rating change, used to display it on the results screen
        return mod;
    }

    private float GetChanceAgainst(uint ownRating, uint opponentRating)
    {
        // Returns the chance to win against a team with the given rating, used in the rating adjustment calculation
        // ELO system
        return (float)(1.0f / (1.0f + Math.Exp(Math.Log(10.0f) * ((float)opponentRating - ownRating) / 650.0f)));
    }

    private int GetMatchmakerRatingMod(uint ownRating, uint opponentRating, bool won)
    {
        // 'Chance' calculation - to beat the opponent
        // This is a simulation. Not much info on how it really works
        var chance = GetChanceAgainst(ownRating, opponentRating);
        var wonMod = won ? 1.0f : 0.0f;
        var mod = wonMod - chance;

        // Work in progress:
        /*
        // This is a simulation, as there is not much info on how it really works
        float confidence_mod = min(1.0f - fabs(mod), 0.5f);

        // Apply confidence factor to the mod:
        mod *= confidence_factor

        // And only after that update the new confidence factor
        confidence_factor -= ((confidence_factor - 1.0f) * confidence_mod) / confidence_factor;
        */

        // Real rating modification
        mod *= _configuration.GetDefaultValue("Arena:ArenaMatchmakerRatingModifier", 24.0f);

        return (int)Math.Ceiling(mod);
    }

    private int GetRatingMod(uint ownRating, uint opponentRating, bool won)
    {
        // 'Chance' calculation - to beat the opponent
        // This is a simulation. Not much info on how it really works
        var chance = GetChanceAgainst(ownRating, opponentRating);

        // Calculate the rating modification
        float mod;

        // todo Replace this hack with using the confidence factor (limiting the factor to 2.0f)
        if (won)
        {
            if (ownRating < 1300)
            {
                var winRatingModifier1 = _configuration.GetDefaultValue("Arena:ArenaWinRatingModifier1", 48.0f);

                if (ownRating < 1000)
                    mod = winRatingModifier1 * (1.0f - chance);
                else
                    mod = (winRatingModifier1 / 2.0f + winRatingModifier1 / 2.0f * (1300.0f - ownRating) / 300.0f) * (1.0f - chance);
            }
            else
                mod = _configuration.GetDefaultValue("Arena:ArenaWinRatingModifier2", 24.0f) * (1.0f - chance);
        }
        else
            mod = _configuration.GetDefaultValue("Arena:ArenaLoseRatingModifier", 24.0f) * -chance;

        return (int)Math.Ceiling(mod);
    }
}