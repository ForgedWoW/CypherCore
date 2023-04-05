// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Forged.MapServer.Arenas.Zones;
using Forged.MapServer.BattleGrounds.Zones;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Events;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking.Packets.BattleGround;
using Forged.MapServer.Networking.Packets.LFG;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Threading;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.BattleGrounds;

public class BattlegroundManager
{
    private readonly Dictionary<uint, BattlegroundTemplate> _battlegroundMapTemplates = new();
    private readonly Dictionary<BattlegroundQueueTypeId, BattlegroundQueue> _battlegroundQueues = new();
    private readonly Dictionary<BattlegroundTypeId, BattlegroundTemplate> _battlegroundTemplates = new();
    private readonly Dictionary<uint, BattlegroundTypeId> _battleMastersMap = new();
    private readonly Dictionary<BattlegroundTypeId, BattlegroundData> _bgDataStore = new();
    private readonly MultiMap<BattlegroundQueueTypeId, Battleground> _bgFreeSlotQueue = new();
    private readonly ClassFactory _classFactory;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly DisableManager _disableManager;
    private readonly GameEventManager _gameEventManager;
    private readonly MapManager _mapManager;
    private readonly GameObjectManager _objectManager;
    private readonly LimitedThreadTaskManager _threadTaskManager;
    private readonly WorldDatabase _worldDatabase;
    private readonly WorldManager _worldManager;
    private bool _arenaTesting;
    private uint _nextRatedArenaUpdate;
    private List<ScheduledQueueUpdate> _queueUpdateScheduler = new();
    private bool _testing;
    private uint _updateTimer;

    public BattlegroundManager(IConfiguration configuration, MapManager mapManager, WorldDatabase worldDatabase, DisableManager disableManager, CliDB cliDB,
                               GameObjectManager objectManager, WorldManager worldManager, GameEventManager gameEventManager, ClassFactory classFactory)
    {
        _configuration = configuration;
        _mapManager = mapManager;
        _worldDatabase = worldDatabase;
        _disableManager = disableManager;
        _cliDB = cliDB;
        _objectManager = objectManager;
        _worldManager = worldManager;
        _gameEventManager = gameEventManager;
        _classFactory = classFactory;
        _nextRatedArenaUpdate = _configuration.GetDefaultValue("Arena.RatedUpdateTimer", 5u * Time.IN_MILLISECONDS);
        _threadTaskManager = new LimitedThreadTaskManager(_configuration.GetDefaultValue("Map.ParellelUpdateTasks", 20));
    }

    public void AddBattleground(Battleground bg)
    {
        if (bg)
            _bgDataStore[bg.GetTypeID()].MBattlegrounds[bg.GetInstanceID()] = bg;
    }

    public void AddToBGFreeSlotQueue(BattlegroundQueueTypeId bgTypeId, Battleground bg)
    {
        _bgFreeSlotQueue.Add(bgTypeId, bg);
    }

    public BattlegroundQueueTypeId BGQueueTypeId(ushort battlemasterListId, BattlegroundQueueIdType type, bool rated, ArenaTypes teamSize)
    {
        return new BattlegroundQueueTypeId(battlemasterListId, (byte)type, rated, (byte)teamSize);
    }

    public void BuildBattlegroundStatusActive(out BattlefieldStatusActive battlefieldStatus, Battleground bg, Player player, uint ticketId, uint joinTime, ArenaTypes arenaType)
    {
        battlefieldStatus = new BattlefieldStatusActive();
        BuildBattlegroundStatusHeader(battlefieldStatus.Hdr, bg, player, ticketId, joinTime, bg.GetQueueId(), arenaType);
        battlefieldStatus.ShutdownTimer = bg.GetRemainingTime();
        battlefieldStatus.ArenaFaction = (byte)(player.GetBgTeam() == TeamFaction.Horde ? TeamIds.Horde : TeamIds.Alliance);
        battlefieldStatus.LeftEarly = false;
        battlefieldStatus.StartTimer = bg.GetElapsedTime();
        battlefieldStatus.Mapid = bg.GetMapId();
    }

