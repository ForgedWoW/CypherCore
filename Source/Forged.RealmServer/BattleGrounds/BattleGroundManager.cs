// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Framework.Threading;
using Forged.RealmServer.BattleGrounds.Zones;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking.Packets;
using Microsoft.Extensions.Configuration;
using Serilog;
using Framework.Util;
using Forged.RealmServer.Conditions;
using Forged.RealmServer.Globals;

namespace Forged.RealmServer.BattleGrounds;

public class BattlegroundManager
{
	readonly Dictionary<BattlegroundQueueTypeId, BattlegroundQueue> m_BattlegroundQueues = new();
	readonly MultiMap<BattlegroundQueueTypeId, Battleground> m_BGFreeSlotQueue = new();
	readonly Dictionary<uint, BattlegroundTypeId> mBattleMastersMap = new();
	readonly Dictionary<BattlegroundTypeId, BattlegroundTemplate> _battlegroundTemplates = new();
	readonly Dictionary<uint, BattlegroundTemplate> _battlegroundMapTemplates = new();
	readonly LimitedThreadTaskManager _threadTaskManager;
    private readonly IConfiguration _configuration;
    private readonly WorldConfig _worldConfig;
    private readonly CliDB _cliDB;
    private readonly DisableManager _disableManager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly WorldManager _worldManager;
    private readonly WorldDatabase _worldDatabase;
    private readonly GameEventManager _gameEventManager;
    List<ScheduledQueueUpdate> _queueUpdateScheduler = new();
	uint _nextRatedArenaUpdate;
	uint _updateTimer;
	bool _arenaTesting;
	bool _testing;

	BattlegroundManager(IConfiguration configuration, WorldConfig worldConfig, CliDB cliDB, DisableManager disableManager,
		GameObjectManager gameObjectManager, WorldManager worldManager, WorldDatabase worldDatabase, GameEventManager gameEventManager)
	{
        _configuration = configuration;
        _worldConfig = worldConfig;
        _cliDB = cliDB;
        _disableManager = disableManager;
        _gameObjectManager = gameObjectManager;
        _worldManager = worldManager;
        _worldDatabase = worldDatabase;
        _gameEventManager = gameEventManager;
        _nextRatedArenaUpdate = _worldConfig.GetUIntValue(WorldCfg.ArenaRatedUpdateTimer);
        _threadTaskManager = new(_configuration.GetDefaultValue("Map.ParellelUpdateTasks", 20));
    }

	public void BuildBattlegroundStatusNone(out BattlefieldStatusNone battlefieldStatus, Player player, uint ticketId, uint joinTime)
	{
		battlefieldStatus = new BattlefieldStatusNone();
		battlefieldStatus.Ticket.RequesterGuid = player.GUID;
		battlefieldStatus.Ticket.Id = ticketId;
		battlefieldStatus.Ticket.Type = RideType.Battlegrounds;
		battlefieldStatus.Ticket.Time = (int)joinTime;
	}

