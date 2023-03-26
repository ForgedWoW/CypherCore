// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Forged.MapServer.Accounts;
using Forged.MapServer.Battlepay;
using Forged.MapServer.BattlePets;
using Forged.MapServer.Chat;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Authentication;
using Forged.MapServer.Networking.Packets.Battlenet;
using Forged.MapServer.Networking.Packets.Character;
using Forged.MapServer.Networking.Packets.Chat;
using Forged.MapServer.Networking.Packets.ClientConfig;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Warden;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Warden;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Realm;
using Serilog;

namespace Forged.MapServer.Server;

public class WorldSession : IDisposable
{
	public long MuteTime;

    private readonly List<ObjectGuid> _legitCharacters = new();
    private readonly WorldSocket[] _socket = new WorldSocket[(int)ConnectionType.Max];
    private readonly string _address;
    private readonly uint _accountId;
    private readonly string _accountName;
    private readonly uint _battlenetAccountId;
    private readonly Expansion _accountExpansion;
    private readonly Expansion _expansion;
    private readonly Expansion _configuredExpansion;
    private readonly string _os;

    private readonly DosProtection _antiDos;
    private readonly Locale _sessionDbcLocale;
    private readonly Locale _sessionDbLocaleIndex;
    private readonly AccountData[] _accountData = new AccountData[(int)AccountDataTypes.Max];
    private readonly uint[] _tutorials = new uint[SharedConst.MaxAccountTutorialValues];
    private readonly Dictionary<uint /*realmAddress*/, byte> _realmCharacterCounts = new();
    private readonly Dictionary<uint, Action<Google.Protobuf.CodedInputStream>> _battlenetResponseCallbacks = new();

    private readonly List<string> _registeredAddonPrefixes = new();
    private readonly uint _recruiterId;
    private readonly bool _isRecruiter;

	private readonly ActionBlock<WorldPacket> _recvQueue;

    private readonly ConcurrentQueue<WorldPacket> _threadUnsafe = new();
    private readonly ConcurrentQueue<WorldPacket> _inPlaceQueue = new();
    private readonly ConcurrentQueue<WorldPacket> _threadSafeQueue = new();

    private readonly CircularBuffer<Tuple<long, uint>> _timeSyncClockDeltaQueue = new(6); // first member: clockDelta. Second member: latency of the packet exchange that was used to compute that clockDelta.

    private readonly Dictionary<uint, uint> _pendingTimeSyncRequests = new(); // key: counter. value: server time when packet with that counter was sent.

    private readonly CollectionMgr _collectionMgr;

    private readonly BattlePetMgr _battlePetMgr;
	private readonly BattlepayManager _battlePayMgr;

    private readonly AsyncCallbackProcessor<QueryCallback> _queryProcessor = new();
    private readonly AsyncCallbackProcessor<TransactionCallback> _transactionCallbacks = new();
    private readonly AsyncCallbackProcessor<ISqlCallback> _queryHolderProcessor = new();

    private readonly CancellationTokenSource _cancellationToken = new();
    private readonly AutoResetEvent _asyncMessageQueueSemaphore = new(false);
    private ulong _guidLow;
    private Player _player;

    private AccountTypes _security;

    private uint _expireTime;
    private bool _forceExit;
    private Warden.Warden _warden; // Remains NULL if Warden system is not enabled by config

    private long _logoutTime;
    private bool _inQueue;
    private ObjectGuid _playerLoading; // code processed in LoginPlayer
    private bool _playerLogout;        // code processed in LogoutPlayer
    private bool _playerRecentlyLogout;
    private bool _playerSave;
    private uint _latency;
    private TutorialsFlag _tutorialsChanged;

    private Array<byte> _realmListSecret = new(32);
    private uint _battlenetRequestToken;
    private bool _filterAddonMessages;
    private long _timeOutTime;

    private RBACData _rbacData;
    private long _timeSyncClockDelta;
    private uint _timeSyncNextCounter;
    private uint _timeSyncTimer;