    public void BuildBattlegroundStatusFailed(out BattlefieldStatusFailed battlefieldStatus, BattlegroundQueueTypeId queueId, Player pPlayer, uint ticketId, GroupJoinBattlegroundResult result, ObjectGuid errorGuid = default)
    {
        battlefieldStatus = new BattlefieldStatusFailed
        {
            Ticket =
            {
                RequesterGuid = pPlayer.GUID,
                Id = ticketId,
                Type = RideType.Battlegrounds,
                Time = (int)pPlayer.GetBattlegroundQueueJoinTime(queueId)
            },
            QueueID = queueId.GetPacked(),
            Reason = (int)result
        };

        if (!errorGuid.IsEmpty && (result == GroupJoinBattlegroundResult.NotInBattleground || result == GroupJoinBattlegroundResult.JoinTimedOut))
            battlefieldStatus.ClientID = errorGuid;
    }

    public void BuildBattlegroundStatusNeedConfirmation(out BattlefieldStatusNeedConfirmation battlefieldStatus, Battleground bg, Player player, uint ticketId, uint joinTime, uint timeout, ArenaTypes arenaType)
    {
        battlefieldStatus = new BattlefieldStatusNeedConfirmation();
        BuildBattlegroundStatusHeader(battlefieldStatus.Hdr, bg, player, ticketId, joinTime, bg.GetQueueId(), arenaType);
        battlefieldStatus.Mapid = bg.GetMapId();
        battlefieldStatus.Timeout = timeout;
        battlefieldStatus.Role = 0;
    }

    public void BuildBattlegroundStatusNone(out BattlefieldStatusNone battlefieldStatus, Player player, uint ticketId, uint joinTime)
    {
        battlefieldStatus = new BattlefieldStatusNone
        {
            Ticket =
            {
                RequesterGuid = player.GUID,
                Id = ticketId,
                Type = RideType.Battlegrounds,
                Time = (int)joinTime
            }
        };
    }

    public void BuildBattlegroundStatusQueued(out BattlefieldStatusQueued battlefieldStatus, Battleground bg, Player player, uint ticketId, uint joinTime, BattlegroundQueueTypeId queueId, uint avgWaitTime, ArenaTypes arenaType, bool asGroup)
    {
        battlefieldStatus = new BattlefieldStatusQueued();
        BuildBattlegroundStatusHeader(battlefieldStatus.Hdr, bg, player, ticketId, joinTime, queueId, arenaType);
        battlefieldStatus.AverageWaitTime = avgWaitTime;
        battlefieldStatus.AsGroup = asGroup;
        battlefieldStatus.SuspendedQueue = false;
        battlefieldStatus.EligibleForMatchmaking = true;
        battlefieldStatus.WaitTime = Time.GetMSTimeDiffToNow(joinTime);
    }

    // create a new Battleground that will really be used to play
    public Battleground CreateNewBattleground(BattlegroundQueueTypeId queueId, PvpDifficultyRecord bracketEntry)
    {
        var bgTypeId = GetRandomBG((BattlegroundTypeId)queueId.BattlemasterListId);

        // get the template BG
        var bgTemplate = GetBattlegroundTemplate(bgTypeId);

        if (bgTemplate == null)
        {
            Log.Logger.Error("Battleground: CreateNewBattleground - bg template not found for {0}", bgTypeId);

            return null;
        }

        if (bgTypeId == BattlegroundTypeId.RB || bgTypeId == BattlegroundTypeId.AA || bgTypeId == BattlegroundTypeId.RandomEpic)
            return null;

        // create a copy of the BG template
        var bg = bgTemplate.GetCopy();

        var isRandom = bgTypeId != (BattlegroundTypeId)queueId.BattlemasterListId && !bg.IsArena();

        bg.SetQueueId(queueId);
        bg.SetBracket(bracketEntry);
        bg.SetInstanceID(_mapManager.GenerateInstanceId());
        bg.SetClientInstanceID(CreateClientVisibleInstanceId((BattlegroundTypeId)queueId.BattlemasterListId, bracketEntry.GetBracketId()));
        bg.Reset();                                // reset the new bg (set status to status_wait_queue from status_none)
        bg.SetStatus(BattlegroundStatus.WaitJoin); // start the joining of the bg
        bg.SetArenaType((ArenaTypes)queueId.TeamSize);
        bg.SetRandomTypeID(bgTypeId);
        bg.SetRated(queueId.Rated);
        bg.SetRandom(isRandom);

        return bg;
    }

