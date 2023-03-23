// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Achievements;
using Framework.Constants;
using Game.Garrisons;
using Game.Loots;
using Game.Mails;
using Game.Spells;
using Game.Common.Chat.Channels;
using Game.Common.Groups;
using Game.Common.Loot;

namespace Game.Entities;

public partial class Player
{
	public PvPInfo PvpInfo;
	readonly List<Channel> _channels = new();
	readonly List<ObjectGuid> _whisperList = new();

	//Inventory
	readonly Dictionary<ulong, EquipmentSetInfo> _equipmentSets = new();
	readonly List<EnchantDuration> _enchantDurations = new();
	readonly List<Item> _itemDuration = new();
	readonly List<ObjectGuid> _itemSoulboundTradeable = new();
	readonly List<ObjectGuid> _refundableItems = new();
	readonly VoidStorageItem[] _voidStorageItems = new VoidStorageItem[SharedConst.VoidStorageMaxSlot];
	readonly Item[] _items = new Item[(int)PlayerSlots.Count];

	//PVP
	readonly BgBattlegroundQueueIdRec[] _battlegroundQueueIdRecs = new BgBattlegroundQueueIdRec[SharedConst.MaxPlayerBGQueues];
	readonly BgData _bgData;

	//Groups/Raids
	readonly GroupReference _group = new();
	readonly GroupReference _originalGroup = new();
	readonly GroupUpdateCounter[] _groupUpdateSequences = new GroupUpdateCounter[2];
	readonly Dictionary<uint, uint> _recentInstances = new();
	readonly Dictionary<uint, long> _instanceResetTimes = new();

	//Spell
	readonly Dictionary<uint, PlayerSpell> _spells = new();
	readonly Dictionary<uint, SkillStatusData> _skillStatus = new();
	readonly Dictionary<uint, PlayerCurrency> _currencyStorage = new();
	readonly List<SpellModifier>[][] _spellModifiers = new List<SpellModifier>[(int)SpellModOp.Max][];
	readonly MultiMap<uint, uint> _overrideSpells = new();
	readonly Dictionary<uint, StoredAuraTeleportLocation> _storedAuraTeleportLocations = new();

	//Mail
	readonly List<Mail> _mail = new();
	readonly Dictionary<ulong, Item> _mailItems = new();
	readonly RestMgr _restMgr;

	//Combat
	readonly int[] _baseRatingValue = new int[(int)CombatRating.Max];
	readonly double[] _auraBaseFlatMod = new double[(int)BaseModGroup.End];
	readonly double[] _auraBasePctMod = new double[(int)BaseModGroup.End];

	//Quest
	readonly List<uint> _timedquests = new();
	readonly List<uint> _weeklyquests = new();
	readonly List<uint> _monthlyquests = new();
	readonly Dictionary<uint, Dictionary<uint, long>> _seasonalquests = new();
	readonly Dictionary<uint, QuestStatusData> _mQuestStatus = new();
	readonly MultiMap<(QuestObjectiveType Type, int ObjectID), QuestObjectiveStatusData> _questObjectiveStatus = new();
	readonly Dictionary<uint, QuestSaveType> _questStatusSave = new();
	readonly List<uint> _dfQuests = new();
	readonly List<uint> _rewardedQuests = new();
	readonly Dictionary<uint, QuestSaveType> _rewardedQuestsSave = new();
	readonly CinematicManager _cinematicMgr;

	//Core
	readonly WorldSession _session;
	readonly QuestObjectiveCriteriaManager _questObjectiveCriteriaManager;
	readonly WorldLocation _homebind = new();
	readonly SceneMgr _sceneMgr;
	readonly Dictionary<ObjectGuid, Loot> _aeLootView = new();
	readonly List<LootRoll> _lootRolls = new(); // loot rolls waiting for answer

	readonly CufProfile[] _cufProfiles = new CufProfile[PlayerConst.MaxCUFProfiles];
	readonly double[] _powerFraction = new double[(int)PowerType.MaxPerClass];
	readonly int[] _mirrorTimer = new int[3];
	readonly TimeTracker _groupUpdateTimer;
	readonly long _logintime;
	readonly Dictionary<int, PlayerSpellState> _traitConfigStates = new();
	readonly Dictionary<byte, ActionButton> _actionButtons = new();
	PlayerSocial _social;
	uint _weaponProficiency;
	uint _armorProficiency;
	uint _currentBuybackSlot;
	TradeData _trade;
	bool _isBgRandomWinner;
	uint _arenaTeamIdInvited;
	long _lastHonorUpdateTime;
	uint _contestedPvPTimer;
	bool _usePvpItemLevels;
	PlayerGroup _groupInvite;
	GroupUpdateFlags _groupUpdateFlags;
	bool _bPassOnGroupLoot;
	uint _pendingBindId;
	uint _pendingBindTimer;

	Difficulty _dungeonDifficulty;
	Difficulty _raidDifficulty;
	Difficulty _legacyRaidDifficulty;
	uint _lastFallTime;
	float _lastFallZ;
	WorldLocation _teleportDest;
	uint? _teleportInstanceId;
	TeleportToOptions _teleportOptions;
	bool _semaphoreTeleportNear;
	bool _semaphoreTeleportFar;
	PlayerDelayedOperations _delayedOperations;
	bool _canDelayTeleport;
	bool _hasDelayedTeleport;

