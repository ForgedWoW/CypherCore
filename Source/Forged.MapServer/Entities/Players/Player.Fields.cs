// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Achievements;
using Forged.MapServer.Chat.Channels;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Garrisons;
using Forged.MapServer.Groups;
using Forged.MapServer.Mails;
using Forged.MapServer.Quest;
using Forged.MapServer.Reputation;
using Forged.MapServer.Server;
using Forged.MapServer.Services;
using Forged.MapServer.Spells;
using Framework.Constants;
using Forged.MapServer.LootManagement;

namespace Forged.MapServer.Entities.Players;

public partial class Player
{
	public PvPInfo PvpInfo;
    private readonly LootFactory _lootFactory;
    private readonly List<Channel> _channels = new();
    private readonly List<ObjectGuid> _whisperList = new();

	//Inventory
    private readonly Dictionary<ulong, EquipmentSetInfo> _equipmentSets = new();
    private readonly List<EnchantDuration> _enchantDurations = new();
    private readonly List<Item> _itemDuration = new();
    private readonly List<ObjectGuid> _itemSoulboundTradeable = new();
    private readonly List<ObjectGuid> _refundableItems = new();
    private readonly VoidStorageItem[] _voidStorageItems = new VoidStorageItem[SharedConst.VoidStorageMaxSlot];
    private readonly Item[] _items = new Item[(int)PlayerSlots.Count];

	//PVP
    private readonly BgBattlegroundQueueIdRec[] _battlegroundQueueIdRecs = new BgBattlegroundQueueIdRec[SharedConst.MaxPlayerBGQueues];
    private readonly BgData _bgData;

	//Groups/Raids
    private readonly GroupReference _group = new();
    private readonly GroupReference _originalGroup = new();
    private readonly GroupUpdateCounter[] _groupUpdateSequences = new GroupUpdateCounter[2];
    private readonly Dictionary<uint, uint> _recentInstances = new();
    private readonly Dictionary<uint, long> _instanceResetTimes = new();

	//Spell
    private readonly Dictionary<uint, PlayerSpell> _spells = new();
    private readonly Dictionary<uint, SkillStatusData> _skillStatus = new();
    private readonly Dictionary<uint, PlayerCurrency> _currencyStorage = new();
    private readonly List<SpellModifier>[][] _spellModifiers = new List<SpellModifier>[(int)SpellModOp.Max][];
    private readonly MultiMap<uint, uint> _overrideSpells = new();
    private readonly Dictionary<uint, StoredAuraTeleportLocation> _storedAuraTeleportLocations = new();

	//Mail
    private readonly List<Mail> _mail = new();
    private readonly Dictionary<ulong, Item> _mailItems = new();
    private readonly RestMgr _restMgr;

	//Combat
    private readonly int[] _baseRatingValue = new int[(int)CombatRating.Max];
    private readonly double[] _auraBaseFlatMod = new double[(int)BaseModGroup.End];
    private readonly double[] _auraBasePctMod = new double[(int)BaseModGroup.End];

	//Quest
    private readonly List<uint> _timedquests = new();
    private readonly List<uint> _weeklyquests = new();
    private readonly List<uint> _monthlyquests = new();
    private readonly Dictionary<uint, Dictionary<uint, long>> _seasonalquests = new();
    private readonly Dictionary<uint, QuestStatusData> _mQuestStatus = new();
    private readonly MultiMap<(QuestObjectiveType Type, int ObjectID), QuestObjectiveStatusData> _questObjectiveStatus = new();
    private readonly Dictionary<uint, QuestSaveType> _questStatusSave = new();
    private readonly List<uint> _dfQuests = new();
    private readonly List<uint> _rewardedQuests = new();
    private readonly Dictionary<uint, QuestSaveType> _rewardedQuestsSave = new();
    private readonly CinematicManager _cinematicMgr;

	//Core
    private readonly WorldSession _session;
    private readonly QuestObjectiveCriteriaManager _questObjectiveCriteriaManager;
    private readonly WorldLocation _homebind = new();
    private readonly SceneMgr _sceneMgr;
    private readonly Dictionary<ObjectGuid, Forged.MapServer.LootManagement.Loot> _aeLootView = new();
    private readonly List<LootRoll> _lootRolls = new(); // loot rolls waiting for answer