    public void DeleteAllBattlegrounds()
    {
        foreach (var data in _bgDataStore.Values.ToList())
            while (!data.MBattlegrounds.Empty())
                data.MBattlegrounds.First().Value.Dispose();

        _bgDataStore.Clear();

        foreach (var bg in _bgFreeSlotQueue.Values.ToList())
            bg.Dispose();

        _bgFreeSlotQueue.Clear();
    }

    public Battleground GetBattleground(uint instanceId, BattlegroundTypeId bgTypeId)
    {
        if (instanceId == 0)
            return null;

        if (bgTypeId is not BattlegroundTypeId.None or BattlegroundTypeId.RB or BattlegroundTypeId.RandomEpic)
        {
            var data = _bgDataStore.LookupByKey(bgTypeId);

            return data.MBattlegrounds.LookupByKey(instanceId);
        }

        foreach (var it in _bgDataStore)
        {
            var bgs = it.Value.MBattlegrounds;
            var bg = bgs.LookupByKey(instanceId);

            if (bg)
                return bg;
        }

        return null;
    }

    public BattlegroundQueue GetBattlegroundQueue(BattlegroundQueueTypeId bgQueueTypeId)
    {
        if (!_battlegroundQueues.ContainsKey(bgQueueTypeId))
            _battlegroundQueues[bgQueueTypeId] = _classFactory.Resolve<BattlegroundQueue>(new PositionalParameter(0, bgQueueTypeId));

        return _battlegroundQueues[bgQueueTypeId];
    }

    public Battleground GetBattlegroundTemplate(BattlegroundTypeId bgTypeId)
    {
        if (_bgDataStore.ContainsKey(bgTypeId))
            return _bgDataStore[bgTypeId].Template;

        return null;
    }

    public BattlegroundTypeId GetBattleMasterBG(uint entry)
    {
        return _battleMastersMap.LookupByKey(entry);
    }

    public List<Battleground> GetBGFreeSlotQueueStore(BattlegroundQueueTypeId bgTypeId)
    {
        return _bgFreeSlotQueue[bgTypeId];
    }

    public uint GetMaxRatingDifference()
    {
        // this is for stupid people who can't use brain and set max rating difference to 0
        var diff = _configuration.GetDefaultValue("Arena.MaxRatingDifference", 150u);

        if (diff == 0)
            diff = 5000;

        return diff;
    }

    public uint GetPrematureFinishTime()
    {
        return _configuration.GetDefaultValue("Battleground.PrematureFinishTimer", 5u * Time.MINUTE * Time.IN_MILLISECONDS);
    }

    public uint GetRatingDiscardTimer()
    {
        return _configuration.GetDefaultValue("Arena.RatingDiscardTimer", 10u * Time.MINUTE * Time.IN_MILLISECONDS);
    }

    public bool IsArenaTesting()
    {
        return _arenaTesting;
    }

    public bool IsBGWeekend(BattlegroundTypeId bgTypeId)
    {
        return _gameEventManager.IsHolidayActive(BGTypeToWeekendHolidayId(bgTypeId));
    }

    public bool IsTesting()
    {
        return _testing;
    }

    public bool IsValidQueueId(BattlegroundQueueTypeId bgQueueTypeId)
    {
        var battlemasterList = _cliDB.BattlemasterListStorage.LookupByKey(bgQueueTypeId.BattlemasterListId);

        if (battlemasterList == null)
            return false;

        switch ((BattlegroundQueueIdType)bgQueueTypeId.BgType)
        {
            case BattlegroundQueueIdType.Battleground:
                if (battlemasterList.InstanceType != (int)MapTypes.Battleground)
                    return false;

                if (bgQueueTypeId.TeamSize != 0)
                    return false;

                break;

            case BattlegroundQueueIdType.Arena:
                if (battlemasterList.InstanceType != (int)MapTypes.Arena)
                    return false;

                if (!bgQueueTypeId.Rated)
                    return false;

                if (bgQueueTypeId.TeamSize == 0)
                    return false;

                break;

            case BattlegroundQueueIdType.Wargame:
                if (bgQueueTypeId.Rated)
                    return false;

                break;

            case BattlegroundQueueIdType.ArenaSkirmish:
                if (battlemasterList.InstanceType != (int)MapTypes.Arena)
                    return false;

                if (bgQueueTypeId.Rated)
                    return false;

                if (bgQueueTypeId.TeamSize != 0)
                    return false;

                break;

            default:
                return false;
        }

        return true;
    }

