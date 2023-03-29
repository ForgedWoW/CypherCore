// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Forged.RealmServer.Accounts;
using Forged.RealmServer.Cache;
using Forged.RealmServer.Chat;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.DungeonFinding;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Globals;
using Forged.RealmServer.Groups;
using Forged.RealmServer.Guilds;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.Scripting;
using Forged.RealmServer.Scripting.Interfaces.IWorld;
using Forged.RealmServer.SupportSystem;
using Framework.Constants;
using Framework.Database;
using Framework.Realm;
using Framework.Threading;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.RealmServer.World;

public class WorldManager
{
    private readonly IConfiguration _configuration;
    private readonly LoginDatabase _loginDatabase;
    private readonly ScriptManager _scriptManager;
    private readonly WorldDatabase _worldDatabase;
    private readonly CharacterDatabase _characterDatabase;
    private readonly SupportManager _supportManager;
    private readonly CliDB _cliDB;
    private AccountManager _accountManager;
    private CharacterCache _characterCache;
    private ObjectAccessor _objectAccessor;
    private QuestPoolManager _questPoolManager;
    private CalendarManager _calendarManager;
    private GuildManager _guildManager;
    private WorldStateManager _worldStateManager;
    private GameEventManager _eventManager;
    private WhoListStorageManager _whoListStorageManager;
    private ChannelManager _channelManager;
    private LFGManager _lFGManager;
    private GroupManager _groupManager;
    private GameEventManager _gameEventManager;
    public const string NEXT_CURRENCY_RESET_TIME_VAR_ID = "NextCurrencyResetTime";
    public const string NEXT_WEEKLY_QUEST_RESET_TIME_VAR_ID = "NextWeeklyQuestResetTime";
    public const string NEXT_BG_RANDOM_DAILY_RESET_TIME_VAR_ID = "NextBGRandomDailyResetTime";
    public const string CHARACTER_DATABASE_CLEANING_FLAGS_VAR_ID = "PersistentCharacterCleanFlags";
    public const string NEXT_GUILD_DAILY_RESET_TIME_VAR_ID = "NextGuildDailyResetTime";
    public const string NEXT_MONTHLY_QUEST_RESET_TIME_VAR_ID = "NextMonthlyQuestResetTime";
    public const string NEXT_DAILY_QUEST_RESET_TIME_VAR_ID = "NextDailyQuestResetTime";
    public const string NEXT_OLD_CALENDAR_EVENT_DELETION_TIME_VAR_ID = "NextOldCalendarEventDeletionTime";
    public const string NEXT_GUILD_WEEKLY_RESET_TIME_VAR_ID = "NextGuildWeeklyResetTime";
    public bool IsStopped;
    private readonly Dictionary<byte, Autobroadcast> _autobroadcasts = new();
    private readonly Dictionary<WorldTimers, IntervalTimer> _timers = new();
    private readonly ConcurrentDictionary<uint, WorldSession> _sessions = new();
    private readonly MultiMap<ObjectGuid, WorldSession> _sessionsByBnetGuid = new();
    private readonly Dictionary<uint, long> _disconnects = new();
    private readonly Dictionary<string, int> _worldVariables = new();
    private readonly List<string> _motd = new();
    private readonly List<WorldSession> _queuedPlayer = new();
    private readonly ConcurrentQueue<WorldSession> _addSessQueue = new();
    private readonly ConcurrentQueue<Tuple<WorldSocket, ulong>> _linkSocketQueue = new();
    private readonly AsyncCallbackProcessor<QueryCallback> _queryProcessor = new();
    private readonly Realm _realm;
    private readonly GameTime _gameTime;
    private readonly WorldUpdateTime _worldUpdateTime;
    private readonly object _guidAlertLock = new();
    private readonly LimitedThreadTaskManager _taskManager = new(10);

    private uint _shutdownTimer;
    private ShutdownMask _shutdownMask;
    private ShutdownExitCode _exitCode;

    private CleaningFlags _cleaningFlags;

    private float _maxVisibleDistanceOnContinents = SharedConst.DefaultVisibilityDistance;
    private float _maxVisibleDistanceInInstances = SharedConst.DefaultVisibilityInstance;
    private float _maxVisibleDistanceInBg = SharedConst.DefaultVisibilityBGAreans;
    private float _maxVisibleDistanceInArenas = SharedConst.DefaultVisibilityBGAreans;

    private int _visibilityNotifyPeriodOnContinents = SharedConst.DefaultVisibilityNotifyPeriod;
    private int _visibilityNotifyPeriodInInstances = SharedConst.DefaultVisibilityNotifyPeriod;
    private int _visibilityNotifyPeriodInBg = SharedConst.DefaultVisibilityNotifyPeriod;
    private int _visibilityNotifyPeriodInArenas = SharedConst.DefaultVisibilityNotifyPeriod;

    private bool _isClosed;
    private long _mailTimer;
    private long _timerExpires;
    private long _blackmarketTimer;
    private uint _maxActiveSessionCount;
    private uint _maxQueuedSessionCount;
    private uint _playerCount;
    private uint _maxPlayerCount;
    private uint _playerLimit;
    private AccountTypes _allowedSecurityLevel;
    private Locale _defaultDbcLocale;       // from config for one from loaded DBC locales
    private BitSet _availableDbcLocaleMask; // by loaded DBC

    // scheduled reset times
    private long _nextDailyQuestReset;
    private long _nextWeeklyQuestReset;
    private long _nextMonthlyQuestReset;
    private long _nextRandomBgReset;
    private long _nextCalendarOldEventsDeletionTime;
    private long _nextGuildReset;
    private long _nextCurrencyReset;

    private string _dataPath;

    private string _guidWarningMsg;
    private string _alertRestartReason;

    private bool _guidWarn;
    private bool _guidAlert;
    private uint _warnDiff;
    private long _warnShutdownTime;

    private uint _maxSkill;

    public bool IsClosed => _isClosed;

    public List<string> Motd => _motd;

    public List<WorldSession> AllSessions => _sessions.Values.ToList();

    public int ActiveAndQueuedSessionCount => _sessions.Count;

    public int ActiveSessionCount => _sessions.Count - _queuedPlayer.Count;

    public int QueuedSessionCount => _queuedPlayer.Count;

    // Get the maximum number of parallel sessions on the server since last reboot
    public uint MaxQueuedSessionCount => _maxQueuedSessionCount;

    public uint MaxActiveSessionCount => _maxActiveSessionCount;

    public uint PlayerCount => _playerCount;

    public uint MaxPlayerCount => _maxPlayerCount;

    public AccountTypes PlayerSecurityLimit
    {
        get => _allowedSecurityLevel;
        set
        {
            var sec = value < AccountTypes.Console ? value : AccountTypes.Player;
            var update = sec > _allowedSecurityLevel;
            _allowedSecurityLevel = sec;

            if (update)
                KickAllLess(_allowedSecurityLevel);
        }
    }

    public uint PlayerAmountLimit
    {
        get => _playerLimit;
        set => _playerLimit = value;
    }

    /// Get the path where data (dbc, maps) are stored on disk
    public string DataPath
    {
        get => _dataPath;
        set => _dataPath = value;
    }

    public long NextDailyQuestsResetTime
    {
        get => _nextDailyQuestReset;
        set => _nextDailyQuestReset = value;
    }

    public long NextWeeklyQuestsResetTime
    {
        get => _nextWeeklyQuestReset;
        set => _nextWeeklyQuestReset = value;
    }

    public long NextMonthlyQuestsResetTime
    {
        get => _nextMonthlyQuestReset;
        set => _nextMonthlyQuestReset = value;
    }