	PlayerUnderwaterState _mirrorTimerFlags;
	PlayerUnderwaterState _mirrorTimerFlagsLast;

	//Stats
	uint _baseSpellPower;
	uint _baseManaRegen;
	uint _baseHealthRegen;
	int _spellPenetrationItemMod;
	uint _lastPotionId;
	uint _oldpetspell;
	long _nextMailDelivereTime;

	//Pets
	PetStable _petStable;
	uint _temporaryUnsummonedPetNumber;
	uint _lastpetnumber;

	// Player summoning
	long _summonExpire;
	WorldLocation _summonLocation;
	uint _summonInstanceId;
	bool _canParry;
	bool _canBlock;
	bool _canTitanGrip;
	uint _titanGripPenaltySpellId;
	uint _deathTimer;
	long _deathExpireTime;
	byte _swingErrorMsg;
	uint _combatExitTime;
	uint _regenTimerCount;
	uint _foodEmoteTimerCount;
	uint _weaponChangeTimer;

	bool _dailyQuestChanged;
	bool _weeklyQuestChanged;
	bool _monthlyQuestChanged;
	bool _seasonalQuestChanged;
	long _lastDailyQuestTime;

	Garrison _garrison;

	// variables to save health and mana before duel and restore them after duel
	ulong _healthBeforeDuel;
	uint _manaBeforeDuel;

	bool _advancedCombatLoggingEnabled;

	WorldLocation _corpseLocation;

	long _createTime;
	PlayerCreateMode _createMode;

	uint _nextSave;
	byte _cinematic;

	uint _movie;
	bool _customizationsChanged;

	SpecializationInfo _specializationInfo;
	TeamFaction _team;
	ReputationMgr _reputationMgr;

	PlayerExtraFlags _extraFlags;
	uint _zoneUpdateId;
	uint _areaUpdateId;
	uint _zoneUpdateTimer;

	uint _championingFaction;
	byte _fishingSteps;

	// Recall position
	WorldLocation _recallLocation;
	uint _recallInstanceId;
	uint _homebindAreaId;
	uint _homebindTimer;

	ResurrectionData _resurrectionData;

	PlayerAchievementMgr _AchievementSys;

	ulong _guildIdInvited;
	DeclinedName _declinedname;
	Runes _runes = new();
	uint _hostileReferenceCheckTimer;
	uint _drunkTimer;
	long _lastTick;
	uint _playedTimeTotal;
	uint _playedTimeLevel;
	ObjectGuid _playerSharingQuest;
	uint _sharedQuestId;
	uint _ingametime;

	PlayerCommandStates _activeCheats;
	public bool AutoAcceptQuickJoin { get; set; }
	public bool OverrideScreenFlash { get; set; }

	//Gossip
	public PlayerMenu PlayerTalkClass { get; set; }
	public string AutoReplyMsg { get; set; }
	public List<ItemSetEffect> ItemSetEff { get; } = new();
	public List<Item> ItemUpdateQueue { get; } = new();
	public bool InstanceValid { get; set; }

	//Movement
	public PlayerTaxi Taxi { get; set; } = new();
	public byte[] ForcedSpeedChanges { get; set; } = new byte[(int)UnitMoveType.Max];
	public byte MovementForceModMagnitudeChanges { get; set; }
	public Spell SpellModTakingSpell { get; set; }
	public float EmpoweredSpellMinHoldPct { get; set; }
	public byte UnReadMails { get; set; }
	public bool MailsUpdated { get; set; }
	public List<PetAura> PetAuras { get; set; } = new();
	public DuelInfo Duel { get; set; }

	public PlayerData PlayerData { get; set; }
	public ActivePlayerData ActivePlayerData { get; set; }
	public List<ObjectGuid> ClientGuiDs { get; set; } = new();
	public List<ObjectGuid> VisibleTransports { get; set; } = new();
	public WorldObject SeerView { get; set; }
	public AtLoginFlags LoginFlags { get; set; }
	public bool ItemUpdateQueueBlocked { get; set; }

	public bool IsDebugAreaTriggers { get; set; }

	public WorldSession Session => _session;

	public PlayerSocial Social => _social;

	class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
	{
		readonly Player _owner;
		readonly ObjectFieldData _objectMask = new();
		readonly UnitData _unitMask = new();
		readonly PlayerData _playerMask = new();
		readonly ActivePlayerData _activePlayerMask = new();

		public ValuesUpdateForPlayerWithMaskSender(Player owner)
		{
			_owner = owner;
		}

		public void Invoke(Player player)
		{
			UpdateData udata = new(_owner.Location.MapId);

			_owner.BuildValuesUpdateForPlayerWithMask(udata, _objectMask.GetUpdateMask(), _unitMask.GetUpdateMask(), _playerMask.GetUpdateMask(), _activePlayerMask.GetUpdateMask(), player);

			udata.BuildPacket(out var packet);
			player.SendPacket(packet);
		}
	}
}

// Holder for Battlegrounddata