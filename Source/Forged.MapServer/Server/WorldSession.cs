// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Autofac;
using Forged.MapServer.Accounts;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Battlepay;
using Forged.MapServer.BattlePets;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Guilds;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Authentication;
using Forged.MapServer.Networking.Packets.Battlenet;
using Forged.MapServer.Networking.Packets.Character;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Networking.Packets.ClientConfig;
using Forged.MapServer.Networking.Packets.Hotfix;
using Forged.MapServer.Networking.Packets.Instance;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Movement;
using Forged.MapServer.Networking.Packets.System;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Realm;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Server;

public class WorldSession : IDisposable
{
    public long MuteTime;

    private readonly DosProtection _antiDos;
    private readonly AccountData[] _accountData = new AccountData[(int)AccountDataTypes.Max];
    private readonly uint[] _tutorials = new uint[SharedConst.MaxAccountTutorialValues];

    private readonly List<string> _registeredAddonPrefixes = new();
    private readonly ClassFactory _classFactory;

    private readonly ActionBlock<WorldPacket> _recvQueue;

    private readonly ConcurrentQueue<WorldPacket> _threadUnsafe = new();
    private readonly ConcurrentQueue<WorldPacket> _inPlaceQueue = new();
    private readonly ConcurrentQueue<WorldPacket> _threadSafeQueue = new();

    private readonly AsyncCallbackProcessor<QueryCallback> _queryProcessor = new();
    private readonly AsyncCallbackProcessor<TransactionCallback> _transactionCallbacks = new();
    private readonly AsyncCallbackProcessor<ISqlCallback> _queryHolderProcessor = new();

    private readonly CancellationTokenSource _cancellationToken = new();
    private readonly AutoResetEvent _asyncMessageQueueSemaphore = new(false);

    private readonly IConfiguration _configuration;
    private readonly Realm _realm;
    private readonly LoginDatabase _loginDatabase;
    private readonly CharacterDatabase _characterDatabase;
    private readonly OutdoorPvPManager _outdoorPvPManager;
    private readonly BattlegroundManager _battlegroundManager;
    private readonly InstanceLockManager _instanceLockManager;
    private readonly CliDB _cliDB;
    private readonly MapManager _mapManager;
    private readonly DB2Manager _db2Manager;
    private readonly PacketManager _packetManager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly CharacterTemplateDataStorage _characterTemplateDataStorage;
    private readonly ScriptManager _scriptManager;
    private readonly SocialManager _socialManager;
    private readonly GuildManager _guildManager;

    private uint _expireTime;
    private bool _forceExit;
    private Warden.Warden _warden; // Remains NULL if Warden system is not enabled by config

    private long _logoutTime;
    private bool _inQueue;
    private ObjectGuid _playerLoading; // code processed in LoginPlayer
    private bool _playerSave;
    private TutorialsFlag _tutorialsChanged;

    private bool _filterAddonMessages;
    private long _timeOutTime;

    private uint _timeSyncNextCounter;
    private uint _timeSyncTimer;

    private ConnectToKey _instanceConnectKey;

    public WorldSocket Socket { get; }

    // Packets cooldown

    public bool CanSpeak => MuteTime <= GameTime.GetGameTime();

    public string PlayerName => Player != null ? Player.GetName() : "Unknown";

    public bool PlayerLoading => !_playerLoading.IsEmpty;
    public bool PlayerLogout { get; private set; }

    public bool PlayerLogoutWithSave => PlayerLogout && _playerSave;
    public bool PlayerRecentlyLoggedOut { get; private set; }

    public bool PlayerDisconnected => !(Socket != null && Socket.IsOpen());

    public AccountTypes Security { get; private set; }

    public uint AccountId { get; }

    public ObjectGuid AccountGUID => ObjectGuid.Create(HighGuid.WowAccount, AccountId);
    public string AccountName { get; }

    public uint BattlenetAccountId { get; }

    public ObjectGuid BattlenetAccountGUID => ObjectGuid.Create(HighGuid.BNetAccount, BattlenetAccountId);

    public Player Player { get; set; }

    public string RemoteAddress { get; }

    public Expansion AccountExpansion { get; }

    public Expansion Expansion { get; }

    public string OS { get; }

    public bool IsLogingOut => _logoutTime != 0 || PlayerLogout;
    public ulong ConnectToInstanceKey => _instanceConnectKey.Raw;
    public AsyncCallbackProcessor<QueryCallback> QueryProcessor => _queryProcessor;

    public RBACData RBACData { get; private set; }

    public Locale SessionDbcLocale { get; }

    public Locale SessionDbLocaleIndex { get; }

    public uint Latency { get; set; }

    private bool IsConnectionIdle => _timeOutTime < GameTime.GetGameTime() && !_inQueue;

    public uint RecruiterId { get; }

    public bool IsARecruiter { get; }

    // Packets cooldown
    public long CalendarEventCreationCooldown { get; set; }

    // Battle Pets
    public BattlePetMgr BattlePetMgr { get; }

    public CollectionMgr CollectionMgr { get; }

    // Battlenet
    public Array<byte> RealmListSecret { get; private set; } = new(32);

    public Dictionary<uint, byte> RealmCharacterCounts { get; } = new();

    public CommandHandler CommandHandler { get; private set; }

    public BattlepayManager BattlePayMgr { get; }