    private ConnectToKey _instanceConnectKey;

	// Packets cooldown
    private long _calendarEventCreationCooldown;

	public bool CanSpeak => MuteTime <= GameTime.GetGameTime();

	public string PlayerName => _player != null ? _player.GetName() : "Unknown";

	public bool PlayerLoading => !_playerLoading.IsEmpty;
	public bool PlayerLogout => _playerLogout;
	public bool PlayerLogoutWithSave => _playerLogout && _playerSave;
	public bool PlayerRecentlyLoggedOut => _playerRecentlyLogout;

	public bool PlayerDisconnected => !(_socket[(int)ConnectionType.Realm] != null &&
										_socket[(int)ConnectionType.Realm].IsOpen() &&
										_socket[(int)ConnectionType.Instance] != null &&
										_socket[(int)ConnectionType.Instance].IsOpen());

	public AccountTypes Security
	{
		get => _security;
		private set => _security = value;
	}

	public uint AccountId => _accountId;
	public ObjectGuid AccountGUID => ObjectGuid.Create(HighGuid.WowAccount, AccountId);
	public string AccountName => _accountName;
	public uint BattlenetAccountId => _battlenetAccountId;
	public ObjectGuid BattlenetAccountGUID => ObjectGuid.Create(HighGuid.BNetAccount, BattlenetAccountId);

	public Player Player
	{
		get => _player;
		set
		{
			_player = value;

			if (_player)
				_guidLow = _player.GUID.Counter;
		}
	}

	public string RemoteAddress => _address;
	public Expansion AccountExpansion => _accountExpansion;
	public Expansion Expansion => _expansion;
	public string OS => _os;

	public bool IsLogingOut => _logoutTime != 0 || _playerLogout;
	public ulong ConnectToInstanceKey => _instanceConnectKey.Raw;
	public AsyncCallbackProcessor<QueryCallback> QueryProcessor => _queryProcessor;

	public RBACData RBACData => _rbacData;

	public Locale SessionDbcLocale => _sessionDbcLocale;
	public Locale SessionDbLocaleIndex => _sessionDbLocaleIndex;

	public uint Latency
	{
		get => _latency;
		set => _latency = value;
	}

    private bool IsConnectionIdle => _timeOutTime < GameTime.GetGameTime() && !_inQueue;

	public uint RecruiterId => _recruiterId;

	public bool IsARecruiter => _isRecruiter;

	// Packets cooldown
	public long CalendarEventCreationCooldown
	{
		get => _calendarEventCreationCooldown;
		set => _calendarEventCreationCooldown = value;
	}

	// Battle Pets
	public BattlePetMgr BattlePetMgr => _battlePetMgr;

	public CollectionMgr CollectionMgr => _collectionMgr;

	// Battlenet
	public Array<byte> RealmListSecret
	{
		get => _realmListSecret;
		private set => _realmListSecret = value;
	}

	public Dictionary<uint, byte> RealmCharacterCounts => _realmCharacterCounts;

	public CommandHandler CommandHandler { get; private set; }

	public BattlepayManager BattlePayMgr => _battlePayMgr;