	public void BuildBattlegroundStatusNeedConfirmation(out BattlefieldStatusNeedConfirmation battlefieldStatus, Battleground bg, Player player, uint ticketId, uint joinTime, uint timeout, ArenaTypes arenaType)
	{
		battlefieldStatus = new BattlefieldStatusNeedConfirmation();
		BuildBattlegroundStatusHeader(battlefieldStatus.Hdr, bg, player, ticketId, joinTime, bg.GetQueueId(), arenaType);
		battlefieldStatus.Mapid = bg.GetMapId();
		battlefieldStatus.Timeout = timeout;
		battlefieldStatus.Role = 0;
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

	public void BuildBattlegroundStatusFailed(out BattlefieldStatusFailed battlefieldStatus, BattlegroundQueueTypeId queueId, Player pPlayer, uint ticketId, GroupJoinBattlegroundResult result, ObjectGuid errorGuid = default)
	{
		battlefieldStatus = new BattlefieldStatusFailed();
		battlefieldStatus.Ticket.RequesterGuid = pPlayer.GUID;
		battlefieldStatus.Ticket.Id = ticketId;
		battlefieldStatus.Ticket.Type = RideType.Battlegrounds;
		battlefieldStatus.Ticket.Time = (int)pPlayer.GetBattlegroundQueueJoinTime(queueId);
		battlefieldStatus.QueueID = queueId.GetPacked();
		battlefieldStatus.Reason = (int)result;

		if (!errorGuid.IsEmpty && (result == GroupJoinBattlegroundResult.NotInBattleground || result == GroupJoinBattlegroundResult.JoinTimedOut))
			battlefieldStatus.ClientID = errorGuid;
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

			BattlegroundTemplate bgTemplate = new();
			bgTemplate.Id = bgTypeId;
			var dist = result.Read<float>(3);
			bgTemplate.MaxStartDistSq = dist * dist;
			bgTemplate.Weight = result.Read<byte>(4);

			bgTemplate.ScriptId = _gameObjectManager.GetScriptId(result.Read<string>(5));
			bgTemplate.BattlemasterEntry = bl;

			_battlegroundTemplates[bgTypeId] = bgTemplate;

			if (bgTemplate.BattlemasterEntry.MapId[1] == -1) // in this case we have only one mapId
				_battlegroundMapTemplates[(uint)bgTemplate.BattlemasterEntry.MapId[0]] = _battlegroundTemplates[bgTypeId];

			++count;
		} while (result.NextRow());

        Log.Logger.Information("Loaded {0} Battlegrounds in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public void SendBattlegroundList(Player player, ObjectGuid guid, BattlegroundTypeId bgTypeId)
	{
		var bgTemplate = GetBattlegroundTemplateByTypeId(bgTypeId);

		if (bgTemplate == null)
			return;

		BattlefieldList battlefieldList = new();
		battlefieldList.BattlemasterGuid = guid;
		battlefieldList.BattlemasterListID = (int)bgTypeId;
		battlefieldList.MinLevel = bgTemplate.GetMinLevel();
		battlefieldList.MaxLevel = bgTemplate.GetMaxLevel();
		battlefieldList.PvpAnywhere = guid.IsEmpty;
		battlefieldList.HasRandomWinToday = player.GetRandomWinner();
		player.SendPacket(battlefieldList);
	}

	public void SendAreaSpiritHealerQuery(Player player, Battleground bg, ObjectGuid guid)
	{
		var time = 30000 - bg.GetLastResurrectTime(); // resurrect every 30 seconds

		if (time == 0xFFFFFFFF)
			time = 0;

		AreaSpiritHealerTime areaSpiritHealerTime = new();
		areaSpiritHealerTime.HealerGuid = guid;
		areaSpiritHealerTime.TimeLeft = time;

		player.SendPacket(areaSpiritHealerTime);
	}

	public BattlegroundQueueTypeId BGQueueTypeId(ushort battlemasterListId, BattlegroundQueueIdType type, bool rated, ArenaTypes teamSize)
	{
		return new BattlegroundQueueTypeId(battlemasterListId, (byte)type, rated, (byte)teamSize);
	}

	public void ToggleTesting()
	{
		_testing = !_testing;
		_worldManager.SendWorldText(_testing ? CypherStrings.DebugBgOn : CypherStrings.DebugBgOff);
	}

	public void ToggleArenaTesting()
	{
		_arenaTesting = !_arenaTesting;
		_worldManager.SendWorldText(_arenaTesting ? CypherStrings.DebugArenaOn : CypherStrings.DebugArenaOff);
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

	public void ScheduleQueueUpdate(uint arenaMatchmakerRating, BattlegroundQueueTypeId bgQueueTypeId, BattlegroundBracketId bracket_id)
	{
		//we will use only 1 number created of bgTypeId and bracket_id
		ScheduledQueueUpdate scheduleId = new(arenaMatchmakerRating, bgQueueTypeId, bracket_id);

		if (!_queueUpdateScheduler.Contains(scheduleId))
			_queueUpdateScheduler.Add(scheduleId);
	}

	public uint GetMaxRatingDifference()
	{
		// this is for stupid people who can't use brain and set max rating difference to 0
		var diff = _worldConfig.GetUIntValue(WorldCfg.ArenaMaxRatingDifference);

		if (diff == 0)
			diff = 5000;

		return diff;
	}

	public uint GetRatingDiscardTimer()
	{
		return _worldConfig.GetUIntValue(WorldCfg.ArenaRatingDiscardTimer);
	}

	public uint GetPrematureFinishTime()
	{
		return _worldConfig.GetUIntValue(WorldCfg.BattlegroundPrematureFinishTimer);
	}

	public void LoadBattleMastersEntry()
	{
		var oldMSTime = Time.MSTime;

		mBattleMastersMap.Clear(); // need for reload case

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
			var cInfo = _gameObjectManager.GetCreatureTemplate(entry);

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
			mBattleMastersMap[entry] = (BattlegroundTypeId)bgTypeId;
		} while (result.NextRow());

		CheckBattleMasters();

        Log.Logger.Information("Loaded {0} battlemaster entries in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
	}

	public BattlegroundTypeId WeekendHolidayIdToBGType(HolidayIds holiday)
	{
		switch (holiday)
		{
			case HolidayIds.CallToArmsAv:
				return BattlegroundTypeId.AV;
			case HolidayIds.CallToArmsEs:
				return BattlegroundTypeId.EY;
			case HolidayIds.CallToArmsWg:
				return BattlegroundTypeId.WS;
			case HolidayIds.CallToArmsSa:
				return BattlegroundTypeId.SA;
			case HolidayIds.CallToArmsAb:
				return BattlegroundTypeId.AB;
			case HolidayIds.CallToArmsIc:
				return BattlegroundTypeId.IC;
			case HolidayIds.CallToArmsTp:
				return BattlegroundTypeId.TP;
			case HolidayIds.CallToArmsBg:
				return BattlegroundTypeId.BFG;
			default:
				return BattlegroundTypeId.None;
		}
	}

	public bool IsBGWeekend(BattlegroundTypeId bgTypeId)
	{
		return _gameEventManager.IsHolidayActive(BGTypeToWeekendHolidayId(bgTypeId));
	}

	public List<Battleground> GetBGFreeSlotQueueStore(BattlegroundQueueTypeId bgTypeId)
	{
		return m_BGFreeSlotQueue[bgTypeId];
	}

	public void AddToBGFreeSlotQueue(BattlegroundQueueTypeId bgTypeId, Battleground bg)
	{
		m_BGFreeSlotQueue.Add(bgTypeId, bg);
	}

	public void RemoveFromBGFreeSlotQueue(BattlegroundQueueTypeId bgTypeId, uint instanceId)
	{
		var queues = m_BGFreeSlotQueue[bgTypeId];

		foreach (var bg in queues)
			if (bg.GetInstanceID() == instanceId)
			{
				queues.Remove(bg);

				return;
			}
	}

	public BattlegroundQueue GetBattlegroundQueue(BattlegroundQueueTypeId bgQueueTypeId)
	{
		if (!m_BattlegroundQueues.ContainsKey(bgQueueTypeId))
			m_BattlegroundQueues[bgQueueTypeId] = new BattlegroundQueue(bgQueueTypeId);

		return m_BattlegroundQueues[bgQueueTypeId];
	}

	public bool IsArenaTesting()
	{
		return _arenaTesting;
	}

	public bool IsTesting()
	{
		return _testing;
	}

	public BattlegroundTypeId GetBattleMasterBG(uint entry)
	{
		return mBattleMastersMap.LookupByKey(entry);
	}

	void BuildBattlegroundStatusHeader(BattlefieldStatusHeader header, Battleground bg, Player player, uint ticketId, uint joinTime, BattlegroundQueueTypeId queueId, ArenaTypes arenaType)
	{
		header.Ticket = new RideTicket();
		header.Ticket.RequesterGuid = player.GUID;
		header.Ticket.Id = ticketId;
		header.Ticket.Type = RideType.Battlegrounds;
		header.Ticket.Time = (int)joinTime;
		header.QueueID.Add(queueId.GetPacked());
		header.RangeMin = (byte)bg.GetMinLevel();
		header.RangeMax = (byte)bg.GetMaxLevel();
		header.TeamSize = (byte)(bg.IsArena() ? arenaType : 0);
		header.InstanceID = bg.GetClientInstanceID();
		header.RegisteredMatch = bg.IsRated();
		header.TournamentRules = false;
	}

	bool IsArenaType(BattlegroundTypeId bgTypeId)
	{
		return bgTypeId == BattlegroundTypeId.AA || bgTypeId == BattlegroundTypeId.BE || bgTypeId == BattlegroundTypeId.NA || bgTypeId == BattlegroundTypeId.DS || bgTypeId == BattlegroundTypeId.RV || bgTypeId == BattlegroundTypeId.RL;
	}

	void CheckBattleMasters()
	{
		var templates = _gameObjectManager.GetCreatureTemplates();

		foreach (var creature in templates)
			if (creature.Value.Npcflag.HasAnyFlag((uint)NPCFlags.BattleMaster) && !mBattleMastersMap.ContainsKey(creature.Value.Entry))
			{
				Log.Logger.Error("CreatureTemplate (Entry: {0}) has UNIT_NPC_FLAG_BATTLEMASTER but no data in `battlemaster_entry` table. Removing flag!", creature.Value.Entry);
				templates[creature.Key].Npcflag &= ~(uint)NPCFlags.BattleMaster;
			}
	}

	HolidayIds BGTypeToWeekendHolidayId(BattlegroundTypeId bgTypeId)
	{
		switch (bgTypeId)
		{
			case BattlegroundTypeId.AV:
				return HolidayIds.CallToArmsAv;
			case BattlegroundTypeId.EY:
				return HolidayIds.CallToArmsEs;
			case BattlegroundTypeId.WS:
				return HolidayIds.CallToArmsWg;
			case BattlegroundTypeId.SA:
				return HolidayIds.CallToArmsSa;
			case BattlegroundTypeId.AB:
				return HolidayIds.CallToArmsAb;
			case BattlegroundTypeId.IC:
				return HolidayIds.CallToArmsIc;
			case BattlegroundTypeId.TP:
				return HolidayIds.CallToArmsTp;
			case BattlegroundTypeId.BFG:
				return HolidayIds.CallToArmsBg;
			default:
				return HolidayIds.None;
		}
	}

	BattlegroundTypeId GetRandomBG(BattlegroundTypeId bgTypeId)
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

	BattlegroundTemplate GetBattlegroundTemplateByTypeId(BattlegroundTypeId id)
	{
		return _battlegroundTemplates.LookupByKey(id);
	}

	BattlegroundTemplate GetBattlegroundTemplateByMapId(uint mapId)
	{
		return _battlegroundMapTemplates.LookupByKey(mapId);
	}

	struct ScheduledQueueUpdate
	{
		public ScheduledQueueUpdate(uint arenaMatchmakerRating, BattlegroundQueueTypeId queueId, BattlegroundBracketId bracketId)
		{
			ArenaMatchmakerRating = arenaMatchmakerRating;
			QueueId = queueId;
			BracketId = bracketId;
		}

		public readonly uint ArenaMatchmakerRating;
		public BattlegroundQueueTypeId QueueId;
		public readonly BattlegroundBracketId BracketId;

		public static bool operator ==(ScheduledQueueUpdate right, ScheduledQueueUpdate left)
		{
			return left.ArenaMatchmakerRating == right.ArenaMatchmakerRating && left.QueueId == right.QueueId && left.BracketId == right.BracketId;
		}

		public static bool operator !=(ScheduledQueueUpdate right, ScheduledQueueUpdate left)
		{
			return !(right == left);
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

public class BattlegroundData
{
	public Dictionary<uint, Battleground> m_Battlegrounds = new();
	public List<uint>[] m_ClientBattlegroundIds = new List<uint>[(int)BattlegroundBracketId.Max];
	public Battleground Template;

	public BattlegroundData()
	{
		for (var i = 0; i < (int)BattlegroundBracketId.Max; ++i)
			m_ClientBattlegroundIds[i] = new List<uint>();
	}
}

public class BattlegroundTemplate
{
	public BattlegroundTypeId Id;
	public float MaxStartDistSq;
	public byte Weight;
	public uint ScriptId;
	public BattlemasterListRecord BattlemasterEntry;

	public bool IsArena()
	{
		return BattlemasterEntry.InstanceType == (uint)MapTypes.Arena;
	}

	public ushort GetMinPlayersPerTeam()
	{
		return (ushort)BattlemasterEntry.MinPlayers;
	}

	public ushort GetMaxPlayersPerTeam()
	{
		return (ushort)BattlemasterEntry.MaxPlayers;
	}

	public byte GetMinLevel()
	{
		return BattlemasterEntry.MinLevel;
	}

	public byte GetMaxLevel()
	{
		return BattlemasterEntry.MaxLevel;
	}
}