    public uint ConfigMaxSkillValue
    {
        get
        {
            if (_maxSkill == 0)
            {
                var lvl = _configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);

                _maxSkill = (uint)(lvl > 60 ? 300 + ((lvl - 60) * 75) / 10 : lvl * 5);
            }

            return _maxSkill;
        }
    }

    public bool IsShuttingDown => _shutdownTimer > 0;

    public uint ShutDownTimeLeft => _shutdownTimer;

    public int ExitCode => (int)_exitCode;

    public bool IsPvPRealm
    {
        get
        {
            var realmtype = (RealmType)_configuration.GetDefaultValue("GameType", 0);

            return realmtype is RealmType.PVP or RealmType.RPPVP or RealmType.FFAPVP;
        }
    }

    public bool IsFFAPvPRealm => _configuration.GetDefaultValue("GameType", 0) == (int)RealmType.FFAPVP;

    public Locale DefaultDbcLocale => _defaultDbcLocale;

    public Realm Realm => _realm;

    public RealmId RealmId => _realm.Id;

    public uint VirtualRealmAddress => _realm.Id.GetAddress();

    public float MaxVisibleDistanceOnContinents => _maxVisibleDistanceOnContinents;

    public float MaxVisibleDistanceInInstances => _maxVisibleDistanceInInstances;

    public float MaxVisibleDistanceInBG => _maxVisibleDistanceInBg;

    public float MaxVisibleDistanceInArenas => _maxVisibleDistanceInArenas;

    public int VisibilityNotifyPeriodOnContinents => _visibilityNotifyPeriodOnContinents;

    public int VisibilityNotifyPeriodInInstances => _visibilityNotifyPeriodInInstances;

    public int VisibilityNotifyPeriodInBG => _visibilityNotifyPeriodInBg;

    public int VisibilityNotifyPeriodInArenas => _visibilityNotifyPeriodInArenas;

    public CleaningFlags CleaningFlags
    {
        get => _cleaningFlags;
        set => _cleaningFlags = value;
    }

    public bool IsGuidWarning => _guidWarn;

    public bool IsGuidAlert => _guidAlert;

    public WorldUpdateTime WorldUpdateTime => _worldUpdateTime;

    public WorldManager(IConfiguration configuration, LoginDatabase loginDatabase, ScriptManager scriptManager,
                        WorldDatabase worldDatabase, CharacterDatabase characterDatabase, SupportManager supportManager,
                        CliDB cliDB, Realm realm, GameTime gameTime)
    {
        _configuration = configuration;
        _loginDatabase = loginDatabase;
        _scriptManager = scriptManager;
        _worldDatabase = worldDatabase;
        _characterDatabase = characterDatabase;
        _supportManager = supportManager;
        _cliDB = cliDB;
        _realm = realm;
        _gameTime = gameTime;

        foreach (WorldTimers timer in Enum.GetValues(typeof(WorldTimers)))
            _timers[timer] = new IntervalTimer();

        _allowedSecurityLevel = AccountTypes.Player;

        _worldUpdateTime = new WorldUpdateTime(_configuration);
        _warnShutdownTime = _gameTime.CurrentGameTime;

        LoadRealmInfo();

        LoadConfigSettings();

        // Initialize Allowed Security Level
        LoadDBAllowedSecurityLevel();
    }

    public void Initialize(AccountManager accountManager, CharacterCache characterCache, ObjectAccessor objectAccessor,
                           QuestPoolManager questPoolManager, CalendarManager calendarManager, GuildManager guildManager,
                           WorldStateManager worldStateManager, GameEventManager eventManager, WhoListStorageManager whoListStorageManager,
                           ChannelManager channelManager, LFGManager lFGManager, GroupManager groupManager, GameEventManager gameEventManager)
    {
        _accountManager = accountManager;
        _characterCache = characterCache;
        _objectAccessor = objectAccessor;
        _questPoolManager = questPoolManager;
        _calendarManager = calendarManager;
        _guildManager = guildManager;
        _worldStateManager = worldStateManager;
        _eventManager = eventManager;
        _whoListStorageManager = whoListStorageManager;
        _channelManager = channelManager;
        _lFGManager = lFGManager;
        _groupManager = groupManager;
        _gameEventManager = gameEventManager;

        // not send custom type REALM_FFA_PVP to realm list
        var serverType = IsFFAPvPRealm ? RealmType.PVP : (RealmType)_configuration.GetDefaultValue("GameType", 0);
        var realmZone = _configuration.GetDefaultValue("RealmZone", (int)RealmZones.Development);

        _loginDatabase.Execute("UPDATE realmlist SET icon = {0}, timezone = {1} WHERE id = '{2}'", (byte)serverType, realmZone, _realm.Id.Index); // One-time query

        Log.Logger.Information("Loading GameObject models...");

        LoadPersistentWorldVariables();
        LoadAutobroadcasts();
        _gameTime.UpdateGameTimers(); // TODO get from Realm


        _loginDatabase.Execute("INSERT INTO uptime (realmid, starttime, uptime, revision) VALUES({0}, {1}, 0, '{2}')", _realm.Id.Index, _gameTime.StartTime, ""); // One-time query

        _timers[WorldTimers.Auctions].Interval = Time.Minute * Time.InMilliseconds;
        _timers[WorldTimers.AuctionsPending].Interval = 250;

        //Update "uptime" table based on configuration entry in minutes.
        _timers[WorldTimers.UpTime]
            .
            //Update "uptime" table based on configuration entry in minutes.
            Interval = 10 * Time.Minute * Time.InMilliseconds;

        //erase corpses every 20 minutes
        _timers[WorldTimers.Corpses]
            . //erase corpses every 20 minutes
            Interval = 20 * Time.Minute * Time.InMilliseconds;

        _timers[WorldTimers.CleanDB].Interval = _configuration.GetDefaultValue("LogDB.Opt.ClearInterval", 10) * Time.Minute * Time.InMilliseconds;
        _timers[WorldTimers.AutoBroadcast].Interval = _configuration.GetDefaultValue("AutoBroadcast.Timer", 60000);

        // check for chars to delete every day
        _timers[WorldTimers.DeleteChars]
            . // check for chars to delete every day
            Interval = Time.Day * Time.InMilliseconds;

        // for AhBot
        _timers[WorldTimers.AhBot]
            .                                                                                                       // for AhBot
            Interval = _configuration.GetDefaultValue("AuctionHouseBot.Update.Interval", 20) * Time.InMilliseconds; // every 20 sec

        _timers[WorldTimers.GuildSave].Interval = _configuration.GetDefaultValue("Guild.SaveInterval", 15) * Time.Minute * Time.InMilliseconds;

        _timers[WorldTimers.Blackmarket].Interval = 10 * Time.InMilliseconds;

        _blackmarketTimer = 0;

        _timers[WorldTimers.WhoList].Interval = 5 * Time.InMilliseconds; // update who list cache every 5 seconds

        _timers[WorldTimers.ChannelSave].Interval = _configuration.GetDefaultValue("PreserveCustomChannelInterval", 5) * Time.Minute * Time.InMilliseconds;

        //to set mailtimer to return mails every day between 4 and 5 am
        //mailtimer is increased when updating auctions
        //one second is 1000 -(tested on win system)
        // @todo Get rid of magic numbers
        var localTime = Time.UnixTimeToDateTime(_gameTime.CurrentGameTime).ToLocalTime();
        var cleanOldMailsTime = _configuration.GetDefaultValue("CleanOldMailTime", 4u);
        _mailTimer = ((((localTime.Hour + (24 - cleanOldMailsTime)) % 24) * Time.Hour * Time.InMilliseconds) / _timers[WorldTimers.Auctions].Interval);
        //1440
        _timerExpires = ((Time.Day * Time.InMilliseconds) / (_timers[(int)WorldTimers.Auctions].Interval));
        Log.Logger.Information("Mail timer set to: {0}, mail return is called every {1} minutes", _mailTimer, _timerExpires);

        _loginDatabase.Execute("DELETE FROM ip_banned WHERE unbandate <= UNIX_TIMESTAMP() AND unbandate<>bandate"); // One-time query

        InitQuestResetTimes();
        CheckScheduledResetTimes();
        InitRandomBGResetTime();
        InitCalendarOldEventsDeletionTime();
        InitGuildResetTime();
        InitCurrencyResetTime();
    }

    public Player FindPlayerInZone(uint zone)
    {
        foreach (var session in _sessions)
        {
            var player = session.Value.Player;

            if (player == null)
                continue;

            if (player.IsInWorld && player.Zone == zone)
                // Used by the weather system. We return the player to broadcast the change weather message to him and all players in the zone.
                return player;
        }

        return null;
    }

    public void SetClosed(bool val)
    {
        _isClosed = val;
        _scriptManager.ForEach<IWorldOnOpenStateChange>(p => p.OnOpenStateChange(!val));
    }

    public void LoadDBAllowedSecurityLevel()
    {
        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_REALMLIST_SECURITY_LEVEL);
        stmt.AddValue(0, (int)_realm.Id.Index);
        var result = _loginDatabase.Query(stmt);

        if (!result.IsEmpty())
            PlayerSecurityLimit = (AccountTypes)result.Read<byte>(0);
    }

    public void SetMotd(string motd)
    {
        _scriptManager.ForEach<IWorldOnMotdChange>(p => p.OnMotdChange(motd));

        _motd.Clear();
        _motd.AddRange(motd.Split('@'));
    }
    
    public void TriggerGuidWarning()
    {
        // Lock this only to prevent multiple maps triggering at the same time
        lock (_guidAlertLock)
        {
            var gameTime = _gameTime.CurrentGameTime;
            var today = (gameTime / Time.Day) * Time.Day;

            // Check if our window to restart today has passed. 5 mins until quiet time
            while (gameTime >= Time.GetLocalHourTimestamp(today, _configuration.GetDefaultValue("Respawn.RestartQuietTime", 3u)) - 1810u)
                today += Time.Day;

            // Schedule restart for 30 minutes before quiet time, or as long as we have
            _warnShutdownTime = Time.GetLocalHourTimestamp(today, _configuration.GetDefaultValue("Respawn.RestartQuietTime", 3u)) - 1800u;

            _guidWarn = true;
            SendGuidWarning();
        }
    }

    public void TriggerGuidAlert()
    {
        // Lock this only to prevent multiple maps triggering at the same time
        lock (_guidAlertLock)
        {
            DoGuidAlertRestart();
            _guidAlert = true;
            _guidWarn = false;
        }
    }

    public WorldSession FindSession(uint id)
    {
        return _sessions.LookupByKey(id);
    }

    public void AddSession(WorldSession s)
    {
        _addSessQueue.Enqueue(s);
    }

    public void AddInstanceSocket(WorldSocket sock, ulong connectToKey)
    {
        _linkSocketQueue.Enqueue(Tuple.Create(sock, connectToKey));
    }

    public void SetEventInterval(long nextGameEvent)
    {
        _timers[WorldTimers.Events].Interval = nextGameEvent; //depend on next event
    }

    public void SetDBCMask(BitSet mask)
    {
        _availableDbcLocaleMask = mask;

        if (_availableDbcLocaleMask == null || !_availableDbcLocaleMask[(int)_defaultDbcLocale])
        {
            Log.Logger.Fatal($"Unable to load db2 files for {_defaultDbcLocale} locale specified in DBC.Locale config!");
            Environment.Exit(1);
        }
    }

    public void LoadConfigSettings(bool reload = false)
    {
        _defaultDbcLocale = (Locale)_configuration.GetDefaultValue("DBC.Locale", 0);

        if (_defaultDbcLocale >= Locale.Total || _defaultDbcLocale == Locale.None)
        {
            Log.Logger.Error("Incorrect DBC.Locale! Must be >= 0 and < {0} and not {1} (set to 0)", Locale.Total, Locale.None);
            _defaultDbcLocale = Locale.enUS;
        }

        Log.Logger.Information("Using {0} DBC Locale", _defaultDbcLocale);

        // load update time related configs
        _worldUpdateTime.LoadFromConfig();

        PlayerAmountLimit = (uint)_configuration.GetDefaultValue("PlayerLimit", 100);
        SetMotd(_configuration.GetDefaultValue("Motd", "Welcome to a Forged Core Server."));

        if (reload)
        {
            _supportManager.SetSupportSystemStatus(_configuration.GetDefaultValue("Support.Enabled", true));
            _supportManager.SetTicketSystemStatus(_configuration.GetDefaultValue("Support.TicketsEnabled", false));
            _supportManager.SetBugSystemStatus(_configuration.GetDefaultValue("Support.BugsEnabled", false));
            _supportManager.SetComplaintSystemStatus(_configuration.GetDefaultValue("Support.ComplaintsEnabled", false));
            _supportManager.SetSuggestionSystemStatus(_configuration.GetDefaultValue("Support.SuggestionsEnabled", false));

            _timers[WorldTimers.UpTime].Interval = _configuration.GetDefaultValue("UpdateUptimeInterval", 10) * Time.Minute * Time.InMilliseconds;
            _timers[WorldTimers.UpTime].Reset();

            _timers[WorldTimers.CleanDB].Interval = _configuration.GetDefaultValue("LogDB.Opt.ClearInterval", 10) * Time.Minute * Time.InMilliseconds;
            _timers[WorldTimers.CleanDB].Reset();


            _timers[WorldTimers.AutoBroadcast].Interval = _configuration.GetDefaultValue("AutoBroadcast.Timer", 60000);
            _timers[WorldTimers.AutoBroadcast].Reset();
        }

        for (byte i = 0; i < (int)UnitMoveType.Max; ++i)
            SharedConst.playerBaseMoveSpeed[i] = SharedConst.baseMoveSpeed[i] * _configuration.GetDefaultValue("Rate.MoveSpeed", 1.0f);

        var rateCreatureAggro = _configuration.GetDefaultValue("Rate.Creature.Aggro", 1.0f);
        //visibility on continents
        _maxVisibleDistanceOnContinents = _configuration.GetDefaultValue("Visibility.Distance.Continents", SharedConst.DefaultVisibilityDistance);

        if (_maxVisibleDistanceOnContinents < 45 * rateCreatureAggro)
        {
            Log.Logger.Error("Visibility.Distance.Continents can't be less max aggro radius {0}", 45 * rateCreatureAggro);
            _maxVisibleDistanceOnContinents = 45 * rateCreatureAggro;
        }
        else if (_maxVisibleDistanceOnContinents > SharedConst.MaxVisibilityDistance)
        {
            Log.Logger.Error("Visibility.Distance.Continents can't be greater {0}", SharedConst.MaxVisibilityDistance);
            _maxVisibleDistanceOnContinents = SharedConst.MaxVisibilityDistance;
        }

        //visibility in instances
        _maxVisibleDistanceInInstances = _configuration.GetDefaultValue("Visibility.Distance.Instances", SharedConst.DefaultVisibilityInstance);

        if (_maxVisibleDistanceInInstances < 45 * rateCreatureAggro)
        {
            Log.Logger.Error("Visibility.Distance.Instances can't be less max aggro radius {0}", 45 * rateCreatureAggro);
            _maxVisibleDistanceInInstances = 45 * rateCreatureAggro;
        }
        else if (_maxVisibleDistanceInInstances > SharedConst.MaxVisibilityDistance)
        {
            Log.Logger.Error("Visibility.Distance.Instances can't be greater {0}", SharedConst.MaxVisibilityDistance);
            _maxVisibleDistanceInInstances = SharedConst.MaxVisibilityDistance;
        }

        //visibility in BG
        _maxVisibleDistanceInBg = _configuration.GetDefaultValue("Visibility.Distance.BG", SharedConst.DefaultVisibilityBGAreans);

        if (_maxVisibleDistanceInBg < 45 * rateCreatureAggro)
        {
            Log.Logger.Error($"Visibility.Distance.BG can't be less max aggro radius {45 * rateCreatureAggro}");
            _maxVisibleDistanceInBg = 45 * rateCreatureAggro;
        }
        else if (_maxVisibleDistanceInBg > SharedConst.MaxVisibilityDistance)
        {
            Log.Logger.Error($"Visibility.Distance.BG can't be greater {SharedConst.MaxVisibilityDistance}");
            _maxVisibleDistanceInBg = SharedConst.MaxVisibilityDistance;
        }

        // Visibility in Arenas
        _maxVisibleDistanceInArenas = _configuration.GetDefaultValue("Visibility.Distance.Arenas", SharedConst.DefaultVisibilityBGAreans);

        if (_maxVisibleDistanceInArenas < 45 * rateCreatureAggro)
        {
            Log.Logger.Error($"Visibility.Distance.Arenas can't be less max aggro radius {45 * rateCreatureAggro}");
            _maxVisibleDistanceInArenas = 45 * rateCreatureAggro;
        }
        else if (_maxVisibleDistanceInArenas > SharedConst.MaxVisibilityDistance)
        {
            Log.Logger.Error($"Visibility.Distance.Arenas can't be greater {SharedConst.MaxVisibilityDistance}");
            _maxVisibleDistanceInArenas = SharedConst.MaxVisibilityDistance;
        }

        _visibilityNotifyPeriodOnContinents = _configuration.GetDefaultValue("Visibility.Notify.Period.OnContinents", SharedConst.DefaultVisibilityNotifyPeriod);
        _visibilityNotifyPeriodInInstances = _configuration.GetDefaultValue("Visibility.Notify.Period.InInstances", SharedConst.DefaultVisibilityNotifyPeriod);
        _visibilityNotifyPeriodInBg = _configuration.GetDefaultValue("Visibility.Notify.Period.InBG", SharedConst.DefaultVisibilityNotifyPeriod);
        _visibilityNotifyPeriodInArenas = _configuration.GetDefaultValue("Visibility.Notify.Period.InArenas", SharedConst.DefaultVisibilityNotifyPeriod);

        _guidWarningMsg = _configuration.GetDefaultValue("Respawn.WarningMessage", "There will be an unscheduled server restart at 03:00. The server will be available again shortly after.");
        _alertRestartReason = _configuration.GetDefaultValue("Respawn.AlertRestartReason", "Urgent Maintenance");

        var dataPath = _configuration.GetDefaultValue("DataDir", "./");

        if (reload)
        {
            if (dataPath != _dataPath)
                Log.Logger.Error("DataDir option can't be changed at worldserver.conf reload, using current value ({0}).", _dataPath);
        }
        else
        {
            _dataPath = dataPath;
            Log.Logger.Information("Using DataDir {0}", _dataPath);
        }

        Log.Logger.Information(@"WORLD: MMap data directory is: {0}\mmaps", _dataPath);
    }
    
    public void DisableForcedWarModeFactionBalanceState()
    {
        UpdateWarModeRewardValues();
    }

    public void LoadAutobroadcasts()
    {
        var oldMSTime = Time.MSTime;

        _autobroadcasts.Clear();

        var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_AUTOBROADCAST);
        stmt.AddValue(0, _realm.Id.Index);

        var result = _loginDatabase.Query(stmt);

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 autobroadcasts definitions. DB table `autobroadcast` is empty for this realm!");

            return;
        }

        do
        {
            var id = result.Read<byte>(0);

            _autobroadcasts[id] = new Autobroadcast(result.Read<string>(2), result.Read<byte>(1));
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} autobroadcast definitions in {1} ms", _autobroadcasts.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }
    
    public void Update(uint diff)
    {
        //- Update the game time and check for shutdown time
        UpdateGameTime();
        var currentGameTime = _gameTime.CurrentGameTime;

        _worldUpdateTime.UpdateWithDiff(diff);

        // Record update if recording set in log and diff is greater then minimum set in log
        _worldUpdateTime.RecordUpdateTime(_gameTime.GameTimeMS, diff, (uint)ActiveSessionCount);
        Realm.PopulationLevel = ActiveSessionCount;

        // Update the different timers
        for (WorldTimers i = 0; i < WorldTimers.Max; ++i)
            if (_timers[i].Current >= 0)
                _timers[i].Update(diff);
            else
                _timers[i].Current = 0;

        // Update Who List Storage
        if (_timers[WorldTimers.WhoList].Passed)
        {
            _timers[WorldTimers.WhoList].Reset();
            _taskManager.Schedule(_whoListStorageManager.Update);
        }

        if (IsStopped || _timers[WorldTimers.ChannelSave].Passed)
        {
            _timers[WorldTimers.ChannelSave].Reset();

            if (_configuration.GetDefaultValue("PreserveCustomChannels", false))
                _taskManager.Schedule(() =>
                {
                    var mgr1 = _channelManager.ForTeam(TeamFaction.Alliance);
                    mgr1.SaveToDB();
                    var mgr2 = _channelManager.ForTeam(TeamFaction.Horde);

                    if (mgr1 != mgr2)
                        mgr2.SaveToDB();
                });
        }

        CheckScheduledResetTimes();

        if (currentGameTime > _nextRandomBgReset)
            _taskManager.Schedule(ResetRandomBG);

        if (currentGameTime > _nextCalendarOldEventsDeletionTime)
            _taskManager.Schedule(CalendarDeleteOldEvents);

        if (currentGameTime > _nextGuildReset)
            _taskManager.Schedule(ResetGuildCap);

        if (currentGameTime > _nextCurrencyReset)
            _taskManager.Schedule(ResetCurrencyWeekCap);

        //Handle session updates when the timer has passed
        _worldUpdateTime.RecordUpdateTimeReset();
        UpdateSessions(diff);
        _worldUpdateTime.RecordUpdateTimeDuration("UpdateSessions");

        // <li> Update uptime table
        if (_timers[WorldTimers.UpTime].Passed)
        {
            var tmpDiff = _gameTime.GetUptime;
            var maxOnlinePlayers = MaxPlayerCount;

            _timers[WorldTimers.UpTime].Reset();

            _taskManager.Schedule(() =>
            {
                var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_UPTIME_PLAYERS);

                stmt.AddValue(0, tmpDiff);
                stmt.AddValue(1, maxOnlinePlayers);
                stmt.AddValue(2, _realm.Id.Index);
                stmt.AddValue(3, (uint)_gameTime.StartTime);

                _loginDatabase.Execute(stmt);
            });
        }

        // <li> Clean logs table
        if (_configuration.GetDefaultValue("LogDB.Opt.ClearTime", 1209600) > 0) // if not enabled, ignore the timer
            if (_timers[WorldTimers.CleanDB].Passed)
            {
                _timers[WorldTimers.CleanDB].Reset();

                _taskManager.Schedule(() =>
                {
                    var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_OLD_LOGS);
                    stmt.AddValue(0, _configuration.GetDefaultValue("LogDB.Opt.ClearTime", 1209600));
                    stmt.AddValue(1, 0);
                    stmt.AddValue(2, Realm.Id.Index);

                    _loginDatabase.Execute(stmt);
                });
            }

        _taskManager.Wait();
        _worldUpdateTime.RecordUpdateTimeReset();
        _worldUpdateTime.RecordUpdateTimeDuration("UpdateMapMgr");

        if (_configuration.GetDefaultValue("AutoBroadcast.On", false))
            if (_timers[WorldTimers.AutoBroadcast].Passed)
            {
                _timers[WorldTimers.AutoBroadcast].Reset();
                _taskManager.Schedule(SendAutoBroadcast);
            }

        //- Delete all characters which have been deleted X days before
        if (_timers[WorldTimers.DeleteChars].Passed)
        {
            _timers[WorldTimers.DeleteChars].Reset();
            _taskManager.Schedule(Player.DeleteOldCharacters);
        }

        _taskManager.Schedule(() => _lFGManager.Update(diff));
        _worldUpdateTime.RecordUpdateTimeDuration("UpdateLFGMgr");

        _taskManager.Schedule(() => _groupManager.Update(diff));
        _worldUpdateTime.RecordUpdateTimeDuration("GroupMgr");

        // execute callbacks from sql queries that were queued recently
        _taskManager.Schedule(ProcessQueryCallbacks);
        _worldUpdateTime.RecordUpdateTimeDuration("ProcessQueryCallbacks");

        // Process Game events when necessary
        if (_timers[WorldTimers.Events].Passed)
        {
            _timers[WorldTimers.Events].Reset(); // to give time for Update() to be processed
            var nextGameEvent = _gameEventManager.Update();
            _timers[WorldTimers.Events].Interval = nextGameEvent;
            _timers[WorldTimers.Events].Reset();
        }

        if (_timers[WorldTimers.GuildSave].Passed)
        {
            _timers[WorldTimers.GuildSave].Reset();
            _taskManager.Schedule(_guildManager.SaveGuilds);
        }

        // Check for shutdown warning
        if (_guidWarn && !_guidAlert)
        {
            _warnDiff += diff;

            if (_gameTime.CurrentGameTime >= _warnShutdownTime)
                DoGuidWarningRestart();
            else if (_warnDiff > _configuration.GetDefaultValue("Respawn.WarningFrequency", 1800) * Time.InMilliseconds)
                SendGuidWarning();
        }

        _scriptManager.ForEach<IWorldOnUpdate>(p => p.OnUpdate(diff));
        _taskManager.Wait(); // wait for all blocks to complete.
    }

    public void ForceGameEventUpdate()
    {
        _timers[WorldTimers.Events].Reset(); // to give time for Update() to be processed
        var nextGameEvent = _eventManager.Update();
        _timers[WorldTimers.Events].Interval = nextGameEvent;
        _timers[WorldTimers.Events].Reset();
    }

    public void SendGlobalMessage(ServerPacket packet, WorldSession self = null, TeamFaction team = 0)
    {
        foreach (var session in _sessions.Values)
            if (session.Player != null &&
                session.Player.IsInWorld &&
                session != self &&
                (team == 0 || session.Player.Team == team))
                session.SendPacket(packet);
    }

    public void SendGlobalGMMessage(ServerPacket packet, WorldSession self = null, TeamFaction team = 0)
    {
        foreach (var session in _sessions.Values)
        {
            // check if session and can receive global GM Messages and its not self
            if (session == null || session == self || !session.HasPermission(RBACPermissions.ReceiveGlobalGmTextmessage))
                continue;

            // Player should be in world
            var player = session.Player;

            if (player == null || !player.IsInWorld)
                continue;

            // Send only to same team, if team is given
            if (team == 0 || player.Team == team)
                session.SendPacket(packet);
        }
    }

    // Send a System Message to all players (except self if mentioned)
    public void SendWorldText(CypherStrings stringID, params object[] args)
    {
        // Send to map server
    }

    // Send a System Message to all GMs (except self if mentioned)
    public void SendGMText(CypherStrings stringID, params object[] args)
    {
        // Send to map server
    }

    // Send a packet to all players (or players selected team) in the zone (except self if mentioned)
    public bool SendZoneMessage(uint zone, ServerPacket packet, WorldSession self = null, uint team = 0)
    {
        var foundPlayerToSend = false;

        foreach (var session in _sessions.Values)
            if (session != null &&
                session.Player &&
                session.Player.IsInWorld &&
                session.Player.Zone == zone &&
                session != self &&
                (team == 0 || (uint)session.Player.Team == team))
            {
                session.SendPacket(packet);
                foundPlayerToSend = true;
            }

        return foundPlayerToSend;
    }

    // Send a System Message to all players in the zone (except self if mentioned)
    public void SendZoneText(uint zone, string text, WorldSession self = null, uint team = 0)
    {
        ChatPkt data = new();
        data.Initialize(ChatMsg.System, Language.Universal, null, null, text);
        SendZoneMessage(zone, data, self, team);
    }

    public void KickAll()
    {
        _queuedPlayer.Clear(); // prevent send queue update packet and login queued sessions

        // session not removed at kick and will removed in next update tick
        foreach (var session in _sessions.Values)
            session.KickPlayer("World::KickAll");
    }

    /// Ban an account or ban an IP address, duration will be parsed using TimeStringToSecs if it is positive, otherwise permban
    public BanReturn BanAccount(BanMode mode, string nameOrIP, string duration, string reason, string author)
    {
        var durationSecs = Time.TimeStringToSecs(duration);

        return BanAccount(mode, nameOrIP, durationSecs, reason, author);
    }

    /// Ban an account or ban an IP address, duration is in seconds if positive, otherwise permban
    public BanReturn BanAccount(BanMode mode, string nameOrIP, uint durationSecs, string reason, string author)
    {
        // Prevent banning an already banned account
        if (mode == BanMode.Account && _accountManager.IsBannedAccount(nameOrIP))
            return BanReturn.Exists;

        SQLResult resultAccounts;
        PreparedStatement stmt;

        // Update the database with ban information
        switch (mode)
        {
            case BanMode.IP:
                // No SQL injection with prepared statements
                stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BY_IP);
                stmt.AddValue(0, nameOrIP);
                resultAccounts = _loginDatabase.Query(stmt);
                stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_IP_BANNED);
                stmt.AddValue(0, nameOrIP);
                stmt.AddValue(1, durationSecs);
                stmt.AddValue(2, author);
                stmt.AddValue(3, reason);
                _loginDatabase.Execute(stmt);

                break;
            case BanMode.Account:
                // No SQL injection with prepared statements
                stmt = _loginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_ID_BY_NAME);
                stmt.AddValue(0, nameOrIP);
                resultAccounts = _loginDatabase.Query(stmt);

                break;
            case BanMode.Character:
                // No SQL injection with prepared statements
                stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_ACCOUNT_BY_NAME);
                stmt.AddValue(0, nameOrIP);
                resultAccounts = _characterDatabase.Query(stmt);

                break;
            default:
                return BanReturn.SyntaxError;
        }

        if (resultAccounts == null)
        {
            if (mode == BanMode.IP)
                return BanReturn.Success; // ip correctly banned but nobody affected (yet)
            else
                return BanReturn.Notfound; // Nobody to ban
        }

        // Disconnect all affected players (for IP it can be several)
        SQLTransaction trans = new();

        do
        {
            var account = resultAccounts.Read<uint>(0);

            if (mode != BanMode.IP)
            {
                // make sure there is only one active ban
                stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_NOT_BANNED);
                stmt.AddValue(0, account);
                trans.Append(stmt);
                // No SQL injection with prepared statements
                stmt = _loginDatabase.GetPreparedStatement(LoginStatements.INS_ACCOUNT_BANNED);
                stmt.AddValue(0, account);
                stmt.AddValue(1, durationSecs);
                stmt.AddValue(2, author);
                stmt.AddValue(3, reason);
                trans.Append(stmt);
            }

            var sess = FindSession(account);

            if (sess)
                if (sess.PlayerName != author)
                    sess.KickPlayer("World::BanAccount Banning account");
        } while (resultAccounts.NextRow());

        _loginDatabase.CommitTransaction(trans);

        return BanReturn.Success;
    }

    /// Remove a ban from an account or IP address
    public bool RemoveBanAccount(BanMode mode, string nameOrIP)
    {
        PreparedStatement stmt;

        if (mode == BanMode.IP)
        {
            stmt = _loginDatabase.GetPreparedStatement(LoginStatements.DEL_IP_NOT_BANNED);
            stmt.AddValue(0, nameOrIP);
            _loginDatabase.Execute(stmt);
        }
        else
        {
            uint account = 0;

            if (mode == BanMode.Account)
                account = _accountManager.GetId(nameOrIP);
            else if (mode == BanMode.Character)
                account = _characterCache.GetCharacterAccountIdByName(nameOrIP);

            if (account == 0)
                return false;

            //NO SQL injection as account is uint32
            stmt = _loginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_NOT_BANNED);
            stmt.AddValue(0, account);
            _loginDatabase.Execute(stmt);
        }

        return true;
    }

    /// Ban an account or ban an IP address, duration will be parsed using TimeStringToSecs if it is positive, otherwise permban
    public BanReturn BanCharacter(string name, string duration, string reason, string author)
    {
        var durationSecs = Time.TimeStringToSecs(duration);

        return BanAccount(BanMode.Character, name, durationSecs, reason, author);
    }

    public BanReturn BanCharacter(string name, uint durationSecs, string reason, string author)
    {
        var pBanned = _objectAccessor.FindConnectedPlayerByName(name);
        ObjectGuid guid;

        // Pick a player to ban if not online
        if (!pBanned)
        {
            guid = _characterCache.GetCharacterGuidByName(name);

            if (guid.IsEmpty)
                return BanReturn.Notfound; // Nobody to ban
        }
        else
        {
            guid = pBanned.GUID;
        }

        //Use transaction in order to ensure the order of the queries
        SQLTransaction trans = new();

        // make sure there is only one active ban
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER_BAN);
        stmt.AddValue(0, guid.Counter);
        trans.Append(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CHARACTER_BAN);
        stmt.AddValue(0, guid.Counter);
        stmt.AddValue(1, (long)durationSecs);
        stmt.AddValue(2, author);
        stmt.AddValue(3, reason);
        trans.Append(stmt);
        _characterDatabase.CommitTransaction(trans);

        if (pBanned)
            pBanned.Session.KickPlayer("World::BanCharacter Banning character");

        return BanReturn.Success;
    }

    // Remove a ban from a character
    public bool RemoveBanCharacter(string name)
    {
        var pBanned = _objectAccessor.FindConnectedPlayerByName(name);
        ObjectGuid guid;

        // Pick a player to ban if not online
        if (!pBanned)
        {
            guid = _characterCache.GetCharacterGuidByName(name);

            if (guid.IsEmpty)
                return false; // Nobody to ban
        }
        else
        {
            guid = pBanned.GUID;
        }

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER_BAN);
        stmt.AddValue(0, guid.Counter);
        _characterDatabase.Execute(stmt);

        return true;
    }

    public void ShutdownServ(uint time, ShutdownMask options, ShutdownExitCode exitcode, string reason = "")
    {
        // ignore if server shutdown at next tick
        if (IsStopped)
            return;

        _shutdownMask = options;
        _exitCode = exitcode;

        // If the shutdown time is 0, evaluate shutdown on next tick (no message)
        if (time == 0)
        {
            _shutdownTimer = 1;
        }
        // Else set the shutdown timer and warn users
        else
        {
            _shutdownTimer = time;
            ShutdownMsg(true, null, reason);
        }

        _scriptManager.ForEach<IWorldOnShutdownInitiate>(p => p.OnShutdownInitiate(exitcode, options));
    }

    public void ShutdownMsg(bool show = false, Player player = null, string reason = "")
    {
        // not show messages for idle shutdown mode
        if (_shutdownMask.HasAnyFlag(ShutdownMask.Idle))
            return;

        // Display a message every 12 hours, hours, 5 minutes, minute, 5 seconds and finally seconds
        if (show ||
            (_shutdownTimer < 5 * Time.Minute && (_shutdownTimer % 15) == 0) ||                 // < 5 min; every 15 sec
            (_shutdownTimer < 15 * Time.Minute && (_shutdownTimer % Time.Minute) == 0) ||       // < 15 min ; every 1 min
            (_shutdownTimer < 30 * Time.Minute && (_shutdownTimer % (5 * Time.Minute)) == 0) || // < 30 min ; every 5 min
            (_shutdownTimer < 12 * Time.Hour && (_shutdownTimer % Time.Hour) == 0) ||           // < 12 h ; every 1 h
            (_shutdownTimer > 12 * Time.Hour && (_shutdownTimer % (12 * Time.Hour)) == 0))      // > 12 h ; every 12 h
        {
            var str = Time.secsToTimeString(_shutdownTimer, TimeFormat.Numeric);

            if (!reason.IsEmpty())
                str += " - " + reason;

            var msgid = _shutdownMask.HasAnyFlag(ShutdownMask.Restart) ? ServerMessageType.RestartTime : ServerMessageType.ShutdownTime;

            SendServerMessage(msgid, str, player);
            Log.Logger.Debug("Server is {0} in {1}", (_shutdownMask.HasAnyFlag(ShutdownMask.Restart) ? "restart" : "shuttingdown"), str);
        }
    }
    
    public uint ShutdownCancel()
    {
        // nothing cancel or too late
        if (_shutdownTimer == 0 || IsStopped)
            return 0;

        var msgid = _shutdownMask.HasAnyFlag(ShutdownMask.Restart) ? ServerMessageType.RestartCancelled : ServerMessageType.ShutdownCancelled;

        var oldTimer = _shutdownTimer;
        _shutdownMask = 0;
        _shutdownTimer = 0;
        _exitCode = (byte)ShutdownExitCode.Shutdown; // to default value
        SendServerMessage(msgid);

        Log.Logger.Debug("Server {0} cancelled.", (_shutdownMask.HasAnyFlag(ShutdownMask.Restart) ? "restart" : "shutdown"));

        _scriptManager.ForEach<IWorldOnShutdownCancel>(p => p.OnShutdownCancel());

        return oldTimer;
    }

    public void SendServerMessage(ServerMessageType messageID, string stringParam = "", Player player = null)
    {
        ChatServerMessage packet = new()
        {
            MessageID = (int)messageID
        };

        if (messageID <= ServerMessageType.String)
            packet.StringParam = stringParam;

        if (player != null)
            player.SendPacket(packet);
        else
            SendGlobalMessage(packet);
    }

    public void UpdateSessions(uint diff)
    {
        while (_linkSocketQueue.TryDequeue(out var linkInfo))
            ProcessLinkInstanceSocket(linkInfo);

        // Add new sessions
        while (_addSessQueue.TryDequeue(out var sess))
            AddSession_(sess);

        // Then send an update signal to remaining ones
        foreach (var pair in _sessions)
        {
            var session = pair.Value;

            if (session != null && !session.UpdateWorld(diff)) // As interval = 0
            {
                if (!RemoveQueuedPlayer(session) && _configuration.GetDefaultValue("DisconnectToleranceInterval", 0) != 0)
                    _disconnects[session.AccountId] = _gameTime.CurrentGameTime;

                RemoveQueuedPlayer(session);
                _sessions.TryRemove(pair.Key, out _);
                _sessionsByBnetGuid.Remove(session.BattlenetAccountGUID, session);
                session.Dispose();
            }
        }
    }

    public void UpdateRealmCharCount(uint accountId)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_COUNT);
        stmt.AddValue(0, accountId);
        _queryProcessor.AddCallback(_characterDatabase.AsyncQuery(stmt).WithCallback(UpdateRealmCharCount));
    }

    public void DailyReset()
    {
        // reset all saved quest status
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_DAILY);
        _characterDatabase.Execute(stmt);

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHARACTER_GARRISON_FOLLOWER_ACTIVATIONS);
        stmt.AddValue(0, 1);
        _characterDatabase.Execute(stmt);

        // reset all quest status in memory
        foreach (var itr in _sessions)
        {
            var player = itr.Value.Player;

            if (player != null)
                player.DailyReset();
        }

        // reselect pools
        _questPoolManager.ChangeDailyQuests();

        // Update faction balance
        UpdateWarModeRewardValues();

        // store next reset time
        var now = _gameTime.CurrentGameTime;
        var next = GetNextDailyResetTime(now);

        _nextDailyQuestReset = next;
        SetPersistentWorldVariable(NEXT_DAILY_QUEST_RESET_TIME_VAR_ID, (int)next);

        Log.Logger.Information("Daily quests for all characters have been reset.");
    }

    public void ResetWeeklyQuests()
    {
        // reset all saved quest status
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_WEEKLY);
        _characterDatabase.Execute(stmt);

        // reset all quest status in memory
        foreach (var itr in _sessions)
        {
            var player = itr.Value.Player;

            if (player != null)
                player.ResetWeeklyQuestStatus();
        }

        // reselect pools
        _questPoolManager.ChangeWeeklyQuests();

        // store next reset time
        var now = _gameTime.CurrentGameTime;
        var next = GetNextWeeklyResetTime(now);

        _nextWeeklyQuestReset = next;
        SetPersistentWorldVariable(NEXT_WEEKLY_QUEST_RESET_TIME_VAR_ID, (int)next);

        Log.Logger.Information("Weekly quests for all characters have been reset.");
    }

    public void ResetMonthlyQuests()
    {
        // reset all saved quest status
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_MONTHLY);
        _characterDatabase.Execute(stmt);

        // reset all quest status in memory
        foreach (var itr in _sessions)
        {
            var player = itr.Value.Player;

            if (player != null)
                player.ResetMonthlyQuestStatus();
        }

        // reselect pools
        _questPoolManager.ChangeMonthlyQuests();

        // store next reset time
        var now = _gameTime.CurrentGameTime;
        var next = GetNextMonthlyResetTime(now);

        _nextMonthlyQuestReset = next;

        Log.Logger.Information("Monthly quests for all characters have been reset.");
    }

    public void ResetEventSeasonalQuests(ushort eventID, long eventStartTime)
    {
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_RESET_CHARACTER_QUESTSTATUS_SEASONAL_BY_EVENT);
        stmt.AddValue(0, eventID);
        stmt.AddValue(1, eventStartTime);
        _characterDatabase.Execute(stmt);

        foreach (var session in _sessions.Values)
            session.Player?.ResetSeasonalQuestStatus(eventID, eventStartTime);
    }

    public string LoadDBVersion()
    {
        var dbVersion = "Unknown world database.";

        var result = _worldDatabase.Query("SELECT db_version, cache_id FROM version LIMIT 1");

        if (!result.IsEmpty())
        {
            dbVersion = result.Read<string>(0);
            // will be overwrite by config values if different and non-0 TODO  
            //WorldConfig.SetValue(WorldCfg.ClientCacheVersion, result.Read<uint>(1));
        }

        return dbVersion;
    }

    public bool IsBattlePetJournalLockAcquired(ObjectGuid battlenetAccountGuid)
    {
        foreach (var sessionForBnet in _sessionsByBnetGuid.LookupByKey(battlenetAccountGuid))
            if (sessionForBnet.BattlePetMgr.HasJournalLock)
                return true;

        return false;
    }

    public int GetPersistentWorldVariable(string var)
    {
        return _worldVariables.LookupByKey(var);
    }

    public void SetPersistentWorldVariable(string var, int value)
    {
        _worldVariables[var] = value;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.REP_WORLD_VARIABLE);
        stmt.AddValue(0, var);
        stmt.AddValue(1, value);
        _characterDatabase.Execute(stmt);
    }

    public void ReloadRBAC()
    {
        // Passive reload, we mark the data as invalidated and next time a permission is checked it will be reloaded
        Log.Logger.Information("World.ReloadRBAC()");

        foreach (var session in _sessions.Values)
            session.InvalidateRBACData();
    }

    public void IncreasePlayerCount()
    {
        _playerCount++;
        _maxPlayerCount = Math.Max(_maxPlayerCount, _playerCount);
    }

    public void DecreasePlayerCount()
    {
        _playerCount--;
    }

    public void StopNow(ShutdownExitCode exitcode = ShutdownExitCode.Error)
    {
        IsStopped = true;
        _exitCode = exitcode;
    }

    public bool LoadRealmInfo()
    {
        var result = _loginDatabase.Query("SELECT id, name, address, localAddress, localSubnetMask, port, icon, flag, timezone, allowedSecurityLevel, population, gamebuild, Region, Battlegroup FROM realmlist WHERE id = {0}", _realm.Id.Index);

        if (result.IsEmpty())
            return false;

        _realm.SetName(result.Read<string>(1));
        _realm.ExternalAddress = System.Net.IPAddress.Parse(result.Read<string>(2));
        _realm.LocalAddress = System.Net.IPAddress.Parse(result.Read<string>(3));
        _realm.LocalSubnetMask = System.Net.IPAddress.Parse(result.Read<string>(4));
        _realm.Port = result.Read<ushort>(5);
        _realm.Type = result.Read<byte>(6);
        _realm.Flags = (RealmFlags)result.Read<byte>(7);
        _realm.Timezone = result.Read<byte>(8);
        _realm.AllowedSecurityLevel = (AccountTypes)result.Read<byte>(9);
        _realm.PopulationLevel = result.Read<float>(10);
        _realm.Id.Region = result.Read<byte>(12);
        _realm.Id.Site = result.Read<byte>(13);
        _realm.Build = result.Read<uint>(11);

        return true;
    }

    public void RemoveOldCorpses()
    {
        _timers[WorldTimers.Corpses].Current = _timers[WorldTimers.Corpses].Interval;
    }

    public Locale GetAvailableDbcLocale(Locale locale)
    {
        if (_availableDbcLocaleMask[(int)locale])
            return locale;
        else
            return _defaultDbcLocale;
    }

    private void DoGuidWarningRestart()
    {
        if (_shutdownTimer != 0)
            return;

        ShutdownServ(1800, ShutdownMask.Restart, ShutdownExitCode.Restart);
        _warnShutdownTime += Time.Hour;
    }

    private void DoGuidAlertRestart()
    {
        if (_shutdownTimer != 0)
            return;

        ShutdownServ(300, ShutdownMask.Restart, ShutdownExitCode.Restart, _alertRestartReason);
    }

    private void SendGuidWarning()
    {
        if (_shutdownTimer == 0 && _guidWarn && _configuration.GetDefaultValue("Respawn.WarningFrequency", 1800) > 0)
            SendServerMessage(ServerMessageType.String, _guidWarningMsg);

        _warnDiff = 0;
    }

    private bool RemoveSession(uint id)
    {
        // Find the session, kick the user, but we can't delete session at this moment to prevent iterator invalidation
        var session = _sessions.LookupByKey(id);

        if (session != null)
        {
            if (!session.PlayerLoading.IsEmpty)
                return false;

            session.KickPlayer("World::RemoveSession");
        }

        return true;
    }

    private void AddSession_(WorldSession s)
    {
        //NOTE - Still there is race condition in WorldSession* being used in the Sockets

        // kick already loaded player with same account (if any) and remove session
        // if player is in loading and want to load again, return
        if (!RemoveSession(s.AccountId))
        {
            s.KickPlayer("World::AddSession_ Couldn't remove the other session while on loading screen");

            return;
        }

        // decrease session counts only at not reconnection case
        var decreaseSession = true;

        // if session already exist, prepare to it deleting at next world update
        // NOTE - KickPlayer() should be called on "old" in RemoveSession()
        {
            var old = _sessions.LookupByKey(s.AccountId);

            if (old != null)
            {
                // prevent decrease sessions count if session queued
                if (RemoveQueuedPlayer(old))
                    decreaseSession = false;

                _sessionsByBnetGuid.Remove(old.BattlenetAccountGUID, old);
                old.Dispose();
            }
        }

        _sessions[s.AccountId] = s;
        _sessionsByBnetGuid.Add(s.BattlenetAccountGUID, s);

        var sessions = ActiveAndQueuedSessionCount;
        var pLimit = PlayerAmountLimit;
        var queueSize = QueuedSessionCount; //number of players in the queue

        //so we don't count the user trying to
        //login as a session and queue the socket that we are using
        if (decreaseSession)
            --sessions;

        if (pLimit > 0 && sessions >= pLimit && !s.HasPermission(RBACPermissions.SkipQueue) && !HasRecentlyDisconnected(s))
        {
            AddQueuedPlayer(s);
            UpdateMaxSessionCounters();
            Log.Logger.Information("PlayerQueue: Account id {0} is in Queue Position ({1}).", s.AccountId, ++queueSize);

            return;
        }

        s.InitializeSession();

        UpdateMaxSessionCounters();

        // Updates the population
        if (pLimit > 0)
        {
            float popu = ActiveSessionCount; // updated number of users on the server
            popu /= pLimit;
            popu *= 2;
            Log.Logger.Information("Server Population ({0}).", popu);
        }
    }

    private void ProcessLinkInstanceSocket(Tuple<WorldSocket, ulong> linkInfo)
    {
        if (!linkInfo.Item1.IsOpen())
            return;

        ConnectToKey key = new()
        {
            Raw = linkInfo.Item2
        };

        var session = FindSession(key.AccountId);

        if (!session || session.ConnectToInstanceKey != linkInfo.Item2)
        {
            linkInfo.Item1.SendAuthResponseError(BattlenetRpcErrorCode.TimedOut);
            linkInfo.Item1.CloseSocket();

            return;
        }

        linkInfo.Item1.SetWorldSession(session);
        session.AddInstanceConnection(linkInfo.Item1);
        session.HandleContinuePlayerLogin(); // this is in character handler. need to figure out how to access
    }

    private bool HasRecentlyDisconnected(WorldSession session)
    {
        if (session == null)
            return false;

        uint tolerance = 0; // TODO WHY

        if (tolerance != 0)
            foreach (var disconnect in _disconnects)
                if ((disconnect.Value - _gameTime.CurrentGameTime) < tolerance)
                {
                    if (disconnect.Key == session.AccountId)
                        return true;
                }
                else
                {
                    _disconnects.Remove(disconnect.Key);
                }

        return false;
    }

    private uint GetQueuePos(WorldSession sess)
    {
        uint position = 1;

        foreach (var iter in _queuedPlayer)
            if (iter != sess)
                ++position;
            else
                return position;

        return 0;
    }

    private void AddQueuedPlayer(WorldSession sess)
    {
        sess.SetInQueue(true);
        _queuedPlayer.Add(sess);

        // The 1st SMSG_AUTH_RESPONSE needs to contain other info too.
        sess.SendAuthResponse(BattlenetRpcErrorCode.Ok, true, GetQueuePos(sess));
    }

    private bool RemoveQueuedPlayer(WorldSession sess)
    {
        // sessions count including queued to remove (if removed_session set)
        var sessions = ActiveSessionCount;

        uint position = 1;

        // search to remove and count skipped positions
        var found = false;

        foreach (var iter in _queuedPlayer)
            if (iter != sess)
            {
                ++position;
            }
            else
            {
                sess.SetInQueue(false);
                sess.ResetTimeOutTime(false);
                _queuedPlayer.Remove(iter);
                found = true; // removing queued session

                break;
            }

        // iter point to next socked after removed or end()
        // position store position of removed socket and then new position next socket after removed

        // if session not queued then we need decrease sessions count
        if (!found && sessions != 0)
            --sessions;

        // accept first in queue
        if ((_playerLimit == 0 || sessions < _playerLimit) && !_queuedPlayer.Empty())
        {
            var popSess = _queuedPlayer.First();
            popSess.InitializeSession();

            _queuedPlayer.RemoveAt(0);

            // update iter to point first queued socket or end() if queue is empty now
            position = 1;
        }

        // update position from iter to end()
        // iter point to first not updated socket, position store new position
        foreach (var iter in _queuedPlayer)
            iter.SendAuthWaitQueue(++position);

        return found;
    }

    private void KickAllLess(AccountTypes sec)
    {
        // session not removed at kick and will removed in next update tick
        foreach (var session in _sessions.Values)
            if (session.Security < sec)
                session.KickPlayer("World::KickAllLess");
    }

    private void UpdateGameTime()
    {
        // update the time
        var lastGameTime = _gameTime.CurrentGameTime;
        _gameTime.UpdateGameTimers();

        var elapsed = (uint)(_gameTime.CurrentGameTime - lastGameTime);

        //- if there is a shutdown timer
        if (!IsStopped && _shutdownTimer > 0 && elapsed > 0)
        {
            //- ... and it is overdue, stop the world
            if (_shutdownTimer <= elapsed)
            {
                if (!_shutdownMask.HasAnyFlag(ShutdownMask.Idle) || ActiveAndQueuedSessionCount == 0)
                    IsStopped = true; // exist code already set
                else
                    _shutdownTimer = 1; // minimum timer value to wait idle state
            }
            //- ... else decrease it and if necessary display a shutdown countdown to the users
            else
            {
                _shutdownTimer -= elapsed;

                ShutdownMsg();
            }
        }
    }

    private void SendAutoBroadcast()
    {
        if (_autobroadcasts.Empty())
            return;

        var pair = _autobroadcasts.SelectRandomElementByWeight(autoPair => autoPair.Value.Weight);

        var abcenter = _configuration.GetDefaultValue("AutoBroadcast.Center", 0);

        if (abcenter == 0)
        {
            SendWorldText(CypherStrings.AutoBroadcast, pair.Value.Message);
        }
        else if (abcenter == 1)
        {
            SendGlobalMessage(new PrintNotification(pair.Value.Message));
        }
        else if (abcenter == 2)
        {
            SendWorldText(CypherStrings.AutoBroadcast, pair.Value.Message);
            SendGlobalMessage(new PrintNotification(pair.Value.Message));
        }

        Log.Logger.Debug("AutoBroadcast: '{0}'", pair.Value.Message);
    }

    private void UpdateRealmCharCount(SQLResult result)
    {
        if (!result.IsEmpty())
        {
            var id = result.Read<uint>(0);
            var charCount = result.Read<uint>(1);

            var stmt = _loginDatabase.GetPreparedStatement(LoginStatements.REP_REALM_CHARACTERS);
            stmt.AddValue(0, charCount);
            stmt.AddValue(1, id);
            stmt.AddValue(2, _realm.Id.Index);
            _loginDatabase.DirectExecute(stmt);
        }
    }

    private void InitQuestResetTimes()
    {
        _nextDailyQuestReset = GetPersistentWorldVariable(NEXT_DAILY_QUEST_RESET_TIME_VAR_ID);
        _nextWeeklyQuestReset = GetPersistentWorldVariable(NEXT_WEEKLY_QUEST_RESET_TIME_VAR_ID);
        _nextMonthlyQuestReset = GetPersistentWorldVariable(NEXT_MONTHLY_QUEST_RESET_TIME_VAR_ID);
    }

    private long GetNextDailyResetTime(long t)
    {
        return Time.GetLocalHourTimestamp(t, _configuration.GetDefaultValue("Quests.DailyResetTime", 3u));
    }

    private long GetNextWeeklyResetTime(long t)
    {
        t = GetNextDailyResetTime(t);
        var time = Time.UnixTimeToDateTime(t);
        var wday = (int)time.DayOfWeek;
        var target = _configuration.GetDefaultValue("Quests.WeeklyResetWDay", 3);

        if (target < wday)
            wday -= 7;

        t += (Time.Day * (target - wday));

        return t;
    }

    private long GetNextMonthlyResetTime(long t)
    {
        t = GetNextDailyResetTime(t);
        var time = Time.UnixTimeToDateTime(t);

        if (time.Day == 1)
            return t;

        var newDate = new DateTime(time.Year, time.Month + 1, 1, 0, 0, 0, time.Kind);

        return Time.DateTimeToUnixTime(newDate);
    }

    private void CheckScheduledResetTimes()
    {
        var now = _gameTime.CurrentGameTime;

        if (_nextDailyQuestReset <= now)
            _taskManager.Schedule(DailyReset);

        if (_nextWeeklyQuestReset <= now)
            _taskManager.Schedule(ResetWeeklyQuests);

        if (_nextMonthlyQuestReset <= now)
            _taskManager.Schedule(ResetMonthlyQuests);
    }

    private void InitRandomBGResetTime()
    {
        long bgtime = GetPersistentWorldVariable(NEXT_BG_RANDOM_DAILY_RESET_TIME_VAR_ID);

        if (bgtime == 0)
            _nextRandomBgReset = _gameTime.CurrentGameTime; // game time not yet init

        // generate time by config
        var curTime = _gameTime.CurrentGameTime;

        // current day reset time
        var nextDayResetTime = Time.GetNextResetUnixTime(_configuration.GetDefaultValue("Battleground.Random.ResetHour", 6));

        // next reset time before current moment
        if (curTime >= nextDayResetTime)
            nextDayResetTime += Time.Day;

        // normalize reset time
        _nextRandomBgReset = bgtime < curTime ? nextDayResetTime - Time.Day : nextDayResetTime;

        if (bgtime == 0)
            SetPersistentWorldVariable(NEXT_BG_RANDOM_DAILY_RESET_TIME_VAR_ID, (int)_nextRandomBgReset);
    }

    private void InitCalendarOldEventsDeletionTime()
    {
        var now = _gameTime.CurrentGameTime;
        var nextDeletionTime = Time.GetLocalHourTimestamp(now, _configuration.GetDefaultValue("Calendar.DeleteOldEventsHour", 6u));
        long currentDeletionTime = GetPersistentWorldVariable(NEXT_OLD_CALENDAR_EVENT_DELETION_TIME_VAR_ID);

        // If the reset time saved in the worldstate is before now it means the server was offline when the reset was supposed to occur.
        // In this case we set the reset time in the past and next world update will do the reset and schedule next one in the future.
        if (currentDeletionTime < now)
            _nextCalendarOldEventsDeletionTime = nextDeletionTime - Time.Day;
        else
            _nextCalendarOldEventsDeletionTime = nextDeletionTime;

        if (currentDeletionTime == 0)
            SetPersistentWorldVariable(NEXT_OLD_CALENDAR_EVENT_DELETION_TIME_VAR_ID, (int)_nextCalendarOldEventsDeletionTime);
    }

    private void InitGuildResetTime()
    {
        long gtime = GetPersistentWorldVariable(NEXT_GUILD_DAILY_RESET_TIME_VAR_ID);

        if (gtime == 0)
            _nextGuildReset = _gameTime.CurrentGameTime; // game time not yet init

        var curTime = _gameTime.CurrentGameTime;
        var nextDayResetTime = Time.GetNextResetUnixTime(_configuration.GetDefaultValue("Guild.ResetHour", 6));

        if (curTime >= nextDayResetTime)
            nextDayResetTime += Time.Day;

        // normalize reset time
        _nextGuildReset = gtime < curTime ? nextDayResetTime - Time.Day : nextDayResetTime;

        if (gtime == 0)
            SetPersistentWorldVariable(NEXT_GUILD_DAILY_RESET_TIME_VAR_ID, (int)_nextGuildReset);
    }

    private void InitCurrencyResetTime()
    {
        long currencytime = GetPersistentWorldVariable(NEXT_CURRENCY_RESET_TIME_VAR_ID);

        if (currencytime == 0)
            _nextCurrencyReset = _gameTime.CurrentGameTime; // game time not yet init

        // generate time by config
        var curTime = _gameTime.CurrentGameTime;

        var nextWeekResetTime = Time.GetNextResetUnixTime(_configuration.GetDefaultValue("Currency.ResetDay", 3), _configuration.GetDefaultValue("Currency.ResetHour", 3));

        // next reset time before current moment
        if (curTime >= nextWeekResetTime)
            nextWeekResetTime += _configuration.GetDefaultValue("Currency.ResetInterval", 7) * Time.Day;

        // normalize reset time
        _nextCurrencyReset = currencytime < curTime ? nextWeekResetTime - _configuration.GetDefaultValue("Currency.ResetInterval", 7) * Time.Day : nextWeekResetTime;

        if (currencytime == 0)
            SetPersistentWorldVariable(NEXT_CURRENCY_RESET_TIME_VAR_ID, (int)_nextCurrencyReset);
    }

    private void ResetCurrencyWeekCap()
    {
        _characterDatabase.Execute("UPDATE `character_currency` SET `WeeklyQuantity` = 0");

        foreach (var session in _sessions.Values)
            if (session.Player != null)
                session.Player.ResetCurrencyWeekCap();

        _nextCurrencyReset += Time.Day * _configuration.GetDefaultValue("Currency.ResetInterval", 7);
        SetPersistentWorldVariable(NEXT_CURRENCY_RESET_TIME_VAR_ID, (int)_nextCurrencyReset);
    }

    private void ResetRandomBG()
    {
        Log.Logger.Information("Random BG status reset for all characters.");

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_BATTLEGROUND_RANDOM_ALL);
        _characterDatabase.Execute(stmt);

        foreach (var session in _sessions.Values)
            if (session.Player)
                session.Player.SetRandomWinner(false);

        _nextRandomBgReset += Time.Day;
        SetPersistentWorldVariable(NEXT_BG_RANDOM_DAILY_RESET_TIME_VAR_ID, (int)_nextRandomBgReset);
    }

    private void CalendarDeleteOldEvents()
    {
        Log.Logger.Information("Calendar deletion of old events.");

        _nextCalendarOldEventsDeletionTime = _nextCalendarOldEventsDeletionTime + Time.Day;
        SetPersistentWorldVariable(NEXT_OLD_CALENDAR_EVENT_DELETION_TIME_VAR_ID, (int)_nextCalendarOldEventsDeletionTime);
        _calendarManager.DeleteOldEvents();
    }

    private void ResetGuildCap()
    {
        _nextGuildReset += Time.Day;
        SetPersistentWorldVariable(NEXT_GUILD_DAILY_RESET_TIME_VAR_ID, (int)_nextGuildReset);
        var week = GetPersistentWorldVariable(NEXT_GUILD_WEEKLY_RESET_TIME_VAR_ID);
        week = week < 7 ? week + 1 : 1;

        Log.Logger.Information("Guild Daily Cap reset. Week: {0}", week == 1);
        SetPersistentWorldVariable(NEXT_GUILD_WEEKLY_RESET_TIME_VAR_ID, week);
        _guildManager.ResetTimes(week == 1);
    }

    private void UpdateMaxSessionCounters()
    {
        _maxActiveSessionCount = Math.Max(_maxActiveSessionCount, (uint)(_sessions.Count - _queuedPlayer.Count));
        _maxQueuedSessionCount = Math.Max(_maxQueuedSessionCount, (uint)_queuedPlayer.Count);
    }

    private void UpdateAreaDependentAuras()
    {
        foreach (var session in _sessions.Values)
            if (session.Player is { IsInWorld: true })
            {
                session.Player.UpdateAreaDependentAuras(session.Player.Area);
                session.Player.UpdateZoneDependentAuras(session.Player.Zone);
            }
    }

    private void LoadPersistentWorldVariables()
    {
        var oldMSTime = Time.MSTime;

        var result = _characterDatabase.Query("SELECT ID, Value FROM world_variable");

        if (!result.IsEmpty())
            do
            {
                _worldVariables[result.Read<string>(0)] = result.Read<int>(1);
            } while (result.NextRow());

        Log.Logger.Information($"Loaded {_worldVariables.Count} world variables in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    private void ProcessQueryCallbacks()
    {
        _queryProcessor.ProcessReadyCallbacks();
    }

    private long GetNextRandomBGResetTime()
    {
        return _nextRandomBgReset;
    }

    private void UpdateWarModeRewardValues()
    {
        var warModeEnabledFaction = new long[2];

        // Search for characters that have war mode enabled and played during the last week
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.SEL_WAR_MODE_TUNING);
        stmt.AddValue(0, (uint)PlayerFlags.WarModeDesired);
        stmt.AddValue(1, (uint)PlayerFlags.WarModeDesired);

        var result = _characterDatabase.Query(stmt);

        if (!result.IsEmpty())
            do
            {
                var race = result.Read<byte>(0);

                var raceEntry = _cliDB.ChrRacesStorage.LookupByKey(race);

                if (raceEntry != null)
                {
                    var raceFaction = _cliDB.FactionTemplateStorage.LookupByKey((uint)raceEntry.FactionID);

                    if (raceFaction != null)
                    {
                        if ((raceFaction.FactionGroup & (byte)FactionMasks.Alliance) != 0)
                            warModeEnabledFaction[TeamIds.Alliance] += result.Read<long>(1);
                        else if ((raceFaction.FactionGroup & (byte)FactionMasks.Horde) != 0)
                            warModeEnabledFaction[TeamIds.Horde] += result.Read<long>(1);
                    }
                }
            } while (result.NextRow());


        var dominantFaction = TeamIds.Alliance;
        var outnumberedFactionReward = 0;

        if (warModeEnabledFaction.Any(val => val != 0))
        {
            var dominantFactionCount = warModeEnabledFaction[TeamIds.Alliance];

            if (warModeEnabledFaction[TeamIds.Alliance] < warModeEnabledFaction[TeamIds.Horde])
            {
                dominantFactionCount = warModeEnabledFaction[TeamIds.Horde];
                dominantFaction = TeamIds.Horde;
            }

            double total = warModeEnabledFaction[TeamIds.Alliance] + warModeEnabledFaction[TeamIds.Horde];
            var pct = dominantFactionCount / total;

            if (pct >= _configuration.GetDefaultValue("Pvp.FactionBalance.Pct20", 0.8f))
                outnumberedFactionReward = 20;
            else if (pct >= _configuration.GetDefaultValue("Pvp.FactionBalance.Pct10", 0.7f))
                outnumberedFactionReward = 10;
            else if (pct >= _configuration.GetDefaultValue("Pvp.FactionBalance.Pct5", 0.6f))
                outnumberedFactionReward = 5;
        }

        _worldStateManager.SetValueAndSaveInDb(WorldStates.WarModeHordeBuffValue, 10 + (dominantFaction == TeamIds.Alliance ? outnumberedFactionReward : 0), false, null);
        _worldStateManager.SetValueAndSaveInDb(WorldStates.WarModeAllianceBuffValue, 10 + (dominantFaction == TeamIds.Horde ? outnumberedFactionReward : 0), false, null);
    }
}