	public WorldSession(uint id, string name, uint battlenetAccountId, WorldSocket sock, AccountTypes sec, Expansion expansion, long mute_time, string os, Locale locale, uint recruiter, bool isARecruiter)
	{
		MuteTime = mute_time;
		_antiDos = new DosProtection(this);
		_socket[(int)ConnectionType.Realm] = sock;
		_security = sec;
		_accountId = id;
		_accountName = name;
		_battlenetAccountId = battlenetAccountId;
		_configuredExpansion = ConfigMgr.GetDefaultValue<int>("Player.OverrideExpansion", -1) == -1 ? Expansion.LevelCurrent : (Expansion)ConfigMgr.GetDefaultValue<int>("Player.OverrideExpansion", -1);
		_accountExpansion = Expansion.LevelCurrent == _configuredExpansion ? expansion : _configuredExpansion;
		_expansion = (Expansion)Math.Min((byte)expansion, GetDefaultValue("Expansion", (int)Expansion.Dragonflight));
		_os = os;
		_sessionDbcLocale = Global.WorldMgr.GetAvailableDbcLocale(locale);
		_sessionDbLocaleIndex = locale;
		_recruiterId = recruiter;
		_isRecruiter = isARecruiter;
		_expireTime = 60000; // 1 min after socket loss, session is deleted
		_battlePetMgr = new BattlePetMgr(this);
		_collectionMgr = new CollectionMgr(this);
		_battlePayMgr = new BattlepayManager(this);
		CommandHandler = new CommandHandler(this);

		_recvQueue = new ActionBlock<WorldPacket>(ProcessQueue,
												new ExecutionDataflowBlockOptions()
												{
													MaxDegreeOfParallelism = 10,
													EnsureOrdered = true,
													CancellationToken = _cancellationToken.Token
												});

		Task.Run(ProcessInPlace, _cancellationToken.Token);

		_address = sock.GetRemoteIpAddress().Address.ToString();
		ResetTimeOutTime(false);
		DB.Login.Execute("UPDATE account SET online = 1 WHERE id = {0};", AccountId); // One-time query
	}

	public void Dispose()
	{
		_cancellationToken.Cancel();

		// unload player if not unloaded
		if (_player)
			LogoutPlayer(true);

		// - If have unclosed socket, close it
		for (byte i = 0; i < 2; ++i)
			if (_socket[i] != null)
			{
				_socket[i].CloseSocket();
				_socket[i] = null;
			}

		// empty incoming packet queue
		_recvQueue.Complete();

		DB.Login.Execute("UPDATE account SET online = 0 WHERE id = {0};", AccountId); // One-time query
	}