    public void LoadBattlegroundTemplates()
    {
        var oldMSTime = Time.MSTime;

        //                                         0   1                 2              3             4       5
        var result = _worldDatabase.Query("SELECT ID, AllianceStartLoc, HordeStartLoc, StartMaxDist, Weight, ScriptName FROM battleground_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 Battlegrounds. DB table `Battleground_template` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var bgTypeId = (BattlegroundTypeId)result.Read<uint>(0);

            if (_disableManager.IsDisabledFor(DisableType.Battleground, (uint)bgTypeId, null))
                continue;

            // can be overwrite by values from DB
            var bl = _cliDB.BattlemasterListStorage.LookupByKey((uint)bgTypeId);

            if (bl == null)
            {
                Log.Logger.Error("Battleground ID {0} not found in BattlemasterList.dbc. Battleground not created.", bgTypeId);

                continue;
            }

            BattlegroundTemplate bgTemplate = new()
            {
                Id = bgTypeId
            };

            var dist = result.Read<float>(3);
            bgTemplate.MaxStartDistSq = dist * dist;
            bgTemplate.Weight = result.Read<byte>(4);

            bgTemplate.ScriptId = _objectManager.GetScriptId(result.Read<string>(5));
            bgTemplate.BattlemasterEntry = bl;

            if (bgTemplate.Id != BattlegroundTypeId.AA && bgTemplate.Id != BattlegroundTypeId.RB && bgTemplate.Id != BattlegroundTypeId.RandomEpic)
            {
                var startId = result.Read<uint>(1);
                var start = _objectManager.GetWorldSafeLoc(startId);

                if (start != null)
                {
                    bgTemplate.StartLocation[TeamIds.Alliance] = start;
                }
                else if (bgTemplate.StartLocation[TeamIds.Alliance] != null) // reload case
                {
                    Log.Logger.Error($"Table `battleground_template` for id {bgTemplate.Id} contains a non-existing WorldSafeLocs.dbc id {startId} in field `AllianceStartLoc`. Ignoring.");
                }
                else
                {
                    Log.Logger.Error($"Table `Battleground_template` for Id {bgTemplate.Id} has a non-existed WorldSafeLocs.dbc id {startId} in field `AllianceStartLoc`. BG not created.");

                    continue;
                }

                startId = result.Read<uint>(2);
                start = _objectManager.GetWorldSafeLoc(startId);

                if (start != null)
                {
                    bgTemplate.StartLocation[TeamIds.Horde] = start;
                }
                else if (bgTemplate.StartLocation[TeamIds.Horde] != null) // reload case
                {
                    Log.Logger.Error($"Table `battleground_template` for id {bgTemplate.Id} contains a non-existing WorldSafeLocs.dbc id {startId} in field `HordeStartLoc`. Ignoring.");
                }
                else
                {
                    Log.Logger.Error($"Table `Battleground_template` for Id {bgTemplate.Id} has a non-existed WorldSafeLocs.dbc id {startId} in field `HordeStartLoc`. BG not created.");

                    continue;
                }
            }

            if (!CreateBattleground(bgTemplate))
            {
                Log.Logger.Error($"Could not create battleground template class ({bgTemplate.Id})!");

                continue;
            }

            _battlegroundTemplates[bgTypeId] = bgTemplate;

            if (bgTemplate.BattlemasterEntry.MapId[1] == -1) // in this case we have only one mapId
                _battlegroundMapTemplates[(uint)bgTemplate.BattlemasterEntry.MapId[0]] = _battlegroundTemplates[bgTypeId];

            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} Battlegrounds in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadBattleMastersEntry()
    {
        var oldMSTime = Time.MSTime;

        _battleMastersMap.Clear(); // need for reload case

        var result = _worldDatabase.Query("SELECT entry, bg_template FROM battlemaster_entry");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 battlemaster entries. DB table `battlemaster_entry` is empty!");

            return;
        }

        uint count = 0;

        do
        {
            var entry = result.Read<uint>(0);
            var cInfo = _objectManager.GetCreatureTemplate(entry);

            if (cInfo != null)
            {
                if (!cInfo.Npcflag.HasAnyFlag((uint)NPCFlags.BattleMaster))
                    Log.Logger.Error("Creature (Entry: {0}) listed in `battlemaster_entry` is not a battlemaster.", entry);
            }
            else
            {
                Log.Logger.Error("Creature (Entry: {0}) listed in `battlemaster_entry` does not exist.", entry);

                continue;
            }

            var bgTypeId = result.Read<uint>(1);

            if (!_cliDB.BattlemasterListStorage.ContainsKey(bgTypeId))
            {
                Log.Logger.Error("Table `battlemaster_entry` contain entry {0} for not existed Battleground type {1}, ignored.", entry, bgTypeId);

                continue;
            }

            ++count;
            _battleMastersMap[entry] = (BattlegroundTypeId)bgTypeId;
        } while (result.NextRow());

        CheckBattleMasters();

        Log.Logger.Information("Loaded {0} battlemaster entries in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void RemoveBattleground(BattlegroundTypeId bgTypeId, uint instanceId)
    {
        _bgDataStore[bgTypeId].MBattlegrounds.Remove(instanceId);
    }

    public void RemoveFromBGFreeSlotQueue(BattlegroundQueueTypeId bgTypeId, uint instanceId)
    {
        var queues = _bgFreeSlotQueue[bgTypeId];

        foreach (var bg in queues)
            if (bg.GetInstanceID() == instanceId)
            {
                queues.Remove(bg);

                return;
            }
    }

    public void ResetHolidays()
    {
        for (var i = BattlegroundTypeId.AV; i < BattlegroundTypeId.Max; i++)
        {
            var bg = GetBattlegroundTemplate(i);

            bg?.SetHoliday(false);
        }
    }

    public void ScheduleQueueUpdate(uint arenaMatchmakerRating, BattlegroundQueueTypeId bgQueueTypeId, BattlegroundBracketId bracketID)
    {
        //we will use only 1 number created of bgTypeId and bracket_id
        ScheduledQueueUpdate scheduleId = new(arenaMatchmakerRating, bgQueueTypeId, bracketID);

        if (!_queueUpdateScheduler.Contains(scheduleId))
            _queueUpdateScheduler.Add(scheduleId);
    }

    public void SendAreaSpiritHealerQuery(Player player, Battleground bg, ObjectGuid guid)
    {
        var time = 30000 - bg.GetLastResurrectTime(); // resurrect every 30 seconds

        if (time == 0xFFFFFFFF)
            time = 0;

        AreaSpiritHealerTime areaSpiritHealerTime = new()
        {
            HealerGuid = guid,
            TimeLeft = time
        };

        player.SendPacket(areaSpiritHealerTime);
    }

    public void SendBattlegroundList(Player player, ObjectGuid guid, BattlegroundTypeId bgTypeId)
    {
        var bgTemplate = GetBattlegroundTemplateByTypeId(bgTypeId);

        if (bgTemplate == null)
            return;

        BattlefieldList battlefieldList = new()
        {
            BattlemasterGuid = guid,
            BattlemasterListID = (int)bgTypeId,
            MinLevel = bgTemplate.GetMinLevel(),
            MaxLevel = bgTemplate.GetMaxLevel(),
            PvpAnywhere = guid.IsEmpty,
            HasRandomWinToday = player.GetRandomWinner()
        };

        player.SendPacket(battlefieldList);
    }

    public void SendToBattleground(Player player, uint instanceId, BattlegroundTypeId bgTypeId)
    {
        var bg = GetBattleground(instanceId, bgTypeId);

        if (bg)
        {
            var mapid = bg.GetMapId();
            var team = player.GetBgTeam();

            var pos = bg.GetTeamStartPosition(Battleground.GetTeamIndexByTeamId(team));
            Log.Logger.Debug($"BattlegroundMgr.SendToBattleground: Sending {player.GetName()} to map {mapid}, {pos.Loc} (bgType {bgTypeId})");
            player.TeleportTo(pos.Loc);
        }
        else
        {
            Log.Logger.Error($"BattlegroundMgr.SendToBattleground: Instance {instanceId} (bgType {bgTypeId}) not found while trying to teleport player {player.GetName()}");
        }
    }

    public void SetHolidayActive(uint battlegroundId)
    {
        var bg = GetBattlegroundTemplate((BattlegroundTypeId)battlegroundId);

        bg?.SetHoliday(true);
    }

    public void ToggleArenaTesting()
    {
        _arenaTesting = !_arenaTesting;
        _worldManager.SendWorldText(_arenaTesting ? CypherStrings.DebugArenaOn : CypherStrings.DebugArenaOff);
    }

    public void ToggleTesting()
    {
        _testing = !_testing;
        _worldManager.SendWorldText(_testing ? CypherStrings.DebugBgOn : CypherStrings.DebugBgOff);
    }

    public void Update(uint diff)
    {
        _updateTimer += diff;

        if (_updateTimer > 1000)
        {
            foreach (var data in _bgDataStore.Values)
            {
                var bgs = data.MBattlegrounds;

                // first one is template and should not be deleted
                foreach (var pair in bgs.ToList())
                {
                    var bg = pair.Value;

                    _threadTaskManager.Schedule(() =>
                    {
                        bg.Update(_updateTimer);

                        if (bg.ToBeDeleted())
                        {
                            bgs.Remove(pair.Key);
                            var clients = data.MClientBattlegroundIds[(int)bg.GetBracketId()];

                            if (!clients.Empty())
                                clients.Remove(bg.GetClientInstanceID());

                            bg.Dispose();
                        }
                    });
                }
            }

            _threadTaskManager.Wait();
            _updateTimer = 0;
        }

        // update events timer
        foreach (var pair in _battlegroundQueues)
            _threadTaskManager.Schedule(() => pair.Value.UpdateEvents(diff));

        _threadTaskManager.Wait();

        // update scheduled queues
        if (!_queueUpdateScheduler.Empty())
        {
            List<ScheduledQueueUpdate> scheduled = new();
            Extensions.Swap(ref scheduled, ref _queueUpdateScheduler);

            for (byte i = 0; i < scheduled.Count; i++)
            {
                var arenaMmRating = scheduled[i].ArenaMatchmakerRating;
                var bgQueueTypeId = scheduled[i].QueueId;
                var bracketID = scheduled[i].BracketId;
                GetBattlegroundQueue(bgQueueTypeId).BattlegroundQueueUpdate(diff, bracketID, arenaMmRating);
            }
        }

        // if rating difference counts, maybe force-update queues
        if (_configuration.GetDefaultValue("Arena.MaxRatingDifference", 150) != 0 && _configuration.GetDefaultValue("Arena.RatedUpdateTimer", 5 * Time.IN_MILLISECONDS) != 0)
        {
            // it's time to force update
            if (_nextRatedArenaUpdate < diff)
            {
                // forced update for rated arenas (scan all, but skipped non rated)
                Log.Logger.Debug("BattlegroundMgr: UPDATING ARENA QUEUES");

                foreach (var teamSize in new[]
                         {
                             ArenaTypes.Team2v2, ArenaTypes.Team3v3, ArenaTypes.Team5v5
                         })
                {
                    var ratedArenaQueueId = BGQueueTypeId((ushort)BattlegroundTypeId.AA, BattlegroundQueueIdType.Arena, true, teamSize);

                    for (var bracket = BattlegroundBracketId.First; bracket < BattlegroundBracketId.Max; ++bracket)
                        GetBattlegroundQueue(ratedArenaQueueId).BattlegroundQueueUpdate(diff, bracket, 0);
                }

                _nextRatedArenaUpdate = _configuration.GetDefaultValue("Arena.RatedUpdateTimer", 5u * Time.IN_MILLISECONDS);
            }
            else
            {
                _nextRatedArenaUpdate -= diff;
            }
        }
    }

    public BattlegroundTypeId WeekendHolidayIdToBGType(HolidayIds holiday)
    {
        return holiday switch
        {
            HolidayIds.CallToArmsAv => BattlegroundTypeId.AV,
            HolidayIds.CallToArmsEs => BattlegroundTypeId.EY,
            HolidayIds.CallToArmsWg => BattlegroundTypeId.WS,
            HolidayIds.CallToArmsSa => BattlegroundTypeId.SA,
            HolidayIds.CallToArmsAb => BattlegroundTypeId.AB,
            HolidayIds.CallToArmsIc => BattlegroundTypeId.IC,
            HolidayIds.CallToArmsTp => BattlegroundTypeId.TP,
            HolidayIds.CallToArmsBg => BattlegroundTypeId.BFG,
            _                       => BattlegroundTypeId.None
        };
    }

    private HolidayIds BGTypeToWeekendHolidayId(BattlegroundTypeId bgTypeId)
    {
        return bgTypeId switch
        {
            BattlegroundTypeId.AV  => HolidayIds.CallToArmsAv,
            BattlegroundTypeId.EY  => HolidayIds.CallToArmsEs,
            BattlegroundTypeId.WS  => HolidayIds.CallToArmsWg,
            BattlegroundTypeId.SA  => HolidayIds.CallToArmsSa,
            BattlegroundTypeId.AB  => HolidayIds.CallToArmsAb,
            BattlegroundTypeId.IC  => HolidayIds.CallToArmsIc,
            BattlegroundTypeId.TP  => HolidayIds.CallToArmsTp,
            BattlegroundTypeId.BFG => HolidayIds.CallToArmsBg,
            _                      => HolidayIds.None
        };
    }

    private void BuildBattlegroundStatusHeader(BattlefieldStatusHeader header, Battleground bg, Player player, uint ticketId, uint joinTime, BattlegroundQueueTypeId queueId, ArenaTypes arenaType)
    {
        header.Ticket = new RideTicket
        {
            RequesterGuid = player.GUID,
            Id = ticketId,
            Type = RideType.Battlegrounds,
            Time = (int)joinTime
        };

        header.QueueID.Add(queueId.GetPacked());
        header.RangeMin = (byte)bg.GetMinLevel();
        header.RangeMax = (byte)bg.GetMaxLevel();
        header.TeamSize = (byte)(bg.IsArena() ? arenaType : 0);
        header.InstanceID = bg.GetClientInstanceID();
        header.RegisteredMatch = bg.IsRated();
        header.TournamentRules = false;
    }

    private void CheckBattleMasters()
    {
        var templates = _objectManager.GetCreatureTemplates();

        foreach (var creature in templates)
            if (creature.Value.Npcflag.HasAnyFlag((uint)NPCFlags.BattleMaster) && !_battleMastersMap.ContainsKey(creature.Value.Entry))
            {
                Log.Logger.Error("CreatureTemplate (Entry: {0}) has UNIT_NPC_FLAG_BATTLEMASTER but no data in `battlemaster_entry` table. Removing Id!", creature.Value.Entry);
                templates[creature.Key].Npcflag &= ~(uint)NPCFlags.BattleMaster;
            }
    }

    // used to create the BG templates
    private bool CreateBattleground(BattlegroundTemplate bgTemplate)
    {
        var bg = GetBattlegroundTemplate(bgTemplate.Id);

        if (!bg)
            // Create the BG
            switch (bgTemplate.Id)
            {
                //case BattlegroundTypeId.AV:
                // bg = new BattlegroundAV(bgTemplate);
                //break;
                case BattlegroundTypeId.WS:
                    bg = new BgWarsongGluch(bgTemplate);

                    break;

                case BattlegroundTypeId.AB:
                case BattlegroundTypeId.DomAb:
                    bg = new BgArathiBasin(bgTemplate);

                    break;

                case BattlegroundTypeId.NA:
                    bg = new NagrandArena(bgTemplate);

                    break;

                case BattlegroundTypeId.BE:
                    bg = new BladesEdgeArena(bgTemplate);

                    break;

                case BattlegroundTypeId.EY:
                    bg = new BgEyeofStorm(bgTemplate);

                    break;

                case BattlegroundTypeId.RL:
                    bg = new RuinsofLordaeronArena(bgTemplate);

                    break;

                case BattlegroundTypeId.SA:
                    bg = new BgStrandOfAncients(bgTemplate);

                    break;

                case BattlegroundTypeId.DS:
                    bg = new DalaranSewersArena(bgTemplate);

                    break;

                case BattlegroundTypeId.RV:
                    bg = new RingofValorArena(bgTemplate);

                    break;
                //case BattlegroundTypeId.IC:
                //bg = new BattlegroundIC(bgTemplate);
                //break;
                case BattlegroundTypeId.AA:
                    bg = new Battleground(bgTemplate);

                    break;

                case BattlegroundTypeId.RB:
                    bg = new Battleground(bgTemplate);
                    bg.SetRandom(true);

                    break;
                /*
            case BattlegroundTypeId.TP:
                bg = new BattlegroundTP(bgTemplate);
                break;

            case BattlegroundTypeId.BFG:
                bg = new BattlegroundBFG(bgTemplate);
                break;
                */
                case BattlegroundTypeId.RandomEpic:
                    bg = new Battleground(bgTemplate);
                    bg.SetRandom(true);

                    break;

                default:
                    return false;
            }

        if (!_bgDataStore.ContainsKey(bg.GetTypeID()))
            _bgDataStore[bg.GetTypeID()] = new BattlegroundData();

        _bgDataStore[bg.GetTypeID()].Template = bg;

        return true;
    }

    private uint CreateClientVisibleInstanceId(BattlegroundTypeId bgTypeId, BattlegroundBracketId bracketID)
    {
        if (IsArenaType(bgTypeId))
            return 0; //arenas don't have client-instanceids

        // we create here an instanceid, which is just for
        // displaying this to the client and without any other use..
        // the client-instanceIds are unique for each Battleground-type
        // the instance-id just needs to be as low as possible, beginning with 1
        // the following works, because std.set is default ordered with "<"
        // the optimalization would be to use as bitmask std.vector<uint32> - but that would only make code unreadable

        var clientIds = _bgDataStore[bgTypeId].MClientBattlegroundIds[(int)bracketID];
        uint lastId = 0;

        foreach (var id in clientIds)
        {
            if (++lastId != id) //if there is a gap between the ids, we will break..
                break;

            lastId = id;
        }

        clientIds.Add(++lastId);

        return lastId;
    }

    private BattlegroundTemplate GetBattlegroundTemplateByMapId(uint mapId)
    {
        return _battlegroundMapTemplates.LookupByKey(mapId);
    }

    private BattlegroundTemplate GetBattlegroundTemplateByTypeId(BattlegroundTypeId id)
    {
        return _battlegroundTemplates.LookupByKey(id);
    }

    private BattlegroundTypeId GetRandomBG(BattlegroundTypeId bgTypeId)
    {
        var bgTemplate = GetBattlegroundTemplateByTypeId(bgTypeId);

        if (bgTemplate != null)
        {
            Dictionary<BattlegroundTypeId, float> selectionWeights = new();

            foreach (var mapId in bgTemplate.BattlemasterEntry.MapId)
            {
                if (mapId == -1)
                    break;

                var bg = GetBattlegroundTemplateByMapId((uint)mapId);

                if (bg != null)
                    selectionWeights.Add(bg.Id, bg.Weight);
            }

            return selectionWeights.SelectRandomElementByWeight(i => i.Value).Key;
        }

        return BattlegroundTypeId.None;
    }

    private bool IsArenaType(BattlegroundTypeId bgTypeId)
    {
        return bgTypeId == BattlegroundTypeId.AA || bgTypeId == BattlegroundTypeId.BE || bgTypeId == BattlegroundTypeId.NA || bgTypeId == BattlegroundTypeId.DS || bgTypeId == BattlegroundTypeId.RV || bgTypeId == BattlegroundTypeId.RL;
    }

    private struct ScheduledQueueUpdate
    {
        public readonly uint ArenaMatchmakerRating;

        public readonly BattlegroundBracketId BracketId;

        public BattlegroundQueueTypeId QueueId;

        public ScheduledQueueUpdate(uint arenaMatchmakerRating, BattlegroundQueueTypeId queueId, BattlegroundBracketId bracketId)
        {
            ArenaMatchmakerRating = arenaMatchmakerRating;
            QueueId = queueId;
            BracketId = bracketId;
        }

        public static bool operator !=(ScheduledQueueUpdate right, ScheduledQueueUpdate left)
        {
            return !(right == left);
        }

        public static bool operator ==(ScheduledQueueUpdate right, ScheduledQueueUpdate left)
        {
            return left.ArenaMatchmakerRating == right.ArenaMatchmakerRating && left.QueueId == right.QueueId && left.BracketId == right.BracketId;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return ArenaMatchmakerRating.GetHashCode() ^ QueueId.GetHashCode() ^ BracketId.GetHashCode();
        }
    }
}