    public WorldSession(uint id, string name, uint battlenetAccountId, WorldSocket sock, AccountTypes sec, Expansion expansion, long muteTime, string os, Locale locale, uint recruiter, bool isARecruiter, ClassFactory classFactory)
    {
        MuteTime = muteTime;
        _antiDos = new DosProtection(this);
        Socket = sock;
        Security = sec;
        AccountId = id;
        AccountName = name;
        BattlenetAccountId = battlenetAccountId;
        _classFactory = classFactory;
        _configuration = classFactory.Resolve<IConfiguration>();
        _realm = classFactory.Resolve<Realm>();
        _loginDatabase = classFactory.Resolve<LoginDatabase>();
        _characterDatabase = classFactory.Resolve<CharacterDatabase>();
        _outdoorPvPManager = classFactory.Resolve<OutdoorPvPManager>();
        _battlegroundManager = classFactory.Resolve<BattlegroundManager>();
        _instanceLockManager = classFactory.Resolve<InstanceLockManager>();
        _cliDB = classFactory.Resolve<CliDB>();
        _mapManager = classFactory.Resolve<MapManager>();
        _db2Manager = classFactory.Resolve<DB2Manager>();
        _packetManager = classFactory.Resolve<PacketManager>();
        _gameObjectManager = classFactory.Resolve<GameObjectManager>();
        _characterTemplateDataStorage = classFactory.Resolve<CharacterTemplateDataStorage>();
        _scriptManager = classFactory.Resolve<ScriptManager>();
        _socialManager = classFactory.Resolve<SocialManager>();
        _guildManager = classFactory.Resolve<GuildManager>();

        var configuredExpansion = _configuration.GetDefaultValue("Player.OverrideExpansion", -1) == -1 ? Expansion.LevelCurrent : (Expansion)_configuration.GetDefaultValue("Player.OverrideExpansion", -1);
        AccountExpansion = Expansion.LevelCurrent == configuredExpansion ? expansion : configuredExpansion;
        Expansion = (Expansion)Math.Min((byte)expansion, _configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight));
        OS = os;
        SessionDbcLocale = classFactory.Resolve<WorldManager>().GetAvailableDbcLocale(locale);
        SessionDbLocaleIndex = locale;
        RecruiterId = recruiter;
        IsARecruiter = isARecruiter;
        _expireTime = 60000; // 1 min after socket loss, session is deleted
        BattlePetMgr = _classFactory.Resolve<BattlePetMgr>(new PositionalParameter(0, this));
        CollectionMgr = _classFactory.Resolve<CollectionMgr>(new PositionalParameter(0, this));
        BattlePayMgr = new BattlepayManager(this);
        CommandHandler = new CommandHandler(this);

        _recvQueue = new ActionBlock<WorldPacket>(ProcessQueue,
                                                  new ExecutionDataflowBlockOptions()
                                                  {
                                                      MaxDegreeOfParallelism = 10,
                                                      EnsureOrdered = true,
                                                      CancellationToken = _cancellationToken.Token
                                                  });

        Task.Run(ProcessInPlace, _cancellationToken.Token);