	public void LogoutPlayer(bool save)
	{
		if (_playerLogout)
			return;

		// finish pending transfers before starting the logout
		while (_player && _player.IsBeingTeleportedFar)
			HandleMoveWorldportAck();

		_playerLogout = true;
		_playerSave = save;

		if (_player)
		{
			if (!_player.GetLootGUID().IsEmpty)
				DoLootReleaseAll();

			// If the player just died before logging out, make him appear as a ghost
			//FIXME: logout must be delayed in case lost connection with client in time of combat
			if (Player.DeathTimer != 0)
			{
				_player.CombatStop();
				_player.BuildPlayerRepop();
				_player.RepopAtGraveyard();
			}
			else if (Player.HasAuraType(AuraType.SpiritOfRedemption))
			{
				// this will kill character by SPELL_AURA_SPIRIT_OF_REDEMPTION
				_player.RemoveAurasByType(AuraType.ModShapeshift);
				_player.KillPlayer();
				_player.BuildPlayerRepop();
				_player.RepopAtGraveyard();
			}
			else if (Player.HasPendingBind)
			{
				_player.RepopAtGraveyard();
				_player.SetPendingBind(0, 0);
			}

			//drop a flag if player is carrying it
			var bg = Player.Battleground;

			if (bg)
				bg.EventPlayerLoggedOut(Player);

			// Teleport to home if the player is in an invalid instance
			if (!_player.InstanceValid && !_player.IsGameMaster)
				_player.TeleportTo(_player.Homebind);

			Global.OutdoorPvPMgr.HandlePlayerLeaveZone(_player, _player.Zone);

			for (uint i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
			{
				var bgQueueTypeId = _player.GetBattlegroundQueueTypeId(i);

				if (bgQueueTypeId != default)
				{
					_player.RemoveBattlegroundQueueId(bgQueueTypeId);
					var queue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);
					queue.RemovePlayer(_player.GUID, true);
				}
			}

			// Repop at GraveYard or other player far teleport will prevent saving player because of not present map
			// Teleport player immediately for correct player save
			while (_player.IsBeingTeleportedFar)
				HandleMoveWorldportAck();

			// If the player is in a guild, update the guild roster and broadcast a logout message to other guild members
			var guild = Global.GuildMgr.GetGuildById(_player.GuildId);

			if (guild)
				guild.HandleMemberLogout(this);

			// Remove pet
			_player.RemovePet(null, PetSaveMode.AsCurrent, true);

			///- Release battle pet journal lock
			if (_battlePetMgr.HasJournalLock)
				_battlePetMgr.ToggleJournalLock(false);

			// Clear whisper whitelist
			_player.ClearWhisperWhiteList();

			// empty buyback items and save the player in the database
			// some save parts only correctly work in case player present in map/player_lists (pets, etc)
			if (save)
			{
				for (uint j = InventorySlots.BuyBackStart; j < InventorySlots.BuyBackEnd; ++j)
				{
					var eslot = j - InventorySlots.BuyBackStart;
					_player.SetInvSlot(j, ObjectGuid.Empty);
					_player.SetBuybackPrice(eslot, 0);
					_player.SetBuybackTimestamp(eslot, 0);
				}

				_player.SaveToDB();
			}

			// Leave all channels before player delete...
			_player.CleanupChannels();

			// If the player is in a group (or invited), remove him. If the group if then only 1 person, disband the group.
			_player.UninviteFromGroup();

			//! Send update to group and reset stored max enchanting level
			var group = _player.Group;

			if (group != null)
			{
				group.SendUpdate();

				if (group.LeaderGUID == _player.GUID)
					group.StartLeaderOfflineTimer();
			}

			//! Broadcast a logout message to the player's friends
			Global.SocialMgr.SendFriendStatus(_player, FriendsResult.Offline, _player.GUID, true);
			_player.RemoveSocial();

			//! Call script hook before deletion
			Global.ScriptMgr.ForEach<IPlayerOnLogout>(p => p.OnLogout(_player));

			//! Remove the player from the world
			// the player may not be in the world when logging out
			// e.g if he got disconnected during a transfer to another map
			// calls to GetMap in this case may cause crashes
			_player.SetDestroyedObject(true);
			_player.CleanupsBeforeDelete();
			Log.Logger.Information($"Account: {AccountId} (IP: {RemoteAddress}) Logout Character:[{_player.GetName()}] ({_player.GUID}) Level: {_player.Level}, XP: {_player.XP}/{_player.XPForNextLevel} ({_player.XPForNextLevel - _player.XP} left)");

			var map = Player.Map;

			if (map != null)
				map.RemovePlayerFromMap(Player, true);

			Player = null;

			//! Send the 'logout complete' packet to the client
			//! Client will respond by sending 3x CMSG_CANCEL_TRADE, which we currently dont handle
			LogoutComplete logoutComplete = new();
			SendPacket(logoutComplete);

			//! Since each account can only have one online character at any given time, ensure all characters for active account are marked as offline
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ACCOUNT_ONLINE);
			stmt.AddValue(0, AccountId);
			DB.Characters.Execute(stmt);
		}

		if (_socket[(int)ConnectionType.Instance] != null)
		{
			_socket[(int)ConnectionType.Instance].CloseSocket();
			_socket[(int)ConnectionType.Instance] = null;
		}

		_playerLogout = false;
		_playerSave = false;
		_playerRecentlyLogout = true;
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


		if (_socket[(int)ConnectionType.Realm] != null && _socket[(int)ConnectionType.Realm].IsOpen() && _warden != null)
			_warden.Update(diff);

		// If necessary, log the player out
		if (ShouldLogOut(currentTime) && _playerLoading.IsEmpty)
			LogoutPlayer(true);

