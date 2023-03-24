// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using Blizzard.Telemetry.Wow;
using Framework.Constants;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;
using Game.Common.Entities.Units;
using Game.Common.Groups;
using Game.Common.Guilds;
using Game.Common.Server;

namespace Game.Common.Entities.Players;

public partial class Player : Unit
{
    
	//Groups/Raids
	readonly GroupReference _group = new();
	readonly GroupReference _originalGroup = new();
	readonly GroupUpdateCounter[] _groupUpdateSequences = new GroupUpdateCounter[2];
	readonly Dictionary<uint, uint> _recentInstances = new();
	readonly Dictionary<uint, long> _instanceResetTimes = new();


	//Core
	readonly WorldSession _session;
	readonly WorldLocation _homebind = new();
	readonly SceneMgr _sceneMgr;

	readonly CufProfile[] _cufProfiles = new CufProfile[PlayerConst.MaxCUFProfiles];
	readonly int[] _mirrorTimer = new int[3];
	readonly long _logintime;
	PlayerSocial _social;
	uint _weaponProficiency;
	uint _armorProficiency;
	uint _currentBuybackSlot;
	TradeData _trade;
	bool _isBgRandomWinner;
	uint _arenaTeamIdInvited;
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



	Garrison _garrison;

	// variables to save health and mana before duel and restore them after duel
	ulong _healthBeforeDuel;
	uint _manaBeforeDuel;

	bool _advancedCombatLoggingEnabled;

	WorldLocation _corpseLocation;

	long _createTime;
	PlayerCreateMode _createMode;

	SpecializationInfo _specializationInfo;
	TeamFaction _team;

	PlayerExtraFlags _extraFlags;

	// Recall position
	WorldLocation _recallLocation;
	uint _recallInstanceId;
	uint _homebindAreaId;
	uint _homebindTimer;

	ResurrectionData _resurrectionData;
	

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


	public PlayerData PlayerData { get; set; }
	public ActivePlayerData ActivePlayerData { get; set; }
	public WorldObject SeerView { get; set; }
	public AtLoginFlags LoginFlags { get; set; }

	public WorldSession Session => _session;

	public PlayerSocial Social => _social;

    public PlayerGroup GroupInvite
    {
        get => _groupInvite;
        set => _groupInvite = value;
    }

    public PlayerGroup Group => _group.Target;

    public GroupReference GroupRef => _group;

    public byte SubGroup => _group.SubGroup;

    public PlayerGroup OriginalGroup => _originalGroup.Target;

    public override bool IsLoading => Session.IsPlayerLoading;

    public DeclinedName DeclinedNames => _declinedname;


    public bool IsAdvancedCombatLoggingEnabled => _advancedCombatLoggingEnabled;


    //GM
    public bool IsDeveloper => HasPlayerFlag(PlayerFlags.Developer);

    public bool IsAcceptWhispers => _extraFlags.HasAnyFlag(PlayerExtraFlags.AcceptWhispers);

    public bool IsGameMaster => _extraFlags.HasAnyFlag(PlayerExtraFlags.GMOn);

    public bool IsGameMasterAcceptingWhispers => IsGameMaster && IsAcceptWhispers;

    public bool CanBeGameMaster => Session.HasPermission(RBACPermissions.CommandGm);

    public bool IsGMChat => _extraFlags.HasAnyFlag(PlayerExtraFlags.GMChat);

    public bool IsTaxiCheater => _extraFlags.HasAnyFlag(PlayerExtraFlags.TaxiCheat);

    public bool IsGMVisible => !_extraFlags.HasAnyFlag(PlayerExtraFlags.GMInvisible);

    //Binds
    public bool HasPendingBind => _pendingBindId > 0;

    //Misc
    public uint TotalPlayedTime => _playedTimeTotal;

    public uint LevelPlayedTime => _playedTimeLevel;

    public CinematicManager CinematicMgr => _cinematicMgr;

    public bool HasCorpse => _corpseLocation != null && _corpseLocation.MapId != 0xFFFFFFFF;

    public WorldLocation CorpseLocation => _corpseLocation;

    public override bool CanFly => MovementInfo.HasMovementFlag(MovementFlag.CanFly);

    public override bool CanEnterWater => true;
    
    public TeamFaction Team => _team;

    public int TeamId => _team == TeamFaction.Alliance ? TeamIds.Alliance : TeamIds.Horde;


    public ulong GuildId => ((ObjectGuid)UnitData.GuildGUID).Counter;

    public Guild Guild
    {
        get
        {
            var guildId = GuildId;

            return guildId != 0 ? Global.GuildMgr.GetGuildById(guildId) : null;
        }
    }

    public uint XPForNextLevel => ActivePlayerData.NextLevelXP;
    public uint DeathTimer => _deathTimer;

    public bool IsBeingTeleportedFar => _semaphoreTeleportFar;

    public bool IsBeingTeleportedSeamlessly => IsBeingTeleportedFar && _teleportOptions.HasAnyFlag(TeleportToOptions.Seamless);

    public bool IsReagentBankUnlocked => HasPlayerFlagEx(PlayerFlagsEx.ReagentBankUnlocked);

    public uint FreePrimaryProfessionPoints => ActivePlayerData.CharacterPoints;

    public WorldObject Viewpoint
    {
        get
        {
            ObjectGuid guid = ActivePlayerData.FarsightObject;

            if (!guid.IsEmpty)
                return Global.ObjAccessor.GetObjectByTypeMask(this, guid, TypeMask.Seer);

            return null;
        }
    }

    public WorldLocation TeleportDest => _teleportDest;

    public uint? TeleportDestInstanceId => _teleportInstanceId;

    public WorldLocation Homebind => _homebind;

    public WorldLocation Recall1 => _recallLocation;

