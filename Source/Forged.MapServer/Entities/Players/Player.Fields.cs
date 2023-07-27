// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Accounts;
using Forged.MapServer.Achievements;
using Forged.MapServer.AI.PlayerAI;
using Forged.MapServer.Arenas;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Chat;
using Forged.MapServer.Chat.Channels;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DungeonFinding;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Events;
using Forged.MapServer.Garrisons;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Groups;
using Forged.MapServer.Guilds;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Mails;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Miscellaneous;
using Forged.MapServer.Quest;
using Forged.MapServer.Reputation;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Skills;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using System;
using System.Collections.Generic;
using System.Linq;

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
    private uint _weaponChangeTimer;
    private uint _weaponProficiency;
    private bool _weeklyQuestChanged;
    private uint _zoneUpdateId;
    private uint _zoneUpdateTimer;
    public AccountManager AccountManager { get; }
    public uint AchievementPoints => _achievementSys.AchievementPoints;
    public ActivePlayerData ActivePlayerData { get; set; }
    public override PlayerAI AI => Ai as PlayerAI;
    public ArenaTeamManager ArenaTeamManager { get; }
    public bool AutoAcceptQuickJoin { get; set; }
    public string AutoReplyMsg { get; set; }
    public Battleground Battleground => BattlegroundId == 0 ? null : BattlegroundManager.GetBattleground(BattlegroundId, _bgData.BgTypeId);
    public WorldLocation BattlegroundEntryPoint => _bgData.JoinPos;
    public uint BattlegroundId => _bgData.BgInstanceId;
    public BattlegroundManager BattlegroundManager { get; }
    public BattlegroundTypeId BattlegroundTypeId => _bgData.BgTypeId;
    public bool CanBeGameMaster => Session.HasPermission(RBACPermissions.CommandGm);
    public bool CanBlock { get; private set; }

    public bool CanCaptureTowerPoint => !HasStealthAura &&      // not stealthed
                                        !HasInvisibilityAura && // not invisible
                                        IsAlive;

    public override bool CanEnterWater => true;
    public override bool CanFly => MovementInfo.HasMovementFlag(MovementFlag.CanFly);
    public bool CanParry { get; private set; }
    public bool CanTameExoticPets => IsGameMaster || HasAuraType(AuraType.AllowTamePetType);
    public ChannelManagerFactory ChannelManagerFactory { get; }
    public CharacterDatabase CharacterDatabase { get; }
    public CharacterTemplateDataStorage CharacterTemplateDataStorage { get; }

    public ChatFlags ChatFlags
    {
        get
        {
            var tag = ChatFlags.None;

            if (IsGMChat)
                tag |= ChatFlags.GM;

            if (IsDnd)
                tag |= ChatFlags.DND;

            if (IsAfk)
                tag |= ChatFlags.AFK;

            if (IsDeveloper)
                tag |= ChatFlags.Dev;

            return tag;
        }
    }

    public byte Cinematic { get; set; }
    public CinematicManager CinematicMgr { get; }
    public List<ObjectGuid> ClientGuiDs { get; set; } = new();
    public CollectionMgr CollectionMgr { get; }
    public Corpse Corpse => Location.Map.GetCorpseByPlayer(GUID);
    public WorldLocation CorpseLocation { get; private set; }
    public PlayerCreateMode CreateMode { get; private set; }
    public byte CufProfilesCount => (byte)_cufProfiles.Count(p => p != null);

    public Pet CurrentPet
    {
        get
        {
            var petGuid = PetGUID;

            if (petGuid.IsEmpty)
                return null;

            if (!petGuid.IsPet)
                return null;

            var pet = ObjectAccessor.GetPet(this, petGuid);

            if (pet == null)
                return null;

            return Location.IsInWorld ? pet : null;
        }
    }

    public uint DeathTimer { get; private set; }
    public DeclinedName DeclinedNames { get; private set; }
    public byte DrunkValue => PlayerData.Inebriation;
    public DuelInfo Duel { get; set; }
    public TeamFaction EffectiveTeam => HasPlayerFlagEx(PlayerFlagsEx.MercenaryMode) ? Team == TeamFaction.Alliance ? TeamFaction.Horde : TeamFaction.Alliance : Team;
    public int EffectiveTeamId => EffectiveTeam == TeamFaction.Alliance ? TeamIds.Alliance : TeamIds.Horde;
    public float EmpoweredSpellMinHoldPct { get; set; }
    public byte[] ForcedSpeedChanges { get; set; } = new byte[(int)UnitMoveType.Max];
    public Formulas Formulas { get; }
    public uint FreePrimaryProfessionPoints => ActivePlayerData.CharacterPoints;
    public GameEventManager GameEventManager { get; }
    public Garrison Garrison { get; private set; }
    public PlayerGroup Group => GroupRef.Target;
    public PlayerGroup GroupInvite { get; set; }
    public GroupManager GroupManager { get; }
    public GroupReference GroupRef { get; } = new();
    public GroupUpdateFlags GroupUpdateFlag { get; private set; }

    public Guild Guild
    {
        get
        {
            var guildId = GuildId;

            return guildId != 0 ? GuildMgr.GetGuildById(guildId) : null;
        }
    }

    public ulong GuildId => ((ObjectGuid)UnitData.GuildGUID).Counter;

    public ulong GuildIdInvited { get; set; }

    public uint GuildLevel
    {
        get => PlayerData.GuildLevel;
        set => SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.GuildLevel), value);
    }

    public GuildManager GuildMgr { get; }

    public string GuildName => GuildId != 0 ? GuildMgr.GetGuildById(GuildId).GetName() : "";

    public uint GuildRank => PlayerData.GuildRankID;

    public bool HasCorpse => CorpseLocation != null && CorpseLocation.MapId != 0xFFFFFFFF;

    public bool HasFreeBattlegroundQueueId
    {
        get
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (_battlegroundQueueIdRecs[i].BgQueueTypeId == default)
                    return true;

            return false;
        }
    }

    //Binds
    public bool HasPendingBind => _pendingBindId > 0;

    public bool HasSummonPending => _summonExpire >= GameTime.CurrentTime;
    public WorldLocation Homebind { get; } = new();
    public uint HonorLevel => PlayerData.HonorLevel;

    public bool InArena => Battleground != null && Battleground.IsArena;
    public bool InBattleground => _bgData.BgInstanceId != 0;

    public bool InRandomLfgDungeon
    {
        get
        {
            if (!LFGManager.SelectedRandomLfgDungeon(GUID))
                return false;

            var map = Location.Map;

            return LFGManager.InLfgDungeonMap(GUID, map.Id, map.DifficultyID);
        }
    }

    public InstanceLockManager InstanceLockManager { get; }
    public bool InstanceValid { get; set; }
    public bool IsAcceptWhispers => _extraFlags.HasAnyFlag(PlayerExtraFlags.AcceptWhispers);
    public bool IsAdvancedCombatLoggingEnabled { get; private set; }
    public bool IsAfk => HasPlayerFlag(PlayerFlags.AFK);
    public bool IsBeingTeleported => IsBeingTeleportedNear || IsBeingTeleportedFar;
    public bool IsBeingTeleportedFar { get; private set; }
    public bool IsBeingTeleportedNear { get; private set; }
    public bool IsBeingTeleportedSeamlessly => IsBeingTeleportedFar && TeleportOptions.HasAnyFlag(TeleportToOptions.Seamless);
    public bool IsDebugAreaTriggers { get; set; }

    //GM
    public bool IsDeveloper => HasPlayerFlag(PlayerFlags.Developer);

    public bool IsDnd => HasPlayerFlag(PlayerFlags.DND);
    public bool IsGameMaster => _extraFlags.HasAnyFlag(PlayerExtraFlags.GMOn);
    public bool IsGameMasterAcceptingWhispers => IsGameMaster && IsAcceptWhispers;
    public bool IsGMChat => _extraFlags.HasAnyFlag(PlayerExtraFlags.GMChat);
    public bool IsGMVisible => !_extraFlags.HasAnyFlag(PlayerExtraFlags.GMInvisible);
    public override bool IsLoading => Session.PlayerLoading;
    public bool IsMaxHonorLevel => HonorLevel == PlayerConst.MaxHonorLevel;

    public bool IsMaxLevel
    {
        get
        {
            if (Configuration.GetDefaultValue("character:MaxLevelDeterminedByConfig", false))
                return Level >= Configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);

            return Level >= ActivePlayerData.MaxLevel;
        }
    }

    public bool IsReagentBankUnlocked => HasPlayerFlagEx(PlayerFlagsEx.ReagentBankUnlocked);
    public bool IsResurrectRequested => _resurrectionData != null;
    public bool IsTaxiCheater => _extraFlags.HasAnyFlag(PlayerExtraFlags.TaxiCheat);
    public bool IsUsingLfg => LFGManager.GetState(GUID) != LfgState.None;
    public bool IsUsingPvpItemLevels { get; private set; }
    public bool IsWarModeLocalActive => HasPlayerLocalFlag(PlayerLocalFlags.WarMode);
    public ItemEnchantmentManager ItemEnchantmentManager { get; }
    public List<ItemSetEffect> ItemSetEff { get; } = new();
    public List<Item> ItemUpdateQueue { get; } = new();
    public bool ItemUpdateQueueBlocked { get; set; }
    public List<Channel> JoinedChannels { get; } = new();
    public LanguageManager LanguageManager { get; }

    // last used pet number (for BG's)
    public uint LastPetNumber { get; set; }

    public uint LevelPlayedTime { get; private set; }
    public LFGManager LFGManager { get; }
    public LoginDatabase LoginDatabase { get; }
    public AtLoginFlags LoginFlags { get; set; }
    public LootFactory LootFactory { get; }
    public LootItemStorage LootItemStorage { get; }
    public List<Mail> Mails { get; } = new();
    public uint MailSize => (uint)Mails.Count;
    public bool MailsUpdated { get; set; }
    public MapManager MapManager { get; }

    //Money
    public ulong Money
    {
        get => ActivePlayerData.Coinage;
        set
        {
            var loading = Session.PlayerLoading;

            if (!loading)
                MoneyChanged(value);

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Coinage), value);

            if (!loading)
                UpdateCriteria(CriteriaType.MostMoneyOwned);
        }
    }

    public byte MovementForceModMagnitudeChanges { get; set; }
    public uint Movie { get; set; }

    public override Gender NativeGender
    {
        get => (Gender)(byte)PlayerData.NativeSex;
        set => SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.NativeSex), (byte)value);
    }

    public byte NumRespecs => ActivePlayerData.NumRespecs;

    public override float ObjectScale
    {
        get => base.ObjectScale;
        set
        {
            base.ObjectScale = value;

            BoundingRadius = value * SharedConst.DefaultPlayerBoundingRadius;
            SetCombatReach(value * SharedConst.DefaultPlayerCombatReach);

            if (Location.IsInWorld)
                SendMovementSetCollisionHeight(CollisionHeight, UpdateCollisionHeightReason.Scale);
        }
    }

    public PlayerGroup OriginalGroup => OriginalGroupRef.Target;
    public GroupReference OriginalGroupRef { get; } = new();
    public byte OriginalSubGroup => OriginalGroupRef.SubGroup;
    public bool OverrideScreenFlash { get; set; }
    public bool PassOnGroupLoot { get; set; }
    public List<PetAura> PetAuras { get; set; } = new();
    public PetStable PetStable { get; set; } = new();
    public PlayerComputators PlayerComputators { get; }
    public PlayerData PlayerData { get; set; }
    public ItemFactory ItemFactory { get; set; }
    public AzeriteItemFactory AzeriteItemFactory { get; set; }
    public AzeriteEmpoweredItemFactory AzeriteEmpoweredItemFactory { get; set; }
    public AccessRequirementsCache AccessRequirementsManager { get; }

    //Gossip
    public PlayerMenu PlayerTalkClass { get; set; }

    public RealmManager RealmManager { get; }
    public WorldLocation Recall { get; private set; }
    public ReputationMgr ReputationMgr { get; private set; }
    public RestMgr RestMgr { get; }
    public uint SaveTimer { get; private set; }
    public SceneMgr SceneMgr { get; }
    public WorldObject SeerView { get; set; }
    public Player SelectedPlayer => !Target.IsEmpty ? ObjectAccessor.GetPlayer(this, Target) : null;
    public Unit SelectedUnit => !Target.IsEmpty ? ObjectAccessor.GetUnit(this, Target) : null;
    public WorldSession Session { get; }
    public SkillDiscovery SkillDiscovery { get; }
    public PlayerSocial Social { get; private set; }
    public SocialManager SocialManager { get; }
    public Spell SpellModTakingSpell { get; set; }
    public byte SubGroup => GroupRef.SubGroup;
    public ObjectGuid SummonedBattlePetGUID => ActivePlayerData.SummonedBattlePetGUID;

    //Movement
    public PlayerTaxi Taxi { get; set; }

    public TeamFaction Team { get; private set; }
    public int TeamId => Team == TeamFaction.Alliance ? TeamIds.Alliance : TeamIds.Horde;
    public WorldLocation TeleportDest { get; private set; }
    public uint? TeleportDestInstanceId { get; private set; }
    public TeleportToOptions TeleportOptions { get; private set; }
    public uint TemporaryUnsummonedPetNumber { get; set; }
    public TerrainManager TerrainManager { get; }

    //Misc
    public uint TotalPlayedTime { get; private set; }

    public TradeData TradeData { get; private set; }

    public Player Trader => TradeData?.Trader;
    public TraitMgr TraitMgr { get; }
    public byte UnReadMails { get; set; }

    public WorldObject Viewpoint
    {
        get
        {
            ObjectGuid guid = ActivePlayerData.FarsightObject;

            return !guid.IsEmpty ? ObjectAccessor.GetObjectByTypeMask(this, guid, TypeMask.Seer) : null;
        }
    }

    public List<ObjectGuid> VisibleTransports { get; set; } = new();
    public WorldManager WorldMgr { get; }
    public WorldStateManager WorldStateManager { get; }

    public uint XP
    {
        get => ActivePlayerData.XP;
        set
        {
            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.XP), value);

            var playerLevelDelta = 0;

            // If XP < 50%, player should see scaling creature with -1 level except for level max
            if (Level < SharedConst.MaxLevel && value < ActivePlayerData.NextLevelXP / 2)
                playerLevelDelta = -1;

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.ScalingPlayerLevelDelta), playerLevelDelta);
        }
    }

    public uint XPForNextLevel => ActivePlayerData.NextLevelXP;

    //Movement
    private bool IsCanDelayTeleport { get; set; }

    private bool IsHasDelayedTeleport { get; set; }

    private bool IsInFriendlyArea
    {
        get
        {
            var areaEntry = CliDB.AreaTableStorage.LookupByKey(Location.Area);

            return areaEntry != null && IsFriendlyArea(areaEntry);
        }
    }

    private bool IsTotalImmune
    {
        get
        {
            var immune = GetAuraEffectsByType(AuraType.SchoolImmunity);

            var immuneMask = 0;

            foreach (var eff in immune)
            {
                immuneMask |= eff.MiscValue;

                if (Convert.ToBoolean(immuneMask & (int)SpellSchoolMask.All)) // total immunity
                    return true;
            }

            return false;
        }
    }

    private bool IsWarModeActive => HasPlayerFlag(PlayerFlags.WarModeActive);
    private bool IsWarModeDesired => HasPlayerFlag(PlayerFlags.WarModeDesired);
}