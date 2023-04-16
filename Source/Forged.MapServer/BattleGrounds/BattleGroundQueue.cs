// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Arenas;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Groups;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Dynamic;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.BattleGrounds;

public class BattlegroundQueue
{
    private readonly ArenaTeamManager _arenaTeamManager;
    private readonly BattlegroundManager _battlegroundManager;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;

    // Event handler
    private readonly EventSystem _events = new();

    private readonly ObjectAccessor _objectAccessor;

    /// <summary>
    ///     This two dimensional array is used to store All queued groups
    ///     First dimension specifies the bgTypeId
    ///     Second dimension specifies the player's group types -
    ///     BG_QUEUE_PREMADE_ALLIANCE  is used for premade alliance groups and alliance rated arena teams
    ///     BG_QUEUE_PREMADE_HORDE     is used for premade horde groups and horde rated arena teams
    ///     BattlegroundConst.BgQueueNormalAlliance   is used for normal (or small) alliance groups or non-rated arena matches
    ///     BattlegroundConst.BgQueueNormalHorde      is used for normal (or small) horde groups or non-rated arena matches
    /// </summary>
    private readonly List<GroupQueueInfo>[][] _queuedGroups = new List<GroupQueueInfo>[(int)BattlegroundBracketId.Max][];

    private readonly Dictionary<ObjectGuid, PlayerQueueInfo> _queuedPlayers = new();
    private readonly BattlegroundQueueTypeId _queueId;
    private readonly SelectionPool[] _selectionPools = new SelectionPool[SharedConst.PvpTeamsCount];
    private readonly uint[][] _sumOfWaitTimes = new uint[SharedConst.PvpTeamsCount][];
    private readonly uint[][] _waitTimeLastPlayer = new uint[SharedConst.PvpTeamsCount][];
    private readonly uint[][][] _waitTimes = new uint[SharedConst.PvpTeamsCount][][];
    private readonly WorldManager _worldManager;

    public BattlegroundQueue(BattlegroundQueueTypeId queueId, IConfiguration configuration, ArenaTeamManager arenaTeamManager, WorldManager worldManager,
                             BattlegroundManager battlegroundManager, DB2Manager db2Manager, ObjectAccessor objectAccessor)
    {
        _queueId = queueId;
        _configuration = configuration;
        _arenaTeamManager = arenaTeamManager;
        _worldManager = worldManager;
        _battlegroundManager = battlegroundManager;
        _db2Manager = db2Manager;
        _objectAccessor = objectAccessor;

        for (var i = 0; i < (int)BattlegroundBracketId.Max; ++i)
        {
            _queuedGroups[i] = new List<GroupQueueInfo>[BattlegroundConst.BgQueueTypesCount];

            for (var c = 0; c < BattlegroundConst.BgQueueTypesCount; ++c)
                _queuedGroups[i][c] = new List<GroupQueueInfo>();
        }

        for (var i = 0; i < SharedConst.PvpTeamsCount; ++i)
        {
            _waitTimes[i] = new uint[(int)BattlegroundBracketId.Max][];

            for (var c = 0; c < (int)BattlegroundBracketId.Max; ++c)
                _waitTimes[i][c] = new uint[SharedConst.CountOfPlayersToAverageWaitTime];

            _waitTimeLastPlayer[i] = new uint[(int)BattlegroundBracketId.Max];
            _sumOfWaitTimes[i] = new uint[(int)BattlegroundBracketId.Max];
        }

        _selectionPools[0] = new SelectionPool();
        _selectionPools[1] = new SelectionPool();
    }

    // add group or player (grp == null) to bg queue with the given leader and bg specifications
    public GroupQueueInfo AddGroup(Player leader, PlayerGroup group, TeamFaction team, PvpDifficultyRecord bracketEntry, bool isPremade, uint arenaRating, uint matchmakerRating, uint arenateamid = 0)
    {
        var bracketId = bracketEntry.BracketId;

        // create new ginfo
        GroupQueueInfo ginfo = new()
        {
            ArenaTeamId = arenateamid,
            IsInvitedToBGInstanceGUID = 0,
            JoinTime = GameTime.CurrentTimeMS,
            RemoveInviteTime = 0,
            Team = team,
            ArenaTeamRating = arenaRating,
            ArenaMatchmakerRating = matchmakerRating,
            OpponentsTeamRating = 0,
            OpponentsMatchmakerRating = 0
        };

        ginfo.Players.Clear();

        //compute index (if group is premade or joined a rated match) to queues
        uint index = 0;

        if (!_queueId.Rated && !isPremade)
            index += SharedConst.PvpTeamsCount;

        if (ginfo.Team == TeamFaction.Horde)
            index++;

        Log.Logger.Debug("Adding Group to BattlegroundQueue bgTypeId : {0}, bracket_id : {1}, index : {2}", _queueId.BattlemasterListId, bracketId, index);

        var lastOnlineTime = GameTime.CurrentTimeMS;

        //announce world (this don't need mutex)
        if (_queueId.Rated && _configuration.GetDefaultValue("Arena:QueueAnnouncer:Enable", false))
        {
            var arenaTeam = _arenaTeamManager.GetArenaTeamById(arenateamid);

            if (arenaTeam != null)
                _worldManager.SendWorldText(CypherStrings.ArenaQueueAnnounceWorldJoin, arenaTeam.GetName(), _queueId.TeamSize, _queueId.TeamSize, ginfo.ArenaTeamRating);
        }

        //add players from group to ginfo
        if (group)
        {
            for (var refe = group.FirstMember; refe != null; refe = refe.Next())
            {
                var member = refe.Source;

                if (!member)
                    continue; // this should never happen

                PlayerQueueInfo plInfo = new()
                {
                    LastOnlineTime = lastOnlineTime,
                    GroupInfo = ginfo
                };

                _queuedPlayers[member.GUID] = plInfo;
                // add the pinfo to ginfo's list
                ginfo.Players[member.GUID] = plInfo;
            }
        }
        else
        {
            PlayerQueueInfo plInfo = new()
            {
                LastOnlineTime = lastOnlineTime,
                GroupInfo = ginfo
            };

            _queuedPlayers[leader.GUID] = plInfo;
            ginfo.Players[leader.GUID] = plInfo;
        }

        //add GroupInfo to _queuedGroups
        {
            //ACE_Guard<ACE_Recursive_Thread_Mutex> guard(m_Lock);
            _queuedGroups[(int)bracketId][index].Add(ginfo);

            //announce to world, this code needs mutex
            if (_queueId.Rated || isPremade || !_configuration.GetDefaultValue("Battleground:QueueAnnouncer:Enable", false))
                return ginfo;

            var bg = _battlegroundManager.GetBattlegroundTemplate((BattlegroundTypeId)_queueId.BattlemasterListId);

            if (bg == null)
                return ginfo;

            var bgName = bg.Name;
            var minPlayers = bg.MinPlayersPerTeam;
            uint qHorde = 0;
            uint qAlliance = 0;
            uint qMinLevel = bracketEntry.MinLevel;
            uint qMaxLevel = bracketEntry.MaxLevel;

            foreach (var groupQueueInfo in _queuedGroups[(int)bracketId][BattlegroundConst.BgQueueNormalAlliance])
                if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0)
                    qAlliance += (uint)groupQueueInfo.Players.Count;

            foreach (var groupQueueInfo in _queuedGroups[(int)bracketId][BattlegroundConst.BgQueueNormalHorde])
                if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0)
                    qHorde += (uint)groupQueueInfo.Players.Count;

