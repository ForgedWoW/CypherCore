// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Achievements;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Quest;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public partial class Player
{
    public PvPInfo PvpInfo;
    private readonly Dictionary<byte, ActionButton> _actionButtons = new();
    private readonly Dictionary<ObjectGuid, Loot> _aeLootView = new();
    private readonly double[] _auraBaseFlatMod = new double[(int)BaseModGroup.End];
    private readonly double[] _auraBasePctMod = new double[(int)BaseModGroup.End];
    //Combat
    private readonly int[] _baseRatingValue = new int[(int)CombatRating.Max];

    //PVP
    private readonly BgBattlegroundQueueIdRec[] _battlegroundQueueIdRecs = new BgBattlegroundQueueIdRec[SharedConst.MaxPlayerBGQueues];

    private readonly BgData _bgData;
    private readonly CufProfile[] _cufProfiles = new CufProfile[PlayerConst.MaxCUFProfiles];
    private readonly Dictionary<uint, PlayerCurrency> _currencyStorage = new();
    private readonly List<uint> _dfQuests = new();
    private readonly List<EnchantDuration> _enchantDurations = new();
    //Inventory
    private readonly Dictionary<ulong, EquipmentSetInfo> _equipmentSets = new();

    //Groups/Raids
    private readonly GroupUpdateCounter[] _groupUpdateSequences = new GroupUpdateCounter[2];

    private readonly TimeTracker _groupUpdateTimer;
    private readonly Dictionary<uint, long> _instanceResetTimes = new();
    private readonly List<Item> _itemDuration = new();
    private readonly Item[] _items = new Item[(int)PlayerSlots.Count];
    private readonly List<ObjectGuid> _itemSoulboundTradeable = new();
    private readonly long _logintime;
    private readonly LootFactory _lootFactory;
    private readonly List<LootRoll> _lootRolls = new();
    //Mail
    private readonly Dictionary<ulong, Item> _mailItems = new();

    private readonly int[] _mirrorTimer = new int[3];
    private readonly List<uint> _monthlyquests = new();
    private readonly Dictionary<uint, QuestStatusData> _mQuestStatus = new();
    private readonly MultiMap<uint, uint> _overrideSpells = new();
    // loot rolls waiting for answer
    private readonly double[] _powerFraction = new double[(int)PowerType.MaxPerClass];

    //Core
    private readonly QuestObjectiveCriteriaManager _questObjectiveCriteriaManager;

    private readonly MultiMap<(QuestObjectiveType Type, int ObjectID), QuestObjectiveStatusData> _questObjectiveStatus = new();
    private readonly Dictionary<uint, QuestSaveType> _questStatusSave = new();
    private readonly Dictionary<uint, uint> _recentInstances = new();
    private readonly List<ObjectGuid> _refundableItems = new();
    private readonly List<uint> _rewardedQuests = new();
    private readonly Dictionary<uint, QuestSaveType> _rewardedQuestsSave = new();
    private readonly Dictionary<uint, Dictionary<uint, long>> _seasonalquests = new();
    private readonly Dictionary<uint, SkillStatusData> _skillStatus = new();
    private readonly List<SpellModifier>[][] _spellModifiers = new List<SpellModifier>[(int)SpellModOp.Max][];
    //Spell
    private readonly Dictionary<uint, PlayerSpell> _spells = new();

    private readonly Dictionary<uint, StoredAuraTeleportLocation> _storedAuraTeleportLocations = new();
    //QuestId
    private readonly List<uint> _timedquests = new();

    private readonly Dictionary<int, PlayerSpellState> _traitConfigStates = new();
    private readonly VoidStorageItem[] _voidStorageItems = new VoidStorageItem[SharedConst.VoidStorageMaxSlot];
    private readonly List<uint> _weeklyquests = new();
    private readonly List<ObjectGuid> _whisperList = new();
    private PlayerAchievementMgr _achievementSys;
    private PlayerCommandStates _activeCheats;
    private uint _areaUpdateId;
    private uint _arenaTeamIdInvited;
    private uint _armorProficiency;
    private uint _baseHealthRegen;
    private uint _baseManaRegen;
    //Stats
    private uint _baseSpellPower;

    private bool _canTitanGrip;
    private uint _championingFaction;
    private uint _combatExitTime;
    private uint _contestedPvPTimer;
    private long _createTime;
    private uint _currentBuybackSlot;
    private bool _customizationsChanged;
    private bool _dailyQuestChanged;
    private long _deathExpireTime;
    private PlayerDelayedOperations _delayedOperations;
    private uint _drunkTimer;
    private PlayerExtraFlags _extraFlags;
    private byte _fishingSteps;
    private uint _foodEmoteTimerCount;
    // variables to save health and mana before duel and restore them after duel
    private ulong _healthBeforeDuel;

    private uint _homebindAreaId;
    private uint _homebindTimer;
    private uint _hostileReferenceCheckTimer;
    private uint _ingametime;
    private bool _isBgRandomWinner;
    private long _lastDailyQuestTime;
    private uint _lastFallTime;
    private float _lastFallZ;
    private long _lastHonorUpdateTime;
    private uint _lastPotionId;
    private long _lastTick;
    private uint _manaBeforeDuel;
    private PlayerUnderwaterState _mirrorTimerFlags;
    private PlayerUnderwaterState _mirrorTimerFlagsLast;
    private bool _monthlyQuestChanged;
    private long _nextMailDelivereTime;
    private uint _oldpetspell;
    private uint _pendingBindId;
    private uint _pendingBindTimer;
    private ObjectGuid _playerSharingQuest;
    // Recall position
    private uint _recallInstanceId;

    private uint _regenTimerCount;
    private ResurrectionData _resurrectionData;
    private Runes _runes = new();
    private bool _seasonalQuestChanged;
    private uint _sharedQuestId;
    private SpecializationInfo _specializationInfo;
    private int _spellPenetrationItemMod;
    // Player summoning
    private long _summonExpire;

    private uint _summonInstanceId;
    //Pets
    private WorldLocation _summonLocation;

    private byte _swingErrorMsg;
    private uint _titanGripPenaltySpellId;
    private TradeData _trade;
    private uint _weaponChangeTimer;
    private uint _weaponProficiency;
    private bool _weeklyQuestChanged;
    private uint _zoneUpdateId;
    private uint _zoneUpdateTimer;
    public ActivePlayerData ActivePlayerData { get; set; }
    public bool AutoAcceptQuickJoin { get; set; }
    public string AutoReplyMsg { get; set; }
    public List<ObjectGuid> ClientGuiDs { get; set; } = new();
    public DuelInfo Duel { get; set; }
    public float EmpoweredSpellMinHoldPct { get; set; }
    public byte[] ForcedSpeedChanges { get; set; } = new byte[(int)UnitMoveType.Max];
    public bool InstanceValid { get; set; }
    public bool IsDebugAreaTriggers { get; set; }
    public List<ItemSetEffect> ItemSetEff { get; } = new();
    public List<Item> ItemUpdateQueue { get; } = new();
    public bool ItemUpdateQueueBlocked { get; set; }
    public AtLoginFlags LoginFlags { get; set; }
    public bool MailsUpdated { get; set; }
    public byte MovementForceModMagnitudeChanges { get; set; }
    public bool OverrideScreenFlash { get; set; }

    public List<PetAura> PetAuras { get; set; } = new();

    public PlayerData PlayerData { get; set; }

    //Gossip
    public PlayerMenu PlayerTalkClass { get; set; }
    public WorldObject SeerView { get; set; }

    public WorldSession Session { get; }

    public PlayerSocial Social { get; private set; }

    public Spell SpellModTakingSpell { get; set; }

    //Movement
    public PlayerTaxi Taxi { get; set; } = new();
    public byte UnReadMails { get; set; }
    public List<ObjectGuid> VisibleTransports { get; set; } = new();
    private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
    {
        private readonly ActivePlayerData _activePlayerMask = new();
        private readonly ObjectFieldData _objectMask = new();
        private readonly Player _owner;
        private readonly PlayerData _playerMask = new();
        private readonly UnitData _unitMask = new();
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