        RemoteAddress = sock.GetRemoteIpAddress().Address.ToString();
        ResetTimeOutTime(false);
        _loginDatabase.Execute("UPDATE account SET online = 1 WHERE id = {0};", AccountId); // One-time query
    }

    public void Dispose()
    {
        _cancellationToken.Cancel();

        // unload player if not unloaded
        if (Player)
            LogoutPlayer(true);

        // - If have unclosed socket, close it
        if (Socket != null)
        {
            Socket.CloseSocket();
            Socket = null;
        }

        // empty incoming packet queue
        _recvQueue.Complete();

        _loginDatabase.Execute("UPDATE account SET online = 0 WHERE id = {0};", AccountId); // One-time query
    }

    public void LogoutPlayer(bool save)
    {
        if (PlayerLogout)
            return;

        // finish pending transfers before starting the logout
        while (Player && Player.IsBeingTeleportedFar)
            HandleMoveWorldportAck();

        PlayerLogout = true;
        _playerSave = save;

        if (Player)
        {
            if (!Player.GetLootGUID().IsEmpty)
                DoLootReleaseAll();

            // If the player just died before logging out, make him appear as a ghost
            //FIXME: logout must be delayed in case lost connection with client in time of combat
            if (Player.DeathTimer != 0)
            {
                Player.CombatStop();
                Player.BuildPlayerRepop();
                Player.RepopAtGraveyard();
            }
            else if (Player.HasAuraType(AuraType.SpiritOfRedemption))
            {
                // this will kill character by SPELL_AURA_SPIRIT_OF_REDEMPTION
                Player.RemoveAurasByType(AuraType.ModShapeshift);
                Player.KillPlayer();
                Player.BuildPlayerRepop();
                Player.RepopAtGraveyard();
            }
            else if (Player.HasPendingBind)
            {
                Player.RepopAtGraveyard();
                Player.SetPendingBind(0, 0);
            }

            //drop a flag if player is carrying it
            var bg = Player.Battleground;

            if (bg)
                bg.EventPlayerLoggedOut(Player);

            // Teleport to home if the player is in an invalid instance
            if (!Player.InstanceValid && !Player.IsGameMaster)
                Player.TeleportTo(Player.Homebind);

            _outdoorPvPManager.HandlePlayerLeaveZone(Player, Player.Location.Zone);

            for (uint i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            {
                var bgQueueTypeId = Player.GetBattlegroundQueueTypeId(i);

                if (bgQueueTypeId != default)
                {
                    Player.RemoveBattlegroundQueueId(bgQueueTypeId);
                    var queue = _battlegroundManager.GetBattlegroundQueue(bgQueueTypeId);
                    queue.RemovePlayer(Player.GUID, true);
                }
            }

            // Repop at GraveYard or other player far teleport will prevent saving player because of not present map
            // Teleport player immediately for correct player save
            while (Player.IsBeingTeleportedFar)
                HandleMoveWorldportAck();

            // If the player is in a guild, update the guild roster and broadcast a logout message to other guild members
            var guild = _guildManager.GetGuildById(Player.GuildId);

            if (guild)
                guild.HandleMemberLogout(this);

            // Remove pet
            Player.RemovePet(null, PetSaveMode.AsCurrent, true);

            //- Release battle pet journal lock
            if (BattlePetMgr.HasJournalLock)
                BattlePetMgr.ToggleJournalLock(false);

            // Clear whisper whitelist
            Player.ClearWhisperWhiteList();

            // empty buyback items and save the player in the database
            // some save parts only correctly work in case player present in map/player_lists (pets, etc)
            if (save)
            {
                for (uint j = InventorySlots.BuyBackStart; j < InventorySlots.BuyBackEnd; ++j)
                {
                    var eslot = j - InventorySlots.BuyBackStart;
                    Player.SetInvSlot(j, ObjectGuid.Empty);
                    Player.SetBuybackPrice(eslot, 0);
                    Player.SetBuybackTimestamp(eslot, 0);
                }

                Player.SaveToDB();
            }

            // Leave all channels before player delete...
            Player.CleanupChannels();

            // If the player is in a group (or invited), remove him. If the group if then only 1 person, disband the group.
            Player.UninviteFromGroup();

            //! Send update to group and reset stored max enchanting level
            var group = Player.Group;

            if (group != null)
            {
                group.SendUpdate();

                if (group.LeaderGUID == Player.GUID)
                    group.StartLeaderOfflineTimer();
            }

            //! Broadcast a logout message to the player's friends
            _socialManager.SendFriendStatus(Player, FriendsResult.Offline, Player.GUID, true);
            Player.RemoveSocial();

            //! Call script hook before deletion
            _scriptManager.ForEach<IPlayerOnLogout>(p => p.OnLogout(Player));

            //! Remove the player from the world
            // the player may not be in the world when logging out
            // e.g if he got disconnected during a transfer to another map
            // calls to GetMap in this case may cause crashes
            Player.SetDestroyedObject(true);
            Player.CleanupsBeforeDelete();
            Log.Logger.Information($"Account: {AccountId} (IP: {RemoteAddress}) Logout Character:[{Player.GetName()}] ({Player.GUID}) Level: {Player.Level}, XP: {Player.XP}/{Player.XPForNextLevel} ({Player.XPForNextLevel - Player.XP} left)");

            var map = Player.Location.Map;

            if (map != null)
                map.RemovePlayerFromMap(Player, true);

            Player = null;

            //! Send the 'logout complete' packet to the client
            //! Client will respond by sending 3x CMSG_CANCEL_TRADE, which we currently dont handle
            LogoutComplete logoutComplete = new();
            SendPacket(logoutComplete);

            //! Since each account can only have one online character at any given time, ensure all characters for active account are marked as offline
            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_ACCOUNT_ONLINE);
            stmt.AddValue(0, AccountId);
            _characterDatabase.Execute(stmt);
        }

        if (Socket != null)
        {
            Socket.CloseSocket();
            Socket = null;
        }

        PlayerLogout = false;
        _playerSave = false;
        PlayerRecentlyLoggedOut = true;
        SetLogoutStartTime(0);
    }


    public bool UpdateMap(uint diff)
    {
        DrainQueue(_threadSafeQueue);

        // Send time sync packet every 10s.
        if (_timeSyncTimer > 0)
        {
            if (diff >= _timeSyncTimer)
                SendTimeSync();
            else
                _timeSyncTimer -= diff;
        }

        ProcessQueryCallbacks();

        return true;
    }

    public bool UpdateWorld(uint diff)
    {
        var currentTime = DrainQueue(_threadUnsafe);

        ProcessQueryCallbacks();


        if (Socket != null && Socket.IsOpen() && _warden != null)
            _warden.Update(diff);

        // If necessary, log the player out
        if (ShouldLogOut(currentTime) && _playerLoading.IsEmpty)
            LogoutPlayer(true);

        //- Cleanup socket if need
        if (Socket != null && !Socket.IsOpen())
        {
            if (Player != null && _warden != null)
                _warden.Update(diff);

            _expireTime -= _expireTime > diff ? diff : _expireTime;

            if (_expireTime < diff || _forceExit || !Player)
                if (Socket != null)
                {
                    Socket.CloseSocket();
                    Socket = null;
                }
        }

        if (Socket == null)
            return false; //Will remove this session from the world session map


        return true;
    }

    public void QueuePacket(WorldPacket packet)
    {
        _recvQueue.Post(packet);
    }

    public void SendPacket(ServerPacket packet)
    {
        if (packet == null)
            return;

        if (packet.GetOpcode() == ServerOpcodes.Unknown || packet.GetOpcode() == ServerOpcodes.Max)
        {
            Log.Logger.Error("Prevented sending of UnknownOpcode to {0}", GetPlayerInfo());

            return;
        }

        var conIdx = packet.GetConnection();

        if (conIdx != ConnectionType.Instance && _packetManager.IsInstanceOnlyOpcode(packet.GetOpcode()))
        {
            Log.Logger.Error("Prevented sending of instance only opcode {0} with connection type {1} to {2}", packet.GetOpcode(), packet.GetConnection(), GetPlayerInfo());

            return;
        }

        if (Socket == null)
        {
            Log.Logger.Verbose("Prevented sending of {0} to non existent socket {1} to {2}", packet.GetOpcode(), conIdx, GetPlayerInfo());

            return;
        }

        Socket.SendPacket(packet);
    }

    public void KickPlayer(string reason)
    {
        Log.Logger.Information($"Account: {AccountId} Character: '{(Player ? Player.GetName() : "<none>")}' {(Player ? Player.GUID : "")} kicked with reason: {reason}");

        if (Socket == null)
            return;

        Socket.CloseSocket();
        _forceExit = true;
    }

    public bool IsAddonRegistered(string prefix)
    {
        if (!_filterAddonMessages) // if we have hit the softcap (64) nothing should be filtered
            return true;

        if (_registeredAddonPrefixes.Empty())
            return false;

        return _registeredAddonPrefixes.Contains(prefix);
    }

    public void SendAccountDataTimes(ObjectGuid playerGuid, AccountDataTypes mask)
    {
        AccountDataTimes accountDataTimes = new()
        {
            PlayerGuid = playerGuid,
            ServerTime = GameTime.GetGameTime()
        };

        for (var i = 0; i < (int)AccountDataTypes.Max; ++i)
            if (((int)mask & (1 << i)) != 0)
                accountDataTimes.AccountTimes[i] = GetAccountData((AccountDataTypes)i).Time;

        SendPacket(accountDataTimes);
    }

    public void LoadTutorialsData(SQLResult result)
    {
        if (!result.IsEmpty())
        {
            for (var i = 0; i < SharedConst.MaxAccountTutorialValues; i++)
                _tutorials[i] = result.Read<uint>(i);

            _tutorialsChanged |= TutorialsFlag.LoadedFromDB;
        }

        _tutorialsChanged &= ~TutorialsFlag.Changed;
    }

    public void SaveTutorialsData(SQLTransaction trans)
    {
        if (!_tutorialsChanged.HasAnyFlag(TutorialsFlag.Changed))
            return;

        var hasTutorialsInDB = _tutorialsChanged.HasAnyFlag(TutorialsFlag.LoadedFromDB);
        var stmt = _characterDatabase.GetPreparedStatement(hasTutorialsInDB ? CharStatements.UPD_TUTORIALS : CharStatements.INS_TUTORIALS);

        for (var i = 0; i < SharedConst.MaxAccountTutorialValues; ++i)
            stmt.AddValue(i, _tutorials[i]);

        stmt.AddValue(SharedConst.MaxAccountTutorialValues, AccountId);
        trans.Append(stmt);

        // now has, set flag so next save uses update query
        if (!hasTutorialsInDB)
            _tutorialsChanged |= TutorialsFlag.LoadedFromDB;

        _tutorialsChanged &= ~TutorialsFlag.Changed;
    }

    public void SendConnectToInstance(ConnectToSerial serial)
    {
        var instanceAddress = _realm.GetAddressForClient(System.Net.IPAddress.Parse(RemoteAddress));

        _instanceConnectKey.AccountId = AccountId;
        _instanceConnectKey.connectionType = ConnectionType.Instance;
        _instanceConnectKey.Key = RandomHelper.URand(0, 0x7FFFFFFF);

        ConnectTo connectTo = new()
        {
            Key = _instanceConnectKey.Raw,
            Serial = serial,
            Payload =
            {
                Port = (ushort)_configuration.GetDefaultValue("InstanceServerPort", 8086)
            },
            Con = (byte)ConnectionType.Instance
        };

        if (instanceAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            connectTo.Payload.Where.IPv4 = instanceAddress.Address.GetAddressBytes();
            connectTo.Payload.Where.Type = ConnectTo.AddressType.IPv4;
        }
        else
        {
            connectTo.Payload.Where.IPv6 = instanceAddress.Address.GetAddressBytes();
            connectTo.Payload.Where.Type = ConnectTo.AddressType.IPv6;
        }

        SendPacket(connectTo);
    }

    public void SendTutorialsData()
    {
        TutorialFlags packet = new();
        Array.Copy(_tutorials, packet.TutorialData, SharedConst.MaxAccountTutorialValues);
        SendPacket(packet);
    }

    public bool DisallowHyperlinksAndMaybeKick(string str)
    {
        if (!str.Contains('|'))
            return true;

        Log.Logger.Error($"Player {Player.GetName()} ({Player.GUID}) sent a message which illegally contained a hyperlink:\n{str}");

        if (_configuration.GetDefaultValue("ChatStrictLinkChecking.Kick", 0) != 0)
            KickPlayer("WorldSession::DisallowHyperlinksAndMaybeKick Illegal chat link");

        return false;
    }

    public void SendNotification(CypherStrings str, params object[] args)
    {
        SendNotification(_gameObjectManager.GetCypherString(str), args);
    }

    public void SendNotification(string str, params object[] args)
    {
        var message = string.Format(str, args);

        if (!string.IsNullOrEmpty(message))
            SendPacket(new PrintNotification(message));
    }

    public string GetPlayerInfo()
    {
        StringBuilder ss = new();
        ss.Append("[Player: ");

        if (!_playerLoading.IsEmpty)
            ss.AppendFormat("Logging in: {0}, ", _playerLoading.ToString());
        else if (Player)
            ss.AppendFormat("{0} {1}, ", Player.GetName(), Player.GUID.ToString());

        ss.AppendFormat("Account: {0}]", AccountId);

        return ss.ToString();
    }

    public void SetInQueue(bool state)
    {
        _inQueue = state;
    }

    public SQLQueryHolderCallback<TR> AddQueryHolderCallback<TR>(SQLQueryHolderCallback<TR> callback)
    {
        return (SQLQueryHolderCallback<TR>)_queryHolderProcessor.AddCallback(callback);
    }

    public bool CanAccessAlliedRaces()
    {
        if (_configuration.GetDefaultValue("CharacterCreating.DisableAlliedRaceAchievementRequirement", false))
            return true;
        else
            return AccountExpansion >= Expansion.BattleForAzeroth;
    }

    public void LoadPermissions()
    {
        var id = AccountId;
        var secLevel = Security;

        Log.Logger.Debug("WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
                         id,
                         AccountName,
                         _realm.Id.Index,
                         secLevel);

        RBACData = new RBACData(id, AccountName, (int)_realm.Id.Index, _classFactory.Resolve<AccountManager>(), _classFactory.Resolve<LoginDatabase>(), (byte)secLevel);
        RBACData.LoadFromDB();
    }

    public QueryCallback LoadPermissionsAsync()
    {
        var id = AccountId;
        var secLevel = Security;

        Log.Logger.Debug("WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
                         id,
                         AccountName,
                         _realm.Id.Index,
                         secLevel);

        RBACData = new RBACData(id, AccountName, (int)_realm.Id.Index, _classFactory.Resolve<AccountManager>(), _classFactory.Resolve<LoginDatabase>(), (byte)secLevel);

        return RBACData.LoadFromDBAsync();
    }

    public void InitializeSession()
    {
        AccountInfoQueryHolderPerRealm realmHolder = new();
        realmHolder.Initialize(AccountId, BattlenetAccountId);

        AccountInfoQueryHolder holder = new();
        holder.Initialize(AccountId, BattlenetAccountId);

        AccountInfoQueryHolderPerRealm characterHolder = null;
        AccountInfoQueryHolder loginHolder = null;

        AddQueryHolderCallback(_characterDatabase.DelayQueryHolder(realmHolder))
            .AfterComplete(result =>
            {
                characterHolder = (AccountInfoQueryHolderPerRealm)result;

                if (loginHolder != null && characterHolder != null)
                    InitializeSessionCallback(loginHolder, characterHolder);
            });

        AddQueryHolderCallback(_loginDatabase.DelayQueryHolder(holder))
            .AfterComplete(result =>
            {
                loginHolder = (AccountInfoQueryHolder)result;

                if (loginHolder != null && characterHolder != null)
                    InitializeSessionCallback(loginHolder, characterHolder);
            });
    }

    public bool HasPermission(RBACPermissions permission)
    {
        if (RBACData == null)
            LoadPermissions();

        if (RBACData == null)
            return false;

        var hasPermission = RBACData.HasPermission(permission);

        Log.Logger.Debug("WorldSession:HasPermission [AccountId: {0}, Name: {1}, realmId: {2}]",
                         RBACData.Id,
                         RBACData.Name,
                         _realm.Id.Index);

        return hasPermission;
    }

    public void InvalidateRBACData()
    {
        Log.Logger.Debug("WorldSession:Invalidaterbac:RBACData [AccountId: {0}, Name: {1}, realmId: {2}]",
                         RBACData.Id,
                         RBACData.Name,
                         _realm.Id.Index);

        RBACData = null;
    }

    public void ResetTimeSync()
    {
        _timeSyncNextCounter = 0;
    }

    public void SendTimeSync()
    {
        TimeSyncRequest timeSyncRequest = new()
        {
            SequenceIndex = _timeSyncNextCounter
        };

        SendPacket(timeSyncRequest);

        // Schedule next sync in 10 sec (except for the 2 first packets, which are spaced by only 5s)
        _timeSyncTimer = _timeSyncNextCounter == 0 ? 5000 : 10000u;
        _timeSyncNextCounter++;
    }

    public void ResetTimeOutTime(bool onlyActive)
    {
        if (Player)
            _timeOutTime = GameTime.GetGameTime() + _configuration.GetDefaultValue("SocketTimeOutTimeActive", 60);
        else if (!onlyActive)
            _timeOutTime = GameTime.GetGameTime() + _configuration.GetDefaultValue("SocketTimeOutTime", 900);
    }

    public static implicit operator bool(WorldSession session)
    {
        return session != null;
    }

    public void SendAuthResponse(BattlenetRpcErrorCode code, bool queued, uint queuePos = 0)
    {
        AuthResponse response = new()
        {
            Result = code
        };

        if (code == BattlenetRpcErrorCode.Ok)
        {
            response.SuccessInfo = new AuthResponse.AuthSuccessInfo();
            var forceRaceAndClass = _configuration.GetDefaultValue("character.EnforceRaceAndClassExpansions", true);

            response.SuccessInfo = new AuthResponse.AuthSuccessInfo
            {
                ActiveExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)Expansion,
                AccountExpansionLevel = !forceRaceAndClass ? (byte)Expansion.Dragonflight : (byte)AccountExpansion,
                VirtualRealmAddress = _realm.Id.GetAddress(),
                Time = (uint)GameTime.GetGameTime()
            };

            var realm = _realm;

            // Send current home realm. Also there is no need to send it later in realm queries.
            response.SuccessInfo.VirtualRealms.Add(new VirtualRealmInfo(realm.Id.GetAddress(), true, false, realm.Name, realm.NormalizedName));

            if (HasPermission(RBACPermissions.UseCharacterTemplates))
                foreach (var templ in _characterTemplateDataStorage.GetCharacterTemplates().Values)
                    response.SuccessInfo.Templates.Add(templ);

            response.SuccessInfo.AvailableClasses = _gameObjectManager.GetClassExpansionRequirements();
        }

        if (queued)
        {
            AuthWaitInfo waitInfo = new()
            {
                WaitCount = queuePos
            };

            response.WaitInfo = waitInfo;
        }

        SendPacket(response);
    }

    public void SendAuthWaitQueue(uint position)
    {
        if (position != 0)
        {
            WaitQueueUpdate waitQueueUpdate = new();
            waitQueueUpdate.WaitInfo.WaitCount = position;
            waitQueueUpdate.WaitInfo.WaitTime = 0;
            waitQueueUpdate.WaitInfo.HasFCM = false;
            SendPacket(waitQueueUpdate);
        }
        else
        {
            SendPacket(new WaitQueueFinish());
        }
    }

    public void SendClientCacheVersion(uint version)
    {
        ClientCacheVersion cache = new()
        {
            CacheVersion = version
        };

        SendPacket(cache); //enabled it
    }

    public void SendSetTimeZoneInformation()
    {
        // @todo: replace dummy values
        SetTimeZoneInformation packet = new()
        {
            ServerTimeTZ = "Europe/Paris",
            GameTimeTZ = "Europe/Paris",
            ServerRegionalTZ = "Europe/Paris"
        };

        SendPacket(packet); //enabled it
    }

    public void SendFeatureSystemStatusGlueScreen()
    {
        FeatureSystemStatusGlueScreen features = new()
        {
            BpayStoreAvailable = _configuration.GetDefaultValue("FeatureSystem.BpayStore.Enabled", false),
            BpayStoreDisabledByParentalControls = false,
            CharUndeleteEnabled = _configuration.GetDefaultValue("FeatureSystem.CharacterUndelete.Enabled", false),
            BpayStoreEnabled = _configuration.GetDefaultValue("FeatureSystem.BpayStore.Enabled", false),
            MaxCharactersPerRealm = _configuration.GetDefaultValue("CharactersPerRealm", 60),
            MinimumExpansionLevel = (int)Expansion.Classic,
            MaximumExpansionLevel = _configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight)
        };

        var europaTicketConfig = new EuropaTicketConfig();
        europaTicketConfig.ThrottleState.MaxTries = 10;
        europaTicketConfig.ThrottleState.PerMilliseconds = 60000;
        europaTicketConfig.ThrottleState.TryCount = 1;
        europaTicketConfig.ThrottleState.LastResetTimeBeforeNow = 111111;
        europaTicketConfig.TicketsEnabled = _configuration.GetDefaultValue("Support.TicketsEnabled", false);
        europaTicketConfig.BugsEnabled = _configuration.GetDefaultValue("Support.BugsEnabled", false);
        europaTicketConfig.ComplaintsEnabled = _configuration.GetDefaultValue("Support.ComplaintsEnabled", false);
        europaTicketConfig.SuggestionsEnabled = _configuration.GetDefaultValue("Support.SuggestionsEnabled", false);

        features.EuropaTicketSystemStatus = europaTicketConfig;

        SendPacket(features);
    }

    public void DoLootRelease(LootManagement.Loot loot)
    {
        var lguid = loot.GetOwnerGuid();
        var player = Player;

        if (player.GetLootGUID() == lguid)
            player.SetLootGUID(ObjectGuid.Empty);

        //Player is not looking at loot list, he doesn't need to see updates on the loot list
        loot.RemoveLooter(player.GUID);
        player.SendLootRelease(lguid);
        player.GetAELootView().Remove(loot.GetGuid());

        if (player.GetAELootView().Empty())
            player.RemoveUnitFlag(UnitFlags.Looting);

        if (!player.Location.IsInWorld)
            return;

        if (lguid.IsGameObject)
        {
            var go = player.Location.Map.GetGameObject(lguid);

            // not check distance for GO in case owned GO (fishing bobber case, for example) or Fishing hole GO
            if (!go || ((go.OwnerGUID != player.GUID && go.GoType != GameObjectTypes.FishingHole) && !go.IsWithinDistInMap(player)))
                return;

            if (loot.IsLooted() || go.GoType == GameObjectTypes.FishingNode || go.GoType == GameObjectTypes.FishingHole)
            {
                if (go.GoType == GameObjectTypes.FishingNode)
                {
                    go.SetLootState(LootState.JustDeactivated);
                }
                else if (go.GoType == GameObjectTypes.FishingHole)
                {
                    // The fishing hole used once more
                    go.AddUse(); // if the max usage is reached, will be despawned in next tick

                    if (go.UseCount >= go.GoValue.FishingHole.MaxOpens)
                        go.SetLootState(LootState.JustDeactivated);
                    else
                        go.SetLootState(LootState.Ready);
                }
                else if (go.GoType != GameObjectTypes.GatheringNode && go.IsFullyLooted)
                {
                    go.SetLootState(LootState.JustDeactivated);
                }

                go.OnLootRelease(player);
            }
            else
            {
                // not fully looted object
                go.SetLootState(LootState.Activated, player);
            }
        }
        else if (lguid.IsCorpse) // ONLY remove insignia at BG
        {
            var corpse = ObjectAccessor.GetCorpse(player, lguid);

            if (!corpse || !corpse.Location.IsWithinDistInMap(player, SharedConst.InteractionDistance))
                return;

            if (loot.IsLooted())
            {
                corpse.Loot = null;
                corpse.RemoveCorpseDynamicFlag(CorpseDynFlags.Lootable);
            }
        }
        else if (lguid.IsItem)
        {
            var pItem = player.GetItemByGuid(lguid);

            if (!pItem)
                return;

            var proto = pItem.Template;

            // destroy only 5 items from stack in case prospecting and milling
            if (loot.LootType == LootType.Prospecting || loot.LootType == LootType.Milling)
            {
                pItem.LootGenerated = false;
                pItem.Loot = null;

                var count = pItem.Count;

                // >=5 checked in spell code, but will work for cheating cases also with removing from another stacks.
                if (count > 5)
                    count = 5;

                player.DestroyItemCount(pItem, ref count, true);
            }
            else
            {
                // Only delete item if no loot or money (unlooted loot is saved to db) or if it isn't an openable item
                if (loot.IsLooted() || !proto.HasFlag(ItemFlags.HasLoot))
                    player.DestroyItem(pItem.BagSlot, pItem.Slot, true);
            }
        }
        else
        {
            var creature = player.Location.Map.GetCreature(lguid);

            if (creature == null)
                return;

            if (loot.IsLooted())
            {
                if (creature.IsFullyLooted)
                {
                    creature.RemoveDynamicFlag(UnitDynFlags.Lootable);

                    // skip pickpocketing loot for speed, skinning timer reduction is no-op in fact
                    if (!creature.IsAlive)
                        creature.AllLootRemovedFromCorpse();
                }
            }
            else
            {
                // if the round robin player release, reset it.
                if (player.GUID == loot.RoundRobinPlayer)
                {
                    loot.RoundRobinPlayer.Clear();
                    loot.NotifyLootList(creature.Location.Map);
                }
            }

            // force dynflag update to update looter and lootable info
            creature.Values.ModifyValue(creature.ObjectData).ModifyValue(creature.ObjectData.DynamicFlags);
            creature.ForceUpdateFieldChange();
        }
    }

    public void DoLootReleaseAll()
    {
        var lootView = Player.GetAELootView();

        foreach (var (_, loot) in lootView)
            DoLootRelease(loot);
    }

    private void ProcessQueue(WorldPacket packet)
    {
        var handler = _packetManager.GetHandler((ClientOpcodes)packet.GetOpcode());

        if (handler != null)
        {
            if (handler.ProcessingPlace == PacketProcessing.Inplace)
            {
                _inPlaceQueue.Enqueue(packet);
                _asyncMessageQueueSemaphore.Set();
            }
            else if (handler.ProcessingPlace == PacketProcessing.ThreadSafe)
            {
                _threadSafeQueue.Enqueue(packet);
            }
            else
            {
                _threadUnsafe.Enqueue(packet);
            }
        }
    }

    private void ProcessInPlace()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            _asyncMessageQueueSemaphore.WaitOne(500);
            DrainQueue(_inPlaceQueue);
        }
    }

    private long DrainQueue(ConcurrentQueue<WorldPacket> queue)
    {
        // Before we process anything:
        // If necessary, kick the player because the client didn't send anything for too long
        // (or they've been idling in character select)
        if (IsConnectionIdle && !HasPermission(RBACPermissions.IgnoreIdleConnection))
            Socket?.CloseSocket();

        WorldPacket firstDelayedPacket = null;
        uint processedPackets = 0;
        var currentTime = GameTime.GetGameTime();

        //Check for any packets they was not recived yet.
        while (Socket != null && !queue.IsEmpty && (queue.TryPeek(out var packet) && packet != firstDelayedPacket) && queue.TryDequeue(out packet))
        {
            try
            {
                var handler = _packetManager.GetHandler((ClientOpcodes)packet.GetOpcode());

                switch (handler.SessionStatus)
                {
                    case SessionStatus.Loggedin:
                        if (!Player)
                        {
                            if (!PlayerRecentlyLoggedOut)
                            {
                                if (firstDelayedPacket == null)
                                    firstDelayedPacket = packet;

                                QueuePacket(packet);
                                Log.Logger.Debug("Re-enqueueing packet with opcode {0} with with status OpcodeStatus.Loggedin. Player is currently not in world yet.", (ClientOpcodes)packet.GetOpcode());
                            }

                            break;
                        }

                        if (Player.Location.IsInWorld && _antiDos.EvaluateOpcode(packet, currentTime))
                            handler.Invoke(this, packet);

                        break;
                    case SessionStatus.LoggedinOrRecentlyLogout:
                        if (!Player && !PlayerRecentlyLoggedOut && !PlayerLogout)
                            LogUnexpectedOpcode(packet, handler.SessionStatus, "the player has not logged in yet and not recently logout");
                        else if (_antiDos.EvaluateOpcode(packet, currentTime))
                            handler.Invoke(this, packet);

                        break;
                    case SessionStatus.Transfer:
                        if (!Player)
                            LogUnexpectedOpcode(packet, handler.SessionStatus, "the player has not logged in yet");
                        else if (Player.Location.IsInWorld)
                            LogUnexpectedOpcode(packet, handler.SessionStatus, "the player is still in world");
                        else if (_antiDos.EvaluateOpcode(packet, currentTime))
                            handler.Invoke(this, packet);

                        break;
                    case SessionStatus.Authed:
                        // prevent cheating with skip queue wait
                        if (_inQueue)
                        {
                            LogUnexpectedOpcode(packet, handler.SessionStatus, "the player not pass queue yet");

                            break;
                        }

                        if ((ClientOpcodes)packet.GetOpcode() == ClientOpcodes.EnumCharacters)
                            PlayerRecentlyLoggedOut = false;

                        if (_antiDos.EvaluateOpcode(packet, currentTime))
                            handler.Invoke(this, packet);

                        break;
                    default:
                        Log.Logger.Error("Received not handled opcode {0} from {1}", (ClientOpcodes)packet.GetOpcode(), GetPlayerInfo());

                        break;
                }
            }
            catch (InternalBufferOverflowException ex)
            {
                Log.Logger.Error("InternalBufferOverflowException: {0} while parsing {1} from {2}.", ex.Message, (ClientOpcodes)packet.GetOpcode(), GetPlayerInfo());
            }
            catch (EndOfStreamException)
            {
                Log.Logger.Error("WorldSession:Update EndOfStreamException occured while parsing a packet (opcode: {0}) from client {1}, accountid={2}. Skipped packet.",
                                 (ClientOpcodes)packet.GetOpcode(),
                                 RemoteAddress,
                                 AccountId);
            }

            processedPackets++;

            if (processedPackets > 100)
                break;
        }

        return currentTime;
    }

    private void LogUnexpectedOpcode(WorldPacket packet, SessionStatus status, string reason)
    {
        Log.Logger.Error("Received unexpected opcode {0} Status: {1} Reason: {2} from {3}", (ClientOpcodes)packet.GetOpcode(), status, reason, GetPlayerInfo());
    }

    private void LoadAccountData(SQLResult result, AccountDataTypes mask)
    {
        for (var i = 0; i < (int)AccountDataTypes.Max; ++i)
            if (Convert.ToBoolean((int)mask & (1 << i)))
                _accountData[i] = new AccountData();

        if (result.IsEmpty())
            return;

        do
        {
            int type = result.Read<byte>(0);

            if (type >= (int)AccountDataTypes.Max)
            {
                Log.Logger.Error("Table `{0}` have invalid account data type ({1}), ignore.",
                                 mask == AccountDataTypes.GlobalCacheMask ? "account_data" : "character_account_data",
                                 type);

                continue;
            }

            if (((int)mask & (1 << type)) == 0)
            {
                Log.Logger.Error("Table `{0}` have non appropriate for table  account data type ({1}), ignore.",
                                 mask == AccountDataTypes.GlobalCacheMask ? "account_data" : "character_account_data",
                                 type);

                continue;
            }

            _accountData[type].Time = result.Read<long>(1);
            var bytes = result.Read<byte[]>(2);
            var line = Encoding.Default.GetString(bytes);
            _accountData[type].Data = line;
        } while (result.NextRow());
    }


    private void SetLogoutStartTime(long requestTime)
    {
        _logoutTime = requestTime;
    }

    private bool ShouldLogOut(long currTime)
    {
        return (_logoutTime > 0 && currTime >= _logoutTime + 20);
    }

    private void ProcessQueryCallbacks()
    {
        _queryProcessor.ProcessReadyCallbacks();
        _transactionCallbacks.ProcessReadyCallbacks();
        _queryHolderProcessor.ProcessReadyCallbacks();
    }

    private void InitializeSessionCallback(SQLQueryHolder<AccountInfoQueryLoad> holder, SQLQueryHolder<AccountInfoQueryLoad> realmHolder)
    {
        LoadAccountData(realmHolder.GetResult(AccountInfoQueryLoad.GlobalAccountDataIndexPerRealm), AccountDataTypes.GlobalCacheMask);
        LoadTutorialsData(realmHolder.GetResult(AccountInfoQueryLoad.TutorialsIndexPerRealm));
        CollectionMgr.LoadAccountToys(holder.GetResult(AccountInfoQueryLoad.GlobalAccountToys));
        CollectionMgr.LoadAccountHeirlooms(holder.GetResult(AccountInfoQueryLoad.GlobalAccountHeirlooms));
        CollectionMgr.LoadAccountMounts(holder.GetResult(AccountInfoQueryLoad.Mounts));
        CollectionMgr.LoadAccountItemAppearances(holder.GetResult(AccountInfoQueryLoad.ItemAppearances), holder.GetResult(AccountInfoQueryLoad.ItemFavoriteAppearances));
        CollectionMgr.LoadAccountTransmogIllusions(holder.GetResult(AccountInfoQueryLoad.TransmogIllusions));

        if (!_inQueue)
            SendAuthResponse(BattlenetRpcErrorCode.Ok, false);
        else
            SendAuthWaitQueue(0);

        SetInQueue(false);
        ResetTimeOutTime(false);

        SendSetTimeZoneInformation();
        SendFeatureSystemStatusGlueScreen();
        SendClientCacheVersion(_configuration.GetDefaultValue("ClientCacheVersion", 0u));
        SendAvailableHotfixes();
        SendAccountDataTimes(ObjectGuid.Empty, AccountDataTypes.GlobalCacheMask);
        SendTutorialsData();

        var result = holder.GetResult(AccountInfoQueryLoad.GlobalRealmCharacterCounts);

        if (!result.IsEmpty())
            do
            {
                RealmCharacterCounts[new RealmId(result.Read<byte>(3), result.Read<byte>(4), result.Read<uint>(2)).GetAddress()] = result.Read<byte>(1);
            } while (result.NextRow());

        ConnectionStatus bnetConnected = new()
        {
            State = 1
        };

        SendPacket(bnetConnected);

        BattlePetMgr.LoadFromDB(holder.GetResult(AccountInfoQueryLoad.BattlePets), holder.GetResult(AccountInfoQueryLoad.BattlePetSlot));
    }

    private AccountData GetAccountData(AccountDataTypes type)
    {
        return _accountData[(int)type];
    }

    private void SendAvailableHotfixes()
    {
        SendPacket(new AvailableHotfixes(_realm.Id.GetAddress(), _db2Manager.GetHotfixData()));
    }


    void HandleMoveWorldportAck()
    {
        var player = Player;

        // ignore unexpected far teleports
        if (!player.IsBeingTeleportedFar)
            return;

        var seamlessTeleport = player.IsBeingTeleportedSeamlessly;
        player.SetSemaphoreTeleportFar(false);

        // get the teleport destination
        var loc = player.TeleportDest;

        // possible errors in the coordinate validity check
        if (!GridDefines.IsValidMapCoord(loc))
        {
            LogoutPlayer(false);

            return;
        }

        // get the destination map entry, not the current one, this will fix homebind and reset greeting
        var mapEntry = _cliDB.MapStorage.LookupByKey(loc.MapId);

        // reset instance validity, except if going to an instance inside an instance
        if (!player.InstanceValid && !mapEntry.IsDungeon())
            player.InstanceValid = true;

        var oldMap = player.Location.Map;
        var newMap = Player.TeleportDestInstanceId.HasValue ? _mapManager.FindMap(loc.MapId, Player.TeleportDestInstanceId.Value) : _mapManager.CreateMap(loc.MapId, Player);

        var transportInfo = player.MovementInfo.Transport;
        var transport = player.Transport;

        if (transport != null)
            transport.RemovePassenger(player);

        if (player.Location.IsInWorld)
        {
            Log.Logger.Error($"Player (Name {player.GetName()}) is still in world when teleported from map {oldMap.Id} to new map {loc.MapId}");
            oldMap.RemovePlayerFromMap(player, false);
        }

        // relocate the player to the teleport destination
        // the CannotEnter checks are done in TeleporTo but conditions may change
        // while the player is in transit, for example the map may get full
        if (newMap == null || newMap.CannotEnter(player) != null)
        {
            Log.Logger.Error($"Map {loc.MapId} could not be created for {(newMap != null ? newMap.MapName : "Unknown")} ({player.GUID}), porting player to homebind");
            player.TeleportTo(player.Homebind);

            return;
        }

        var z = loc.Z + player.HoverOffset;
        player.Location.Relocate(loc.X, loc.Y, z, loc.Orientation);
        player.SetFallInformation(0, player.Location.Z);

        player.ResetMap();
        player.Location.Map = newMap;

        ResumeToken resumeToken = new();
        resumeToken.SequenceIndex = player.MovementCounter;
        resumeToken.Reason = seamlessTeleport ? 2 : 1u;
        SendPacket(resumeToken);

        if (!seamlessTeleport)
            player.SendInitialPacketsBeforeAddToMap();

        // move player between transport copies on each map
        var newTransport = newMap.GetTransport(transportInfo.Guid);

        if (newTransport != null)
        {
            player.MovementInfo.Transport = transportInfo;
            newTransport.AddPassenger(player);
        }

        if (!player.Location.Map.AddPlayerToMap(player, !seamlessTeleport))
        {
            Log.Logger.Error($"WORLD: failed to teleport player {player.GetName()} ({player.GUID}) to map {loc.MapId} ({(newMap ? newMap.MapName : "Unknown")}) because of unknown reason!");
            player.ResetMap();
            player.Location.Map = oldMap;
            player.TeleportTo(player.Homebind);

            return;
        }

        // Battleground state prepare (in case join to BG), at relogin/tele player not invited
        // only add to bg group and object, if the player was invited (else he entered through command)
        if (player.InBattleground)
        {
            // cleanup setting if outdated
            if (!mapEntry.IsBattlegroundOrArena())
            {
                // We're not in BG
                player.SetBattlegroundId(0, BattlegroundTypeId.None);
                // reset destination bg team
                player.SetBgTeam(0);
            }
            // join to bg case
            else
            {
                var bg = player.Battleground;

                if (bg)
                    if (player.IsInvitedForBattlegroundInstance(player.BattlegroundId))
                        bg.AddPlayer(player);
            }
        }

        if (!seamlessTeleport)
        {
            player.SendInitialPacketsAfterAddToMap();
        }
        else
        {
            player.UpdateVisibilityForPlayer();
            var garrison = player.Garrison;

            if (garrison != null)
                garrison.SendRemoteInfo();
        }

        // flight fast teleport case
        if (player.IsInFlight)
        {
            if (!player.InBattleground)
            {
                if (!seamlessTeleport)
                {
                    // short preparations to continue flight
                    var movementGenerator = player.MotionMaster.GetCurrentMovementGenerator();
                    movementGenerator.Initialize(player);
                }

                return;
            }

            // Battlegroundstate prepare, stop flight
            player.FinishTaxiFlight();
        }

        if (!player.IsAlive && player.TeleportOptions.HasAnyFlag(TeleportToOptions.ReviveAtTeleport))
            player.ResurrectPlayer(0.5f);

        // resurrect character at enter into instance where his corpse exist after add to map
        if (mapEntry.IsDungeon() && !player.IsAlive)
            if (player.CorpseLocation.MapId == mapEntry.Id)
            {
                player.ResurrectPlayer(0.5f);
                player.SpawnCorpseBones();
            }

        if (mapEntry.IsDungeon())
        {
            // check if this instance has a reset time and send it to player if so
            MapDb2Entries entries = new(mapEntry.Id, newMap.DifficultyID);

            if (entries.MapDifficulty.HasResetSchedule())
            {
                RaidInstanceMessage raidInstanceMessage = new();
                raidInstanceMessage.Type = InstanceResetWarningType.Welcome;
                raidInstanceMessage.MapID = mapEntry.Id;
                raidInstanceMessage.DifficultyID = newMap.DifficultyID;

                var playerLock = _instanceLockManager.FindActiveInstanceLock(Player.GUID, entries);

                if (playerLock != null)
                {
                    raidInstanceMessage.Locked = !playerLock.IsExpired();
                    raidInstanceMessage.Extended = playerLock.IsExtended();
                }
                else
                {
                    raidInstanceMessage.Locked = false;
                    raidInstanceMessage.Extended = false;
                }

                SendPacket(raidInstanceMessage);
            }

            // check if instance is valid
            if (!player.CheckInstanceValidity(false))
                player.InstanceValid = false;
        }

        // update zone immediately, otherwise leave channel will cause crash in mtmap
        player.GetZoneAndAreaId(out var newzone, out var newarea);
        player.UpdateZone(newzone, newarea);

        // honorless target
        if (player.PvpInfo.IsHostile)
            player.SpellFactory.CastSpell(player, 2479, true);

        // in friendly area
        else if (player.IsPvP && !player.HasPlayerFlag(PlayerFlags.InPVP))
            player.UpdatePvP(false);

        // resummon pet
        player.ResummonPetTemporaryUnSummonedIfAny();

        //lets process all delayed operations on successful teleport
        player.ProcessDelayedOperations();
    }
}