            // Show queue status to player only (when joining queue)
            if (_configuration.GetDefaultValue("Battleground:QueueAnnouncer:PlayerOnly", false))
                leader.SendSysMessage(CypherStrings.BgQueueAnnounceSelf,
                                      bgName,
                                      qMinLevel,
                                      qMaxLevel,
                                      qAlliance,
                                      minPlayers > qAlliance ? minPlayers - qAlliance : 0,
                                      qHorde,
                                      minPlayers > qHorde ? minPlayers - qHorde : 0);
            // System message
            else
                _worldManager.SendWorldText(CypherStrings.BgQueueAnnounceWorld,
                                            bgName,
                                            qMinLevel,
                                            qMaxLevel,
                                            qAlliance,
                                            minPlayers > qAlliance ? minPlayers - qAlliance : 0,
                                            qHorde,
                                            minPlayers > qHorde ? minPlayers - qHorde : 0);
            //release mutex
        }

        return ginfo;
    }

    /// <summary>
    ///     this method is called when group is inserted, or player / group is removed from BG Queue - there is only one player's status changed, so we don't use while (true) cycles to invite whole queue
    ///     it must be called after fully adding the members of a group to ensure group joining
    ///     should be called from Battleground.RemovePlayer function in some cases
    /// </summary>
    /// <param name="diff"> </param>
    /// <param name="bracketID"> </param>
    /// <param name="arenaRating"> </param>
    public void BattlegroundQueueUpdate(uint diff, BattlegroundBracketId bracketID, uint arenaRating)
    {
        //if no players in queue - do nothing
        if (_queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeAlliance].Empty() &&
            _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeHorde].Empty() &&
            _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance].Empty() &&
            _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalHorde].Empty())
            return;

        // Battleground with free slot for player should be always in the beggining of the queue
        // maybe it would be better to create bgfreeslotqueue for each bracket_id
        var bgQueues = _battlegroundManager.GetBGFreeSlotQueueStore(_queueId);

        foreach (var bg in bgQueues)
            // DO NOT allow queue manager to invite new player to rated games
            if (!bg.IsRated &&
                bg.BracketId == bracketID &&
                bg.Status > BattlegroundStatus.WaitQueue &&
                bg.Status < BattlegroundStatus.WaitLeave)
            {
                // clear selection pools
                _selectionPools[TeamIds.Alliance].Init();
                _selectionPools[TeamIds.Horde].Init();

                // call a function that does the job for us
                FillPlayersToBG(bg, bracketID);

                // now everything is set, invite players
                foreach (var queueInfo in _selectionPools[TeamIds.Alliance].SelectedGroups)
                    InviteGroupToBG(queueInfo, bg, queueInfo.Team);

                foreach (var queueInfo in _selectionPools[TeamIds.Horde].SelectedGroups)
                    InviteGroupToBG(queueInfo, bg, queueInfo.Team);

                if (!bg.HasFreeSlots)
                    bg.RemoveFromBGFreeSlotQueue();
            }

        // finished iterating through the bgs with free slots, maybe we need to create a new bg

        var bgTemplate = _battlegroundManager.GetBattlegroundTemplate((BattlegroundTypeId)_queueId.BattlemasterListId);

        if (!bgTemplate)
        {
            Log.Logger.Error($"Battleground: Update: bg template not found for {_queueId.BattlemasterListId}");

            return;
        }

        var bracketEntry = _db2Manager.GetBattlegroundBracketById(bgTemplate.MapId, bracketID);

        if (bracketEntry == null)
        {
            Log.Logger.Error("Battleground: Update: bg bracket entry not found for map {0} bracket id {1}", bgTemplate.MapId, bracketID);

            return;
        }

        // get the min. players per team, properly for larger arenas as well. (must have full teams for arena matches!)
        var minPlayersPerTeam = bgTemplate.MinPlayersPerTeam;
        var maxPlayersPerTeam = bgTemplate.GetMaxPlayersPerTeam();

        if (bgTemplate.IsArena)
        {
            maxPlayersPerTeam = _queueId.TeamSize;
            minPlayersPerTeam = _battlegroundManager.IsArenaTesting() ? 1u : _queueId.TeamSize;
        }
        else if (_battlegroundManager.IsTesting())
        {
            minPlayersPerTeam = 1;
        }

        _selectionPools[TeamIds.Alliance].Init();
        _selectionPools[TeamIds.Horde].Init();

        if (bgTemplate.IsBattleground)
            if (CheckPremadeMatch(bracketID, minPlayersPerTeam, maxPlayersPerTeam))
            {
                // create new Battleground
                var bg2 = _battlegroundManager.CreateNewBattleground(_queueId, bracketEntry);

                if (bg2 == null)
                {
                    Log.Logger.Error($"BattlegroundQueue.Update - Cannot create Battleground: {_queueId.BattlemasterListId}");

                    return;
                }

                // invite those selection pools
                for (uint i = 0; i < SharedConst.PvpTeamsCount; i++)
                    foreach (var queueInfo in _selectionPools[TeamIds.Alliance + i].SelectedGroups)
                        InviteGroupToBG(queueInfo, bg2, queueInfo.Team);

                bg2.StartBattleground();
                //clear structures
                _selectionPools[TeamIds.Alliance].Init();
                _selectionPools[TeamIds.Horde].Init();
            }

        // now check if there are in queues enough players to start new GameInfo of (normal Battleground, or non-rated arena)
        if (!_queueId.Rated)
        {
            // if there are enough players in pools, start new Battleground or non rated arena
            if (CheckNormalMatch(bracketID, minPlayersPerTeam, maxPlayersPerTeam) || (bgTemplate.IsArena && CheckSkirmishForSameFaction(bracketID, minPlayersPerTeam)))
            {
                // we successfully created a pool
                var bg2 = _battlegroundManager.CreateNewBattleground(_queueId, bracketEntry);

                if (bg2 == null)
                {
                    Log.Logger.Error($"BattlegroundQueue.Update - Cannot create Battleground: {_queueId.BattlemasterListId}");

                    return;
                }

                // invite those selection pools
                for (uint i = 0; i < SharedConst.PvpTeamsCount; i++)
                    foreach (var queueInfo in _selectionPools[TeamIds.Alliance + i].SelectedGroups)
                        InviteGroupToBG(queueInfo, bg2, queueInfo.Team);

                // start bg
                bg2.StartBattleground();
            }
        }
        else if (bgTemplate.IsArena)
        {
            // found out the minimum and maximum ratings the newly added team should battle against
            // arenaRating is the rating of the latest joined team, or 0
            // 0 is on (automatic update call) and we must set it to team's with longest wait time
            if (arenaRating == 0)
            {
                GroupQueueInfo front1 = null;
                GroupQueueInfo front2 = null;

                if (!_queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeAlliance].Empty())
                {
                    front1 = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeAlliance].First();
                    arenaRating = front1.ArenaMatchmakerRating;
                }

                if (!_queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeHorde].Empty())
                {
                    front2 = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeHorde].First();
                    arenaRating = front2.ArenaMatchmakerRating;
                }

                if (front1 != null && front2 != null)
                {
                    if (front1.JoinTime < front2.JoinTime)
                        arenaRating = front1.ArenaMatchmakerRating;
                }
                else if (front1 == null && front2 == null)
                {
                    return; //queues are empty
                }
            }

            //set rating range
            var arenaMinRating = arenaRating <= _battlegroundManager.GetMaxRatingDifference() ? 0 : arenaRating - _battlegroundManager.GetMaxRatingDifference();
            var arenaMaxRating = arenaRating + _battlegroundManager.GetMaxRatingDifference();
            // if max rating difference is set and the time past since server startup is greater than the rating discard time
            // (after what time the ratings aren't taken into account when making teams) then
            // the discard time is current_time - time_to_discard, teams that joined after that, will have their ratings taken into account
            // else leave the discard time on 0, this way all ratings will be discarded
            var discardTime = (int)(GameTime.CurrentTimeMS - _battlegroundManager.GetRatingDiscardTimer());

            // we need to find 2 teams which will play next GameInfo
            var queueArray = new GroupQueueInfo[SharedConst.PvpTeamsCount];
            byte found = 0;
            byte team = 0;

            for (var i = (byte)BattlegroundConst.BgQueuePremadeAlliance; i < BattlegroundConst.BgQueueNormalAlliance; i++)
                // take the group that joined first
                foreach (var queueInfo in _queuedGroups[(int)bracketID][i])
                    // if group match conditions, then add it to pool
                    if (queueInfo.IsInvitedToBGInstanceGUID == 0 && ((queueInfo.ArenaMatchmakerRating >= arenaMinRating && queueInfo.ArenaMatchmakerRating <= arenaMaxRating) || queueInfo.JoinTime < discardTime))
                    {
                        queueArray[found++] = queueInfo;
                        team = i;

                        break;
                    }

            if (found == 0)
                return;

            if (found == 1)
                foreach (var queueInfo in _queuedGroups[(int)bracketID][team])
                    if (queueInfo.IsInvitedToBGInstanceGUID == 0 && ((queueInfo.ArenaMatchmakerRating >= arenaMinRating && queueInfo.ArenaMatchmakerRating <= arenaMaxRating) || queueInfo.JoinTime < discardTime) && queueArray[0].ArenaTeamId != queueInfo.ArenaTeamId)
                    {
                        queueArray[found++] = queueInfo;

                        break;
                    }

            //if we have 2 teams, then start new arena and invite players!
            if (found == 2)
            {
                var aTeam = queueArray[TeamIds.Alliance];
                var hTeam = queueArray[TeamIds.Horde];
                var arena = _battlegroundManager.CreateNewBattleground(_queueId, bracketEntry);

                if (!arena)
                {
                    Log.Logger.Error("BattlegroundQueue.Update couldn't create arena instance for rated arena match!");

                    return;
                }

                aTeam.OpponentsTeamRating = hTeam.ArenaTeamRating;
                hTeam.OpponentsTeamRating = aTeam.ArenaTeamRating;
                aTeam.OpponentsMatchmakerRating = hTeam.ArenaMatchmakerRating;
                hTeam.OpponentsMatchmakerRating = aTeam.ArenaMatchmakerRating;
                Log.Logger.Debug("setting oposite teamrating for team {0} to {1}", aTeam.ArenaTeamId, aTeam.OpponentsTeamRating);
                Log.Logger.Debug("setting oposite teamrating for team {0} to {1}", hTeam.ArenaTeamId, hTeam.OpponentsTeamRating);

                // now we must move team if we changed its faction to another faction queue, because then we will spam log by errors in Queue.RemovePlayer
                if (aTeam.Team != TeamFaction.Alliance)
                {
                    _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeAlliance].Insert(0, aTeam);
                    _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeHorde].Remove(queueArray[TeamIds.Alliance]);
                }

                if (hTeam.Team != TeamFaction.Horde)
                {
                    _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeHorde].Insert(0, hTeam);
                    _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeAlliance].Remove(queueArray[TeamIds.Horde]);
                }

                arena.SetArenaMatchmakerRating(TeamFaction.Alliance, aTeam.ArenaMatchmakerRating);
                arena.SetArenaMatchmakerRating(TeamFaction.Horde, hTeam.ArenaMatchmakerRating);
                InviteGroupToBG(aTeam, arena, TeamFaction.Alliance);
                InviteGroupToBG(hTeam, arena, TeamFaction.Horde);

                Log.Logger.Debug("Starting rated arena match!");
                arena.StartBattleground();
            }
        }
    }

    public uint GetAverageQueueWaitTime(GroupQueueInfo ginfo, BattlegroundBracketId bracketId)
    {
        uint teamIndex = TeamIds.Alliance; //default set to TeamIndex.Alliance - or non rated arenas!

        if (_queueId.TeamSize == 0)
        {
            if (ginfo.Team == TeamFaction.Horde)
                teamIndex = TeamIds.Horde;
        }
        else
        {
            if (_queueId.Rated)
                teamIndex = TeamIds.Horde; //for rated arenas use TeamIndex.Horde
        }

        //check if there is enought values(we always add values > 0)
        if (_waitTimes[teamIndex][(int)bracketId][SharedConst.CountOfPlayersToAverageWaitTime - 1] != 0)
            return _sumOfWaitTimes[teamIndex][(int)bracketId] / SharedConst.CountOfPlayersToAverageWaitTime;
        else
            //if there aren't enough values return 0 - not available
            return 0;
    }

    public bool GetPlayerGroupInfoData(ObjectGuid guid, out GroupQueueInfo ginfo)
    {
        ginfo = null;
        if (!_queuedPlayers.TryGetValue(guid, out var playerQueueInfo))
            return false;

        ginfo = playerQueueInfo.GroupInfo;

        return true;
    }

    public BattlegroundQueueTypeId GetQueueId()
    {
        return _queueId;
    }

    //returns true when player pl_guid is in queue and is invited to bgInstanceGuid
    public bool IsPlayerInvited(ObjectGuid plGUID, uint bgInstanceGuid, uint removeTime)
    {
        var queueInfo = _queuedPlayers.LookupByKey(plGUID);

        return queueInfo != null && queueInfo.GroupInfo.IsInvitedToBGInstanceGUID == bgInstanceGuid && queueInfo.GroupInfo.RemoveInviteTime == removeTime;
    }

    //remove player from queue and from group info, if group info is empty then remove it too
    public void RemovePlayer(ObjectGuid guid, bool decreaseInvitedCount)
    {
        var bracketID = -1; // signed for proper for-loop finish

        //remove player from map, if he's there
        if (!_queuedPlayers.TryGetValue(guid, out var playerQueueInfo))
        {
            var playerName = "Unknown";
            var player = _objectAccessor.FindPlayer(guid);

            if (player != null)
                playerName = player.GetName();

            Log.Logger.Debug("BattlegroundQueue: couldn't find player {0} ({1})", playerName, guid.ToString());

            return;
        }

        var group = playerQueueInfo.GroupInfo;
        GroupQueueInfo groupQueseInfo = null;
        // mostly people with the highest levels are in Battlegrounds, thats why
        // we count from MAX_Battleground_QUEUES - 1 to 0

        var index = group.Team == TeamFaction.Horde ? BattlegroundConst.BgQueuePremadeHorde : BattlegroundConst.BgQueuePremadeAlliance;

        for (var bracketIDTmp = (int)BattlegroundBracketId.Max - 1; bracketIDTmp >= 0 && bracketID == -1; --bracketIDTmp)
        {
            //we must check premade and normal team's queue - because when players from premade are joining bg,
            //they leave groupinfo so we can't use its players size to find out index
            for (var j = index; j < BattlegroundConst.BgQueueTypesCount; j += SharedConst.PvpTeamsCount)
                foreach (var k in _queuedGroups[bracketIDTmp][j])
                    if (k == group)
                    {
                        bracketID = bracketIDTmp;
                        groupQueseInfo = k;
                        //we must store index to be able to erase iterator
                        index = j;

                        break;
                    }
        }

        //player can't be in queue without group, but just in case
        if (bracketID == -1)
        {
            Log.Logger.Error("BattlegroundQueue: ERROR Cannot find groupinfo for {0}", guid.ToString());

            return;
        }

        Log.Logger.Debug("BattlegroundQueue: Removing {0}, from bracket_id {1}", guid.ToString(), bracketID);

        // ALL variables are correctly set
        // We can ignore leveling up in queue - it should not cause crash
        // remove player from group
        // if only one player there, remove group

        // remove player queue info from group queue info
        if (group.Players.ContainsKey(guid))
            group.Players.Remove(guid);

        // if invited to bg, and should decrease invited count, then do it
        if (decreaseInvitedCount && group.IsInvitedToBGInstanceGUID != 0)
        {
            var bg = _battlegroundManager.GetBattleground(group.IsInvitedToBGInstanceGUID, (BattlegroundTypeId)_queueId.BattlemasterListId);

            if (bg)
                bg.DecreaseInvitedCount(group.Team);
        }

        // remove player queue info
        _queuedPlayers.Remove(guid);

        // announce to world if arena team left queue for rated match, show only once
        if (_queueId.TeamSize != 0 && _queueId.Rated && group.Players.Empty() && _configuration.GetDefaultValue("Arena:QueueAnnouncer:Enable", false))
        {
            var team = _arenaTeamManager.GetArenaTeamById(group.ArenaTeamId);

            if (team != null)
                _worldManager.SendWorldText(CypherStrings.ArenaQueueAnnounceWorldExit, team.GetName(), _queueId.TeamSize, _queueId.TeamSize, group.ArenaTeamRating);
        }

        // if player leaves queue and he is invited to rated arena match, then he have to lose
        if (group.IsInvitedToBGInstanceGUID != 0 && _queueId.Rated && decreaseInvitedCount)
        {
            var at = _arenaTeamManager.GetArenaTeamById(group.ArenaTeamId);

            if (at != null)
            {
                Log.Logger.Debug("UPDATING memberLost's personal arena rating for {0} by opponents rating: {1}", guid.ToString(), group.OpponentsTeamRating);
                var player = _objectAccessor.FindPlayer(guid);

                if (player)
                    at.MemberLost(player, group.OpponentsMatchmakerRating);
                else
                    at.OfflineMemberLost(guid, group.OpponentsMatchmakerRating);

                at.SaveToDB();
            }
        }

        // remove group queue info if needed
        if (group.Players.Empty())
        {
            _queuedGroups[bracketID][index].Remove(groupQueseInfo);

            return;
        }

        // if group wasn't empty, so it wasn't deleted, and player have left a rated
        // queue . everyone from the group should leave too
        // don't remove recursively if already invited to bg!
        if (group.IsInvitedToBGInstanceGUID == 0 && _queueId.Rated)
        {
            // remove next player, this is recursive
            // first send removal information
            var plr2 = _objectAccessor.FindConnectedPlayer(group.Players.FirstOrDefault().Key);

            if (plr2)
            {
                var queueSlot = plr2.GetBattlegroundQueueIndex(_queueId);

                plr2.RemoveBattlegroundQueueId(_queueId); // must be called this way, because if you move this call to
                // queue.removeplayer, it causes bugs

                _battlegroundManager.BuildBattlegroundStatusNone(out var battlefieldStatus, plr2, queueSlot, plr2.GetBattlegroundQueueJoinTime(_queueId));
                plr2.SendPacket(battlefieldStatus);
            }

            // then actually delete, this may delete the group as well!
            RemovePlayer(group.Players.First().Key, decreaseInvitedCount);
        }
    }

    public void UpdateEvents(uint diff)
    {
        _events.Update(diff);
    }

    // this method tries to create Battleground or arena with MinPlayersPerTeam against MinPlayersPerTeam
    private bool CheckNormalMatch(BattlegroundBracketId bracketID, uint minPlayers, uint maxPlayers)
    {
        var teamIndex = new int[SharedConst.PvpTeamsCount];

        for (uint i = 0; i < SharedConst.PvpTeamsCount; i++)
        {
            teamIndex[i] = 0;

            for (; teamIndex[i] != _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + i].Count; ++teamIndex[i])
            {
                var groupQueueInfo = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + i][teamIndex[i]];

                if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0)
                {
                    _selectionPools[i].AddGroup(groupQueueInfo, maxPlayers);

                    if (_selectionPools[i].GetPlayerCount() >= minPlayers)
                        break;
                }
            }
        }

        //try to invite same number of players - this cycle may cause longer wait time even if there are enough players in queue, but we want ballanced bg
        uint j = TeamIds.Alliance;

        if (_selectionPools[TeamIds.Horde].GetPlayerCount() < _selectionPools[TeamIds.Alliance].GetPlayerCount())
            j = TeamIds.Horde;

        if (_configuration.GetDefaultValue("Battleground:InvitationType", 0) != (int)BattlegroundQueueInvitationType.NoBalance && _selectionPools[TeamIds.Horde].GetPlayerCount() >= minPlayers && _selectionPools[TeamIds.Alliance].GetPlayerCount() >= minPlayers)
        {
            //we will try to invite more groups to team with less players indexed by j
            ++teamIndex[j]; //this will not cause a crash, because for cycle above reached break;

            for (; teamIndex[j] != _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + j].Count; ++teamIndex[j])
            {
                var groupQueueInfo = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + j][teamIndex[j]];

                if (groupQueueInfo.IsInvitedToBGInstanceGUID != 0)
                    continue;

                if (!_selectionPools[j].AddGroup(groupQueueInfo, _selectionPools[(j + 1) % SharedConst.PvpTeamsCount].GetPlayerCount()))
                    break;
            }

            // do not allow to start bg with more than 2 players more on 1 faction
            if (_selectionPools[TeamIds.Horde].GetPlayerCount() - _selectionPools[TeamIds.Alliance].GetPlayerCount() > 2)
                return false;
        }

        //allow 1v0 if debug bg
        if (_battlegroundManager.IsTesting() && (_selectionPools[TeamIds.Alliance].GetPlayerCount() != 0 || _selectionPools[TeamIds.Horde].GetPlayerCount() != 0))
            return true;

        //return true if there are enough players in selection pools - enable to work .debug bg command correctly
        return _selectionPools[TeamIds.Alliance].GetPlayerCount() >= minPlayers && _selectionPools[TeamIds.Horde].GetPlayerCount() >= minPlayers;
    }

    // this method checks if premade versus premade Battleground is possible
    // then after 30 mins (default) in queue it moves premade group to normal queue
    // it tries to invite as much players as it can - to MaxPlayersPerTeam, because premade groups have more than MinPlayersPerTeam players
    private bool CheckPremadeMatch(BattlegroundBracketId bracketID, uint minPlayersPerTeam, uint maxPlayersPerTeam)
    {
        //check match
        if (!_queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeAlliance].Empty() && !_queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeHorde].Empty())
        {
            //start premade match
            //if groups aren't invited
            GroupQueueInfo aliGroup = null;
            GroupQueueInfo hordeGroup = null;

            foreach (var groupQueueInfo in _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeAlliance])
            {
                aliGroup = groupQueueInfo;

                if (aliGroup.IsInvitedToBGInstanceGUID == 0)
                    break;
            }

            foreach (var groupQueueInfo in _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeHorde])
            {
                hordeGroup = groupQueueInfo;

                if (hordeGroup.IsInvitedToBGInstanceGUID == 0)
                    break;
            }

            if (aliGroup != null && hordeGroup != null)
            {
                _selectionPools[TeamIds.Alliance].AddGroup(aliGroup, maxPlayersPerTeam);
                _selectionPools[TeamIds.Horde].AddGroup(hordeGroup, maxPlayersPerTeam);
                //add groups/players from normal queue to size of bigger group
                var maxPlayers = Math.Min(_selectionPools[TeamIds.Alliance].GetPlayerCount(), _selectionPools[TeamIds.Horde].GetPlayerCount());

                for (uint i = 0; i < SharedConst.PvpTeamsCount; i++)
                    foreach (var groupQueueInfo in _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + i])
                        //if groupQueueInfo can join BG and player count is less that maxPlayers, then add group to selectionpool
                        if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0 && !_selectionPools[i].AddGroup(groupQueueInfo, maxPlayers))
                            break;

                //premade selection pools are set
                return true;
            }
        }

        // now check if we can move group from Premade queue to normal queue (timer has expired) or group size lowered!!
        // this could be 2 cycles but i'm checking only first team in queue - it can cause problem -
        // if first is invited to BG and seconds timer expired, but we can ignore it, because players have only 80 seconds to click to enter bg
        // and when they click or after 80 seconds the queue info is removed from queue
        var timeBefore = GameTime.CurrentTimeMS - _configuration.GetDefaultValue("Battleground:PremadeGroupWaitForMatch", 30 * Time.MINUTE * Time.IN_MILLISECONDS);

        for (uint i = 0; i < SharedConst.PvpTeamsCount; i++)
            if (!_queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeAlliance + i].Empty())
            {
                var groupQueueInfo = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeAlliance + i].First();

                if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0 && (groupQueueInfo.JoinTime < timeBefore || groupQueueInfo.Players.Count < minPlayersPerTeam))
                {
                    //we must insert group to normal queue and erase pointer from premade queue
                    _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + i].Insert(0, groupQueueInfo);
                    _queuedGroups[(int)bracketID][BattlegroundConst.BgQueuePremadeAlliance + i].Remove(groupQueueInfo);
                }
            }

        //selection pools are not set
        return false;
    }

    // this method will check if we can invite players to same faction skirmish match
    private bool CheckSkirmishForSameFaction(BattlegroundBracketId bracketID, uint minPlayersPerTeam)
    {
        if (_selectionPools[TeamIds.Alliance].GetPlayerCount() < minPlayersPerTeam && _selectionPools[TeamIds.Horde].GetPlayerCount() < minPlayersPerTeam)
            return false;

        uint teamIndex = TeamIds.Alliance;
        uint otherTeam = TeamIds.Horde;
        var otherTeamId = TeamFaction.Horde;

        if (_selectionPools[TeamIds.Horde].GetPlayerCount() == minPlayersPerTeam)
        {
            teamIndex = TeamIds.Horde;
            otherTeam = TeamIds.Alliance;
            otherTeamId = TeamFaction.Alliance;
        }

        //clear other team's selection
        _selectionPools[otherTeam].Init();
        //store last ginfo pointer
        var ginfo = _selectionPools[teamIndex].SelectedGroups.Last();
        //set itr_team to group that was added to selection pool latest
        var team = 0;

        foreach (var groupQueueInfo in _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + teamIndex])
            if (ginfo == groupQueueInfo)
                break;

        if (team == _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + teamIndex].Count - 1)
            return false;

        var team2 = team;
        ++team2;

        //invite players to other selection pool
        for (; team2 != _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + teamIndex].Count - 1; ++team2)
        {
            var groupQueueInfo = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + teamIndex][team2];

            //if selection pool is full then break;
            if (groupQueueInfo.IsInvitedToBGInstanceGUID == 0 && !_selectionPools[otherTeam].AddGroup(groupQueueInfo, minPlayersPerTeam))
                break;
        }

        if (_selectionPools[otherTeam].GetPlayerCount() != minPlayersPerTeam)
            return false;

        //here we have correct 2 selections and we need to change one teams team and move selection pool teams to other team's queue
        foreach (var groupQueueInfo in _selectionPools[otherTeam].SelectedGroups)
        {
            //set correct team
            groupQueueInfo.Team = otherTeamId;
            //add team to other queue
            _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + otherTeam].Insert(0, groupQueueInfo);
            //remove team from old queue
            var team3 = team;
            ++team3;

            for (; team3 != _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + teamIndex].Count - 1; ++team3)
            {
                var groupQueueInfo1 = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + teamIndex][team3];

                if (groupQueueInfo1 == groupQueueInfo)
                {
                    _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance + teamIndex].Remove(groupQueueInfo1);

                    break;
                }
            }
        }

        return true;
    }

    private void FillPlayersToBG(Battleground bg, BattlegroundBracketId bracketID)
    {
        var hordeFree = bg.GetFreeSlotsForTeam(TeamFaction.Horde);
        var aliFree = bg.GetFreeSlotsForTeam(TeamFaction.Alliance);
        var aliCount = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance].Count;
        var hordeCount = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalHorde].Count;

        // try to get even teams
        if (_configuration.GetDefaultValue("Battleground:InvitationType", 0) == (int)BattlegroundQueueInvitationType.Even)
            // check if the teams are even
            if (hordeFree == 1 && aliFree == 1)
            {
                // if we are here, the teams have the same amount of players
                // then we have to allow to join the same amount of players
                var hordeExtra = hordeCount - aliCount;
                var aliExtra = aliCount - hordeCount;

                hordeExtra = Math.Max(hordeExtra, 0);
                aliExtra = Math.Max(aliExtra, 0);

                if (aliCount != hordeCount)
                {
                    aliFree -= (uint)aliExtra;
                    hordeFree -= (uint)hordeExtra;

                    aliFree = Math.Max(aliFree, 0);
                    hordeFree = Math.Max(hordeFree, 0);
                }
            }

        //count of groups in queue - used to stop cycles
        var alyIndex = 0;

        {
            var listIndex = 0;
            var info = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance].FirstOrDefault();

            for (; alyIndex < aliCount && _selectionPools[TeamIds.Alliance].AddGroup(info, aliFree); alyIndex++)
                info = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance][listIndex++];
        }

        //the same thing for horde
        var hordeIndex = 0;

        {
            var listIndex = 0;
            var info = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalHorde].FirstOrDefault();

            for (; hordeIndex < hordeCount && _selectionPools[TeamIds.Horde].AddGroup(info, hordeFree); hordeIndex++)
                info = _queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalHorde][listIndex++];
        }

        //if ofc like BG queue invitation is set in config, then we are happy
        if (_configuration.GetDefaultValue("Battleground:InvitationType", 0) == (int)BattlegroundQueueInvitationType.NoBalance)
            return;
        /*
        if we reached this code, then we have to solve NP - complete problem called Subset sum problem
        So one solution is to check all possible invitation subgroups, or we can use these conditions:
        1. Last time when BattlegroundQueue.Update was executed we invited all possible players - so there is only small possibility
            that we will invite now whole queue, because only 1 change has been made to queues from the last BattlegroundQueue.Update call
        2. Other thing we should consider is group order in queue
        */

        // At first we need to compare free space in bg and our selection pool
        var diffAli = (int)(aliFree - _selectionPools[TeamIds.Alliance].GetPlayerCount());
        var diffHorde = (int)(hordeFree - _selectionPools[TeamIds.Horde].GetPlayerCount());

        while (Math.Abs(diffAli - diffHorde) > 1 && (_selectionPools[TeamIds.Horde].GetPlayerCount() > 0 || _selectionPools[TeamIds.Alliance].GetPlayerCount() > 0))
        {
            //each cycle execution we need to kick at least 1 group
            if (diffAli < diffHorde)
            {
                //kick alliance group, add to pool new group if needed
                if (_selectionPools[TeamIds.Alliance].KickGroup((uint)(diffHorde - diffAli)))
                    for (; alyIndex < aliCount && _selectionPools[TeamIds.Alliance].AddGroup(_queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalAlliance][alyIndex], (uint)(aliFree >= diffHorde ? aliFree - diffHorde : 0)); alyIndex++)
                        ++alyIndex;

                //if ali selection is already empty, then kick horde group, but if there are less horde than ali in bg - break;
                if (_selectionPools[TeamIds.Alliance].GetPlayerCount() == 0)
                {
                    if (aliFree <= diffHorde + 1)
                        break;

                    _selectionPools[TeamIds.Horde].KickGroup((uint)(diffHorde - diffAli));
                }
            }
            else
            {
                //kick horde group, add to pool new group if needed
                if (_selectionPools[TeamIds.Horde].KickGroup((uint)(diffAli - diffHorde)))
                    for (; hordeIndex < hordeCount && _selectionPools[TeamIds.Horde].AddGroup(_queuedGroups[(int)bracketID][BattlegroundConst.BgQueueNormalHorde][hordeIndex], (uint)(hordeFree >= diffAli ? hordeFree - diffAli : 0)); hordeIndex++)
                        ++hordeIndex;

                if (_selectionPools[TeamIds.Horde].GetPlayerCount() == 0)
                {
                    if (hordeFree <= diffAli + 1)
                        break;

                    _selectionPools[TeamIds.Alliance].KickGroup((uint)(diffAli - diffHorde));
                }
            }

            //count diffs after small update
            diffAli = (int)(aliFree - _selectionPools[TeamIds.Alliance].GetPlayerCount());
            diffHorde = (int)(hordeFree - _selectionPools[TeamIds.Horde].GetPlayerCount());
        }
    }

    private void InviteGroupToBG(GroupQueueInfo ginfo, Battleground bg, TeamFaction side)
    {
        // set side if needed
        if (side != 0)
            ginfo.Team = side;

        if (ginfo.IsInvitedToBGInstanceGUID != 0)
            return;

        // not yet invited
        // set invitation
        ginfo.IsInvitedToBGInstanceGUID = bg.InstanceID;
        var bgTypeId = bg.GetTypeID();
        var bgQueueTypeId = bg.GetQueueId();
        var bracketID = bg.BracketId;

        // set ArenaTeamId for rated matches
        if (bg.IsArena && bg.IsRated)
            bg.SetArenaTeamIdForTeam(ginfo.Team, ginfo.ArenaTeamId);

        ginfo.RemoveInviteTime = GameTime.CurrentTimeMS + BattlegroundConst.InviteAcceptWaitTime;

        // loop through the players
        foreach (var guid in ginfo.Players.Keys)
        {
            // get the player
            var player = _objectAccessor.FindPlayer(guid);

            // if offline, skip him, this should not happen - player is removed from queue when he logs out
            if (!player)
                continue;

            // invite the player
            PlayerInvitedToBGUpdateAverageWaitTime(ginfo, bracketID);

            // set invited player counters
            bg.IncreaseInvitedCount(ginfo.Team);

            player.SetInviteForBattlegroundQueueType(bgQueueTypeId, ginfo.IsInvitedToBGInstanceGUID);

            // create remind invite events
            BGQueueInviteEvent inviteEvent = new(player.GUID, ginfo.IsInvitedToBGInstanceGUID, bgTypeId, (ArenaTypes)_queueId.TeamSize, ginfo.RemoveInviteTime, _battlegroundManager, _objectAccessor);
            _events.AddEvent(inviteEvent, _events.CalculateTime(TimeSpan.FromMilliseconds(BattlegroundConst.InvitationRemindTime)));
            // create automatic remove events
            BGQueueRemoveEvent removeEvent = new(player.GUID, ginfo.IsInvitedToBGInstanceGUID, bgQueueTypeId, ginfo.RemoveInviteTime, _battlegroundManager, _objectAccessor);
            _events.AddEvent(removeEvent, _events.CalculateTime(TimeSpan.FromMilliseconds(BattlegroundConst.InviteAcceptWaitTime)));

            var queueSlot = player.GetBattlegroundQueueIndex(bgQueueTypeId);

            Log.Logger.Debug("Battleground: invited player {0} ({1}) to BG instance {2} queueindex {3} bgtype {4}",
                             player.GetName(),
                             player.GUID.ToString(),
                             bg.InstanceID,
                             queueSlot,
                             bg.GetTypeID());

            _battlegroundManager.BuildBattlegroundStatusNeedConfirmation(out var battlefieldStatus, bg, player, queueSlot, player.GetBattlegroundQueueJoinTime(bgQueueTypeId), BattlegroundConst.InviteAcceptWaitTime, (ArenaTypes)_queueId.TeamSize);
            player.SendPacket(battlefieldStatus);
        }
    }

    private void PlayerInvitedToBGUpdateAverageWaitTime(GroupQueueInfo ginfo, BattlegroundBracketId bracketID)
    {
        var timeInQueue = Time.GetMSTimeDiff(ginfo.JoinTime, GameTime.CurrentTimeMS);
        uint teamIndex = TeamIds.Alliance; //default set to TeamIndex.Alliance - or non rated arenas!

        if (_queueId.TeamSize == 0)
        {
            if (ginfo.Team == TeamFaction.Horde)
                teamIndex = TeamIds.Horde;
        }
        else
        {
            if (_queueId.Rated)
                teamIndex = TeamIds.Horde; //for rated arenas use TeamIndex.Horde
        }

        //store pointer to arrayindex of player that was added first
        var lastPlayerAddedPointer = _waitTimeLastPlayer[teamIndex][(int)bracketID];
        //remove his time from sum
        _sumOfWaitTimes[teamIndex][(int)bracketID] -= _waitTimes[teamIndex][(int)bracketID][lastPlayerAddedPointer];
        //set average time to new
        _waitTimes[teamIndex][(int)bracketID][lastPlayerAddedPointer] = timeInQueue;
        //add new time to sum
        _sumOfWaitTimes[teamIndex][(int)bracketID] += timeInQueue;
        //set index of last player added to next one
        lastPlayerAddedPointer++;
        _waitTimeLastPlayer[teamIndex][(int)bracketID] = lastPlayerAddedPointer % SharedConst.CountOfPlayersToAverageWaitTime;
    }

    /*
    This function is inviting players to already running Battlegrounds
    Invitation type is based on config file
    large groups are disadvantageous, because they will be kicked first if invitation type = 1
    */

    // class to select and invite groups to bg
    private class SelectionPool
    {
        public readonly List<GroupQueueInfo> SelectedGroups = new();

        private uint _playerCount;

        public bool AddGroup(GroupQueueInfo ginfo, uint desiredCount)
        {
            //if group is larger than desired count - don't allow to add it to pool
            if (ginfo.IsInvitedToBGInstanceGUID == 0 && desiredCount >= _playerCount + ginfo.Players.Count)
            {
                SelectedGroups.Add(ginfo);
                // increase selected players count
                _playerCount += (uint)ginfo.Players.Count;

                return true;
            }

            if (_playerCount < desiredCount)
                return true;

            return false;
        }

        public uint GetPlayerCount()
        {
            return _playerCount;
        }

        public void Init()
        {
            SelectedGroups.Clear();
            _playerCount = 0;
        }

        public bool KickGroup(uint size)
        {
            //find maxgroup or LAST group with size == size and kick it
            var found = false;
            GroupQueueInfo groupToKick = null;

            foreach (var groupQueueInfo in SelectedGroups)
                if (Math.Abs(groupQueueInfo.Players.Count - size) <= 1)
                {
                    groupToKick = groupQueueInfo;
                    found = true;
                }
                else if (!found)
                {
                    groupToKick = groupQueueInfo;
                }

            //if pool is empty, do nothing
            if (GetPlayerCount() == 0)
                return true;

            //update player count
            SelectedGroups.Remove(groupToKick);

            if (groupToKick == null)
                return true;

            _playerCount -= (uint)groupToKick.Players.Count;

            //return false if we kicked smaller group or there are enough players in selection pool
            return groupToKick.Players.Count > size + 1;
        }
    }
}