		//- Cleanup socket if need
		if ((_socket[(int)ConnectionType.Realm] != null && !_socket[(int)ConnectionType.Realm].IsOpen()) ||
			(_socket[(int)ConnectionType.Instance] != null && !_socket[(int)ConnectionType.Instance].IsOpen()))
		{
			if (Player != null && _warden != null)
				_warden.Update(diff);

			_expireTime -= _expireTime > diff ? diff : _expireTime;

			if (_expireTime < diff || _forceExit || !Player)
			{
				if (_socket[(int)ConnectionType.Realm] != null)
				{
					_socket[(int)ConnectionType.Realm].CloseSocket();
					_socket[(int)ConnectionType.Realm] = null;
				}

				if (_socket[(int)ConnectionType.Instance] != null)
				{
					_socket[(int)ConnectionType.Instance].CloseSocket();
					_socket[(int)ConnectionType.Instance] = null;
				}
			}
		}

		if (_socket[(int)ConnectionType.Realm] == null)
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

		if (conIdx != ConnectionType.Instance && PacketManager.IsInstanceOnlyOpcode(packet.GetOpcode()))
		{
			Log.Logger.Error("Prevented sending of instance only opcode {0} with connection type {1} to {2}", packet.GetOpcode(), packet.GetConnection(), GetPlayerInfo());

			return;
		}

		if (_socket[(int)conIdx] == null)
		{
			Log.Logger.Verbose("Prevented sending of {0} to non existent socket {1} to {2}", packet.GetOpcode(), conIdx, GetPlayerInfo());

			return;
		}