    public override Gender NativeGender
    {
        get => (Gender)(byte)PlayerData.NativeSex;
        set => SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.NativeSex), (byte)value);
    }

    public ObjectGuid SummonedBattlePetGUID => ActivePlayerData.SummonedBattlePetGUID;

    public byte NumRespecs => ActivePlayerData.NumRespecs;

    //Movement
    bool IsCanDelayTeleport => _canDelayTeleport;

    bool IsHasDelayedTeleport => _hasDelayedTeleport;

    bool IsInFriendlyArea
    {
        get
        {
            var areaEntry = CliDB.AreaTableStorage.LookupByKey(Area);

            if (areaEntry != null)
                return IsFriendlyArea(areaEntry);

            return false;
        }
    }

    bool IsWarModeDesired => HasPlayerFlag(PlayerFlags.WarModeDesired);

    bool IsWarModeActive => HasPlayerFlag(PlayerFlags.WarModeActive);


    public bool IsResurrectRequested => _resurrectionData != null;

    public Unit SelectedUnit
    {
        get
        {
            var selectionGUID = Target;

            if (!selectionGUID.IsEmpty)
                return Global.ObjAccessor.GetUnit(this, selectionGUID);

            return null;
        }
    }

    public Player SelectedPlayer
    {
        get
        {
            var selectionGUID = Target;

            if (!selectionGUID.IsEmpty)
                return Global.ObjAccessor.GetPlayer(this, selectionGUID);

            return null;
        }
    }

    public static TeamFaction TeamForRace(Race race)
    {
        switch (TeamIdForRace(race))
        {
            case 0:
                return TeamFaction.Alliance;
            case 1:
                return TeamFaction.Horde;
        }

        return TeamFaction.Alliance;
    }


    public static uint TeamIdForRace(Race race)
    {
        var rEntry = CliDB.ChrRacesStorage.LookupByKey((byte)race);

        if (rEntry != null)
            return (uint)rEntry.Alliance;

        Log.outError(LogFilter.Player, "Race ({0}) not found in DBC: wrong DBC files?", race);

        return TeamIds.Neutral;
    }


    public Player(WorldSession session) : base(true)
    {
        ObjectTypeMask |= TypeMask.Player;
        ObjectTypeId = TypeId.Player;

        PlayerData = new PlayerData();
        ActivePlayerData = new ActivePlayerData();

        _session = session;

        // players always accept
        if (!Session.HasPermission(RBACPermissions.CanFilterWhispers))
            SetAcceptWhispers(true);

        _zoneUpdateId = 0xffffffff;
        _nextSave = WorldConfig.GetUIntValue(WorldCfg.IntervalSave);
        _customizationsChanged = false;

        GroupInvite = null;

        LoginFlags = AtLoginFlags.None;
        PlayerTalkClass = new PlayerMenu(session);
        _currentBuybackSlot = InventorySlots.BuyBackStart;

        for (byte i = 0; i < (int)MirrorTimerType.Max; i++)
            _mirrorTimer[i] = -1;

        _logintime = GameTime.GetGameTime();
        _lastTick = _logintime;

        _dungeonDifficulty = Difficulty.Normal;
        _raidDifficulty = Difficulty.NormalRaid;
        _legacyRaidDifficulty = Difficulty.Raid10N;
        InstanceValid = true;

        _specializationInfo = new SpecializationInfo();

        for (byte i = 0; i < (byte)BaseModGroup.End; ++i)
        {
            _auraBaseFlatMod[i] = 0.0f;
            _auraBasePctMod[i] = 1.0f;
        }

        for (var i = 0; i < (int)SpellModOp.Max; ++i)
        {
            _spellModifiers[i] = new List<SpellModifier>[(int)SpellModType.End];

            for (var c = 0; c < (int)SpellModType.End; ++c)
                _spellModifiers[i][c] = new List<SpellModifier>();
        }

        // Honor System
        _lastHonorUpdateTime = GameTime.GetGameTime();

        UnitMovedByMe = this;
        PlayerMovingMe = this;
        SeerView = this;

        m_isActive = true;
        ControlledByPlayer = true;

        Global.WorldMgr.IncreasePlayerCount();

        _cinematicMgr = new CinematicManager(this);

        _AchievementSys = new PlayerAchievementMgr(this);
        _reputationMgr = new ReputationMgr(this);
        _questObjectiveCriteriaManager = new QuestObjectiveCriteriaManager(this);
        _sceneMgr = new SceneMgr(this);

        _battlegroundQueueIdRecs[0] = new BgBattlegroundQueueIdRec();
        _battlegroundQueueIdRecs[1] = new BgBattlegroundQueueIdRec();

        _bgData = new BgData();

        _restMgr = new RestMgr(this);

        _groupUpdateTimer = new TimeTracker(5000);

        ApplyCustomConfigs();

        ObjectScale = 1;
    }

    public override void Dispose()
    {
        // Note: buy back item already deleted from DB when player was saved
        for (byte i = 0; i < (int)PlayerSlots.Count; ++i)
            if (_items[i] != null)
                _items[i].Dispose();

        _spells.Clear();
        _specializationInfo = null;
        _mail.Clear();

        foreach (var item in _mailItems.Values)
            item.Dispose();

        PlayerTalkClass.ClearMenus();
        ItemSetEff.Clear();

        _declinedname = null;
        _runes = null;
        _AchievementSys = null;
        _reputationMgr = null;

        _cinematicMgr.Dispose();

        for (byte i = 0; i < SharedConst.VoidStorageMaxSlot; ++i)
            _voidStorageItems[i] = null;

        ClearResurrectRequestData();

        Global.WorldMgr.DecreasePlayerCount();

        base.Dispose();
    }

}

// Holder for Battlegrounddata