    private readonly CufProfile[] _cufProfiles = new CufProfile[PlayerConst.MaxCUFProfiles];
    private readonly double[] _powerFraction = new double[(int)PowerType.MaxPerClass];
    private readonly int[] _mirrorTimer = new int[3];
    private readonly TimeTracker _groupUpdateTimer;
    private readonly long _logintime;
    private readonly Dictionary<int, PlayerSpellState> _traitConfigStates = new();
    private readonly Dictionary<byte, ActionButton> _actionButtons = new();
    private PlayerSocial _social;
    private uint _weaponProficiency;
    private uint _armorProficiency;
    private uint _currentBuybackSlot;
    private TradeData _trade;
    private bool _isBgRandomWinner;
    private uint _arenaTeamIdInvited;
    private long _lastHonorUpdateTime;
    private uint _contestedPvPTimer;
    private bool _usePvpItemLevels;
    private PlayerGroup _groupInvite;
    private GroupUpdateFlags _groupUpdateFlags;
    private bool _bPassOnGroupLoot;
    private uint _pendingBindId;
    private uint _pendingBindTimer;

    private Difficulty _dungeonDifficulty;
    private Difficulty _raidDifficulty;
    private Difficulty _legacyRaidDifficulty;
    private uint _lastFallTime;
    private float _lastFallZ;
    private WorldLocation _teleportDest;
    private uint? _teleportInstanceId;
    private TeleportToOptions _teleportOptions;
    private bool _semaphoreTeleportNear;
    private bool _semaphoreTeleportFar;
    private PlayerDelayedOperations _delayedOperations;
    private bool _canDelayTeleport;
    private bool _hasDelayedTeleport;

    private PlayerUnderwaterState _mirrorTimerFlags;
    private PlayerUnderwaterState _mirrorTimerFlagsLast;

	//Stats
    private uint _baseSpellPower;
    private uint _baseManaRegen;
    private uint _baseHealthRegen;
    private int _spellPenetrationItemMod;
    private uint _lastPotionId;
    private uint _oldpetspell;
    private long _nextMailDelivereTime;

	//Pets
    private PetStable _petStable;
    private uint _temporaryUnsummonedPetNumber;
    private uint _lastpetnumber;

	// Player summoning
    private long _summonExpire;
    private WorldLocation _summonLocation;
    private uint _summonInstanceId;
    private bool _canParry;
    private bool _canBlock;
    private bool _canTitanGrip;
    private uint _titanGripPenaltySpellId;
    private uint _deathTimer;
    private long _deathExpireTime;
    private byte _swingErrorMsg;
    private uint _combatExitTime;
    private uint _regenTimerCount;
    private uint _foodEmoteTimerCount;
    private uint _weaponChangeTimer;

    private bool _dailyQuestChanged;
    private bool _weeklyQuestChanged;
    private bool _monthlyQuestChanged;
    private bool _seasonalQuestChanged;
    private long _lastDailyQuestTime;

    private Garrison _garrison;

	// variables to save health and mana before duel and restore them after duel
    private ulong _healthBeforeDuel;
    private uint _manaBeforeDuel;

    private bool _advancedCombatLoggingEnabled;

    private WorldLocation _corpseLocation;

    private long _createTime;
    private PlayerCreateMode _createMode;

    private uint _nextSave;
    private byte _cinematic;

    private uint _movie;
    private bool _customizationsChanged;

    private SpecializationInfo _specializationInfo;
    private TeamFaction _team;
    private ReputationMgr _reputationMgr;

    private PlayerExtraFlags _extraFlags;
    private uint _zoneUpdateId;
    private uint _areaUpdateId;
    private uint _zoneUpdateTimer;

    private uint _championingFaction;
    private byte _fishingSteps;

	// Recall position
    private WorldLocation _recallLocation;
    private uint _recallInstanceId;
    private uint _homebindAreaId;
    private uint _homebindTimer;

    private ResurrectionData _resurrectionData;

    private PlayerAchievementMgr _AchievementSys;

    private ulong _guildIdInvited;
    private DeclinedName _declinedname;
    private Runes _runes = new();
    private uint _hostileReferenceCheckTimer;
    private uint _drunkTimer;
    private long _lastTick;
    private uint _playedTimeTotal;
    private uint _playedTimeLevel;
    private ObjectGuid _playerSharingQuest;
    private uint _sharedQuestId;
    private uint _ingametime;

    private PlayerCommandStates _activeCheats;
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

    private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
	{
        private readonly Player _owner;
        private readonly ObjectFieldData _objectMask = new();
        private readonly UnitData _unitMask = new();
        private readonly PlayerData _playerMask = new();
        private readonly ActivePlayerData _activePlayerMask = new();

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