		_socket[(int)conIdx].SendPacket(packet);
	}

	public void AddInstanceConnection(WorldSocket sock)
	{
		_socket[(int)ConnectionType.Instance] = sock;
	}

	public void KickPlayer(string reason)
	{
		Log.Logger.Information($"Account: {AccountId} Character: '{(_player ? _player.GetName() : "<none>")}' {(_player ? _player.GUID : "")} kicked with reason: {reason}");

		for (byte i = 0; i < 2; ++i)
			if (_socket[i] != null)
			{
				_socket[i].CloseSocket();
				_forceExit = true;
			}
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
		var stmt = DB.Characters.GetPreparedStatement(hasTutorialsInDB ? CharStatements.UPD_TUTORIALS : CharStatements.INS_TUTORIALS);

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
		var instanceAddress = Global.WorldMgr.Realm.GetAddressForClient(System.Net.IPAddress.Parse(RemoteAddress));

		_instanceConnectKey.AccountId = AccountId;
		_instanceConnectKey.connectionType = ConnectionType.Instance;
		_instanceConnectKey.Key = RandomHelper.URand(0, 0x7FFFFFFF);

		ConnectTo connectTo = new()
		{
			Key = _instanceConnectKey.Raw,
			Serial = serial,
			Payload =
			{
				Port = (ushort)GetDefaultValue("InstanceServerPort", 8086)
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

		if (GetDefaultValue("ChatStrictLinkChecking.Kick", 0) != 0)
			KickPlayer("WorldSession::DisallowHyperlinksAndMaybeKick Illegal chat link");

		return false;
	}

	public void SendNotification(CypherStrings str, params object[] args)
	{
		SendNotification(Global.ObjectMgr.GetCypherString(str), args);
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
		else if (_player)
			ss.AppendFormat("{0} {1}, ", _player.GetName(), _player.GUID.ToString());

		ss.AppendFormat("Account: {0}]", AccountId);

		return ss.ToString();
	}

	public void SetInQueue(bool state)
	{
		_inQueue = state;
	}

	public SQLQueryHolderCallback<R> AddQueryHolderCallback<R>(SQLQueryHolderCallback<R> callback)
	{
		return (SQLQueryHolderCallback<R>)_queryHolderProcessor.AddCallback(callback);
	}

	public bool CanAccessAlliedRaces()
	{
		if (ConfigMgr.GetDefaultValue("CharacterCreating.DisableAlliedRaceAchievementRequirement", false))
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
						_accountName,
						Global.WorldMgr.Realm.Id.Index,
						secLevel);

		_rbacData = new RBACData(id, _accountName, (int)Global.WorldMgr.Realm.Id.Index, (byte)secLevel);
		_rbacData.LoadFromDB();
	}

	public QueryCallback LoadPermissionsAsync()
	{
		var id = AccountId;
		var secLevel = Security;

		Log.Logger.Debug("WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
						id,
						_accountName,
						Global.WorldMgr.Realm.Id.Index,
						secLevel);

		_rbacData = new RBACData(id, _accountName, (int)Global.WorldMgr.Realm.Id.Index, (byte)secLevel);

		return _rbacData.LoadFromDBAsync();
	}

	public void InitializeSession()
	{
		AccountInfoQueryHolderPerRealm realmHolder = new();
		realmHolder.Initialize(AccountId, BattlenetAccountId);

		AccountInfoQueryHolder holder = new();
		holder.Initialize(AccountId, BattlenetAccountId);

		AccountInfoQueryHolderPerRealm characterHolder = null;
		AccountInfoQueryHolder loginHolder = null;

		AddQueryHolderCallback(DB.Characters.DelayQueryHolder(realmHolder))
			.AfterComplete(result =>
			{
				characterHolder = (AccountInfoQueryHolderPerRealm)result;

				if (loginHolder != null && characterHolder != null)
					InitializeSessionCallback(loginHolder, characterHolder);
			});

		AddQueryHolderCallback(DB.Login.DelayQueryHolder(holder))
			.AfterComplete(result =>
			{
				loginHolder = (AccountInfoQueryHolder)result;

				if (loginHolder != null && characterHolder != null)
					InitializeSessionCallback(loginHolder, characterHolder);
			});
	}

	public bool HasPermission(RBACPermissions permission)
	{
		if (_rbacData == null)
			LoadPermissions();

		var hasPermission = _rbacData.HasPermission(permission);

		Log.Logger.Debug("WorldSession:HasPermission [AccountId: {0}, Name: {1}, realmId: {2}]",
						_rbacData.Id,
						_rbacData.Name,
						Global.WorldMgr.Realm.Id.Index);

		return hasPermission;
	}

	public void InvalidateRBACData()
	{
		Log.Logger.Debug("WorldSession:Invalidaterbac:RBACData [AccountId: {0}, Name: {1}, realmId: {2}]",
						_rbacData.Id,
						_rbacData.Name,
						Global.WorldMgr.Realm.Id.Index);

		_rbacData = null;
	}

	public void ResetTimeSync()
	{
		_timeSyncNextCounter = 0;
		_pendingTimeSyncRequests.Clear();
	}

	public void SendTimeSync()
	{
		TimeSyncRequest timeSyncRequest = new()
		{
			SequenceIndex = _timeSyncNextCounter
		};

		SendPacket(timeSyncRequest);

		_pendingTimeSyncRequests[_timeSyncNextCounter] = Time.MSTime;

		// Schedule next sync in 10 sec (except for the 2 first packets, which are spaced by only 5s)
		_timeSyncTimer = _timeSyncNextCounter == 0 ? 5000 : 10000u;
		_timeSyncNextCounter++;
	}

	public void ResetTimeOutTime(bool onlyActive)
	{
		if (Player)
			_timeOutTime = GameTime.GetGameTime() + GetDefaultValue("SocketTimeOutTimeActive", 60);
		else if (!onlyActive)
			_timeOutTime = GameTime.GetGameTime() + GetDefaultValue("SocketTimeOutTime", 900);
	}

	public static implicit operator bool(WorldSession session)
	{
		return session != null;
	}

    private void ProcessQueue(WorldPacket packet)
	{
		var handler = PacketManager.GetHandler((ClientOpcodes)packet.GetOpcode());

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

	private long DrainQueue(ConcurrentQueue<WorldPacket> _queue)
	{
		// Before we process anything:
		/// If necessary, kick the player because the client didn't send anything for too long
		/// (or they've been idling in character select)
		if (IsConnectionIdle && !HasPermission(RBACPermissions.IgnoreIdleConnection))
			_socket[(int)ConnectionType.Realm]?.CloseSocket();

		WorldPacket firstDelayedPacket = null;
		uint processedPackets = 0;
		var currentTime = GameTime.GetGameTime();

		//Check for any packets they was not recived yet.
		while (_socket[(int)ConnectionType.Realm] != null && !_queue.IsEmpty && (_queue.TryPeek(out var packet) && packet != firstDelayedPacket) && _queue.TryDequeue(out packet))
		{
			try
			{
				var handler = PacketManager.GetHandler((ClientOpcodes)packet.GetOpcode());

				switch (handler.sessionStatus)
				{
					case SessionStatus.Loggedin:
						if (!_player)
						{
							if (!_playerRecentlyLogout)
							{
								if (firstDelayedPacket == null)
									firstDelayedPacket = packet;

								QueuePacket(packet);
								Log.Logger.Debug("Re-enqueueing packet with opcode {0} with with status OpcodeStatus.Loggedin. Player is currently not in world yet.", (ClientOpcodes)packet.GetOpcode());
							}

							break;
						}
						else if (_player.IsInWorld && _antiDos.EvaluateOpcode(packet, currentTime))
						{
							handler.Invoke(this, packet);
						}

						break;
					case SessionStatus.LoggedinOrRecentlyLogout:
						if (!_player && !_playerRecentlyLogout && !_playerLogout)
							LogUnexpectedOpcode(packet, handler.sessionStatus, "the player has not logged in yet and not recently logout");
						else if (_antiDos.EvaluateOpcode(packet, currentTime))
							handler.Invoke(this, packet);

						break;
					case SessionStatus.Transfer:
						if (!_player)
							LogUnexpectedOpcode(packet, handler.sessionStatus, "the player has not logged in yet");
						else if (_player.IsInWorld)
							LogUnexpectedOpcode(packet, handler.sessionStatus, "the player is still in world");
						else if (_antiDos.EvaluateOpcode(packet, currentTime))
							handler.Invoke(this, packet);

						break;
					case SessionStatus.Authed:
						// prevent cheating with skip queue wait
						if (_inQueue)
						{
							LogUnexpectedOpcode(packet, handler.sessionStatus, "the player not pass queue yet");

							break;
						}

						if ((ClientOpcodes)packet.GetOpcode() == ClientOpcodes.EnumCharacters)
							_playerRecentlyLogout = false;

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

    private void SetAccountData(AccountDataTypes type, long time, string data)
	{
		if (Convert.ToBoolean((1 << (int)type) & (int)AccountDataTypes.GlobalCacheMask))
		{
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_ACCOUNT_DATA);
			stmt.AddValue(0, AccountId);
			stmt.AddValue(1, (byte)type);
			stmt.AddValue(2, time);
			stmt.AddValue(3, data);
			DB.Characters.Execute(stmt);
		}
		else
		{
			// _player can be NULL and packet received after logout but m_GUID still store correct guid
			if (_guidLow == 0)
				return;

			var stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_PLAYER_ACCOUNT_DATA);
			stmt.AddValue(0, _guidLow);
			stmt.AddValue(1, (byte)type);
			stmt.AddValue(2, time);
			stmt.AddValue(3, data);
			DB.Characters.Execute(stmt);
		}

		_accountData[(int)type].Time = time;
		_accountData[(int)type].Data = data;
	}

    private bool ValidateHyperlinksAndMaybeKick(string str)
	{
		if (Hyperlink.CheckAllLinks(str))
			return true;

		Log.Logger.Error($"Player {Player.GetName()} {Player.GUID} sent a message with an invalid link:\n{str}");

		if (GetDefaultValue("ChatStrictLinkChecking.Kick", 0) != 0)
			KickPlayer("WorldSession::ValidateHyperlinksAndMaybeKick Invalid chat link");

		return false;
	}

    private void HandleWardenData(WardenData packet)
	{
		if (_warden == null || packet.Data.GetSize() == 0)
			return;

		_warden.HandleData(packet.Data);
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

    private TransactionCallback AddTransactionCallback(TransactionCallback callback)
	{
		return _transactionCallbacks.AddCallback(callback);
	}

    private void InitWarden(BigInteger k)
	{
		if (_os == "Win")
		{
			_warden = new WardenWin();
			_warden.Init(this, k);
		}
		else if (_os == "Wn64")
		{
			// Not implemented
		}
		else if (_os == "Mc64")
		{
			// Not implemented
		}
	}

    private void InitializeSessionCallback(SQLQueryHolder<AccountInfoQueryLoad> holder, SQLQueryHolder<AccountInfoQueryLoad> realmHolder)
	{
		LoadAccountData(realmHolder.GetResult(AccountInfoQueryLoad.GlobalAccountDataIndexPerRealm), AccountDataTypes.GlobalCacheMask);
		LoadTutorialsData(realmHolder.GetResult(AccountInfoQueryLoad.TutorialsIndexPerRealm));
		_collectionMgr.LoadAccountToys(holder.GetResult(AccountInfoQueryLoad.GlobalAccountToys));
		_collectionMgr.LoadAccountHeirlooms(holder.GetResult(AccountInfoQueryLoad.GlobalAccountHeirlooms));
		_collectionMgr.LoadAccountMounts(holder.GetResult(AccountInfoQueryLoad.Mounts));
		_collectionMgr.LoadAccountItemAppearances(holder.GetResult(AccountInfoQueryLoad.ItemAppearances), holder.GetResult(AccountInfoQueryLoad.ItemFavoriteAppearances));
		_collectionMgr.LoadAccountTransmogIllusions(holder.GetResult(AccountInfoQueryLoad.TransmogIllusions));

		if (!_inQueue)
			SendAuthResponse(BattlenetRpcErrorCode.Ok, false);
		else
			SendAuthWaitQueue(0);

		SetInQueue(false);
		ResetTimeOutTime(false);

		SendSetTimeZoneInformation();
		SendFeatureSystemStatusGlueScreen();
		SendClientCacheVersion(GetDefaultValue("ClientCacheVersion", 0));
		SendAvailableHotfixes();
		SendAccountDataTimes(ObjectGuid.Empty, AccountDataTypes.GlobalCacheMask);
		SendTutorialsData();

		var result = holder.GetResult(AccountInfoQueryLoad.GlobalRealmCharacterCounts);

		if (!result.IsEmpty())
			do
			{
				_realmCharacterCounts[new RealmId(result.Read<byte>(3), result.Read<byte>(4), result.Read<uint>(2)).GetAddress()] = result.Read<byte>(1);
			} while (result.NextRow());

		ConnectionStatus bnetConnected = new()
		{
			State = 1
		};

		SendPacket(bnetConnected);

		_battlePetMgr.LoadFromDB(holder.GetResult(AccountInfoQueryLoad.BattlePets), holder.GetResult(AccountInfoQueryLoad.BattlePetSlot));
	}

    private AccountData GetAccountData(AccountDataTypes type)
	{
		return _accountData[(int)type];
	}

    private uint GetTutorialInt(byte index)
	{
		return _tutorials[index];
	}

    private void SetTutorialInt(byte index, uint value)
	{
		if (_tutorials[index] != value)
		{
			_tutorials[index] = value;
			_tutorialsChanged |= TutorialsFlag.Changed;
		}
	}

    private uint AdjustClientMovementTime(uint time)
	{
		var movementTime = (long)time + _timeSyncClockDelta;

		if (_timeSyncClockDelta == 0 || movementTime < 0 || movementTime > 0xFFFFFFFF)
		{
			Log.Logger.Warning("The computed movement time using clockDelta is erronous. Using fallback instead");

			return GameTime.GetGameTimeMS();
		}
		else
		{
			return (uint)movementTime;
		}
	}
}