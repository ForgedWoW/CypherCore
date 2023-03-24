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
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Realm;
using Game.Common.Accounts;
using Game.Common.Battlepay;
using Game.Common.Chat;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Players;
using Game.Common.Extentions;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Authentication;
using Game.Common.Networking.Packets.Battlenet;
using Game.Common.Networking.Packets.Character;
using Game.Common.Networking.Packets.Chat;
using Game.Common.Networking.Packets.ClientConfig;
using Game.Common.Networking.Packets.Misc;
using Game.Common.Networking.Packets.Warden;
using Game.Common.Warden;
using Game.Common.World;
using Microsoft.Extensions.Configuration;

namespace Game.Common.Server;

public class WorldSession : IDisposable
{
	public long MuteTime;

    private readonly WorldSocket _socket;
	readonly string _address;
	readonly uint _accountId;
	readonly string _accountName;
	readonly uint _battlenetAccountId;
	readonly Expansion _accountExpansion;
	readonly Expansion _expansion;
	readonly Expansion _configuredExpansion;
	readonly string _os;

	readonly DosProtection _antiDos;
	readonly Locale _sessionDbcLocale;
	readonly Locale _sessionDbLocaleIndex;
	readonly AccountData[] _accountData = new AccountData[(int)AccountDataTypes.Max];
	readonly uint[] _tutorials = new uint[SharedConst.MaxAccountTutorialValues];
	readonly Dictionary<uint /*realmAddress*/, byte> _realmCharacterCounts = new();
	readonly Dictionary<uint, Action<Google.Protobuf.CodedInputStream>> _battlenetResponseCallbacks = new();

    public List<string> RegisteredAddonPrefixes { get; } = new();
    readonly uint _recruiterId;
	readonly bool _isRecruiter;
    private readonly IConfiguration _configuration;
    private readonly WorldManager _worldManager;

    private readonly ActionBlock<WorldPacket> _recvQueue;

	readonly ConcurrentQueue<WorldPacket> _threadUnsafe = new();
	readonly ConcurrentQueue<WorldPacket> _inPlaceQueue = new();
	readonly ConcurrentQueue<WorldPacket> _threadSafeQueue = new();

	readonly CircularBuffer<Tuple<long, uint>> _timeSyncClockDeltaQueue = new(6); // first member: clockDelta. Second member: latency of the packet exchange that was used to compute that clockDelta.

	readonly Dictionary<uint, uint> _pendingTimeSyncRequests = new(); // key: counter. value: server time when packet with that counter was sent.

	private readonly BattlepayManager _battlePayMgr;

	readonly AsyncCallbackProcessor<QueryCallback> _queryProcessor = new();
	readonly AsyncCallbackProcessor<TransactionCallback> _transactionCallbacks = new();
	readonly AsyncCallbackProcessor<ISqlCallback> _queryHolderProcessor = new();

	readonly CancellationTokenSource _cancellationToken = new();
	readonly AutoResetEvent _asyncMessageQueueSemaphore = new(false);
    public ulong GuidLow { get; set; }
    Player _player;

	AccountTypes _security;

	uint _expireTime;
	bool _forceExit;
    public Warden.Warden GameWarden { get; set; }

    long _logoutTime;
	bool _inQueue;
    public ObjectGuid PlayerLoading { get; set; }
    bool _playerLogout;        // code processed in LogoutPlayer
	bool _playerRecentlyLogout;
	bool _playerSave;
	uint _latency;
	TutorialsFlag _tutorialsChanged;

	Array<byte> _realmListSecret = new(32);
	uint _battlenetRequestToken;
	bool _filterAddonMessages;
	long _timeOutTime;

	RBACData _rbacData;
	long _timeSyncClockDelta;
	uint _timeSyncNextCounter;
	uint _timeSyncTimer;

	ConnectToKey _instanceConnectKey;

	// Packets cooldown
	long _calendarEventCreationCooldown;

	public bool CanSpeak => MuteTime <= GameTime.GetGameTime();

	public string PlayerName => _player != null ? _player.GetName() : "Unknown";

	public bool IsPlayerLoading => !PlayerLoading.IsEmpty;
	public bool PlayerLogout => _playerLogout;
	public bool PlayerLogoutWithSave => _playerLogout && _playerSave;
	public bool PlayerRecentlyLoggedOut => _playerRecentlyLogout;

	public bool PlayerDisconnected => !(_socket != null && _socket.IsOpen());

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
				GuidLow = _player.GUID.Counter;
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

	bool IsConnectionIdle => _timeOutTime < GameTime.GetGameTime() && !_inQueue;

	public uint RecruiterId => _recruiterId;

	public bool IsARecruiter => _isRecruiter;

	// Packets cooldown
	public long CalendarEventCreationCooldown
	{
		get => _calendarEventCreationCooldown;
		set => _calendarEventCreationCooldown = value;
	}

	// Battlenet
	public Array<byte> RealmListSecret
	{
		get => _realmListSecret;
		private set => _realmListSecret = value;
	}

	public Dictionary<uint, byte> RealmCharacterCounts => _realmCharacterCounts;

    public WorldSession(uint id, string name, uint battlenetAccountId, WorldSocket sock, AccountTypes sec, Expansion expansion, long mute_time, string os, Locale locale, uint recruiter, bool isARecruiter, IConfiguration configuration, WorldManager worldManager)
	{
		MuteTime = mute_time;
		_antiDos = new DosProtection(this);
		_socket = sock;
		_security = sec;
		_accountId = id;
		_accountName = name;
        _configuration = configuration;
        _worldManager = worldManager;
        _battlenetAccountId = battlenetAccountId;
		_configuredExpansion = _configuration.GetDefaultValue<int>("Player.OverrideExpansion", -1) == -1 ? Expansion.LevelCurrent : (Expansion)_configuration.GetDefaultValue<int>("Player.OverrideExpansion", -1);
		_accountExpansion = Expansion.LevelCurrent == _configuredExpansion ? expansion : _configuredExpansion;
		_expansion = (Expansion)Math.Min((byte)expansion, WorldConfig.GetIntValue(WorldCfg.Expansion));
		_os = os;
		_sessionDbLocaleIndex = locale;
		_recruiterId = recruiter;
		_isRecruiter = isARecruiter;
        _expireTime = 60000; // 1 min after socket loss, session is deleted

        _recvQueue = new(ProcessQueue, new ExecutionDataflowBlockOptions()
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
        if (_socket != null)
        {
            _socket.CloseSocket();
        }

        // empty incoming packet queue
		_recvQueue.Complete();

		DB.Login.Execute("UPDATE account SET online = 0 WHERE id = {0};", AccountId); // One-time query
	}

	public void LogoutPlayer(bool save)
	{
		if (_playerLogout)
			return;

		_playerLogout = true;
		_playerSave = save;


		if (_socket != null)
		{
			_socket.CloseSocket();
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


		if (_socket != null && _socket.IsOpen() && GameWarden != null)
			GameWarden.Update(diff);

		// If necessary, log the player out
		if (ShouldLogOut(currentTime) && PlayerLoading.IsEmpty)
			LogoutPlayer(true);

		//- Cleanup socket if need
		if (_socket != null && !_socket.IsOpen())
		{
			if (Player != null && GameWarden != null)
				GameWarden.Update(diff);

			_expireTime -= _expireTime > diff ? diff : _expireTime;

			if (_expireTime < diff || _forceExit || !Player)
                _socket.CloseSocket();
        }

		if (_socket == null)
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
			Log.outError(LogFilter.Network, "Prevented sending of UnknownOpcode to {0}", GetPlayerInfo());

			return;
		}

		if (_socket == null)
		{
			Log.outTrace(LogFilter.Network, "Prevented sending of {0} to non existent socket {1}", packet.GetOpcode(), GetPlayerInfo());

			return;
		}

		_socket.SendPacket(packet);
	}

    public void KickPlayer(string reason)
	{
		Log.outInfo(LogFilter.Network, $"Account: {AccountId} Character: '{(_player ? _player.GetName() : "<none>")}' {(_player ? _player.GUID : "")} kicked with reason: {reason}");

		if (_socket != null)
		{
			_socket.CloseSocket();
			_forceExit = true;
		}
	}

	public bool IsAddonRegistered(string prefix)
	{
		if (!_filterAddonMessages) // if we have hit the softcap (64) nothing should be filtered
			return true;

		if (RegisteredAddonPrefixes.Empty())
			return false;

		return RegisteredAddonPrefixes.Contains(prefix);
	}

	public void SendConnectToInstance(ConnectToSerial serial)
	{
		var instanceAddress = _worldManager.Realm.GetAddressForClient(System.Net.IPAddress.Parse(RemoteAddress));

		_instanceConnectKey.AccountId = AccountId;
		_instanceConnectKey.connectionType = ConnectionType.Instance;
		_instanceConnectKey.Key = RandomHelper.URand(0, 0x7FFFFFFF);

		ConnectTo connectTo = new();
		connectTo.Key = _instanceConnectKey.Raw;
		connectTo.Serial = serial;
		connectTo.Payload.Port = (ushort)WorldConfig.GetIntValue(WorldCfg.PortInstance);
		connectTo.Con = (byte)ConnectionType.Instance;

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

		Log.outError(LogFilter.Network, $"Player {Player.GetName()} ({Player.GUID}) sent a message which illegally contained a hyperlink:\n{str}");

		if (WorldConfig.GetIntValue(WorldCfg.ChatStrictLinkCheckingKick) != 0)
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

		if (!PlayerLoading.IsEmpty)
			ss.AppendFormat("Logging in: {0}, ", PlayerLoading.ToString());
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

    public void LoadPermissions()
	{
		var id = AccountId;
		var secLevel = Security;

		Log.outDebug(LogFilter.Rbac,
					"WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
					id,
					_accountName,
					_worldManager.Realm.Id.Index,
					secLevel);

		_rbacData = new RBACData(id, _accountName, (int)_worldManager.Realm.Id.Index, (byte)secLevel);
		_rbacData.LoadFromDB();
	}

	public QueryCallback LoadPermissionsAsync()
	{
		var id = AccountId;
		var secLevel = Security;

		Log.outDebug(LogFilter.Rbac,
					"WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
					id,
					_accountName,
					_worldManager.Realm.Id.Index,
					secLevel);

		_rbacData = new RBACData(id, _accountName, (int)_worldManager.Realm.Id.Index, (byte)secLevel);

		return _rbacData.LoadFromDBAsync();
	}



	public bool HasPermission(RBACPermissions permission)
	{
		if (_rbacData == null)
			LoadPermissions();

		var hasPermission = _rbacData.HasPermission(permission);

		Log.outDebug(LogFilter.Rbac,
					"WorldSession:HasPermission [AccountId: {0}, Name: {1}, realmId: {2}]",
					_rbacData.Id,
					_rbacData.Name,
					_worldManager.Realm.Id.Index);

		return hasPermission;
	}

	public void SendTimeSync()
	{
		TimeSyncRequest timeSyncRequest = new();
		timeSyncRequest.SequenceIndex = _timeSyncNextCounter;
		SendPacket(timeSyncRequest);

		_pendingTimeSyncRequests[_timeSyncNextCounter] = Time.MSTime;

		// Schedule next sync in 10 sec (except for the 2 first packets, which are spaced by only 5s)
		_timeSyncTimer = _timeSyncNextCounter == 0 ? 5000 : 10000u;
		_timeSyncNextCounter++;
	}

	public void ResetTimeOutTime(bool onlyActive)
	{
		if (Player)
			_timeOutTime = GameTime.GetGameTime() + WorldConfig.GetIntValue(WorldCfg.SocketTimeoutTimeActive);
		else if (!onlyActive)
			_timeOutTime = GameTime.GetGameTime() + WorldConfig.GetIntValue(WorldCfg.SocketTimeoutTime);
	}

	public static implicit operator bool(WorldSession session)
	{
		return session != null;
	}

	void ProcessQueue(WorldPacket packet)
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

	void ProcessInPlace()
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
			_socket?.CloseSocket();

		WorldPacket firstDelayedPacket = null;
		uint processedPackets = 0;
		var currentTime = GameTime.GetGameTime();

		//Check for any packets they was not recived yet.
		while (_socket != null && !_queue.IsEmpty && (_queue.TryPeek(out var packet) && packet != firstDelayedPacket) && _queue.TryDequeue(out packet))
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
								Log.outDebug(LogFilter.Network, "Re-enqueueing packet with opcode {0} with with status OpcodeStatus.Loggedin. Player is currently not in world yet.", (ClientOpcodes)packet.GetOpcode());
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
						Log.outError(LogFilter.Network, "Received not handled opcode {0} from {1}", (ClientOpcodes)packet.GetOpcode(), GetPlayerInfo());

						break;
				}
			}
			catch (InternalBufferOverflowException ex)
			{
				Log.outError(LogFilter.Network, "InternalBufferOverflowException: {0} while parsing {1} from {2}.", ex.Message, (ClientOpcodes)packet.GetOpcode(), GetPlayerInfo());
			}
			catch (EndOfStreamException)
			{
				Log.outError(LogFilter.Network,
							"WorldSession:Update EndOfStreamException occured while parsing a packet (opcode: {0}) from client {1}, accountid={2}. Skipped packet.",
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

	void LogUnexpectedOpcode(WorldPacket packet, SessionStatus status, string reason)
	{
		Log.outError(LogFilter.Network, "Received unexpected opcode {0} Status: {1} Reason: {2} from {3}", (ClientOpcodes)packet.GetOpcode(), status, reason, GetPlayerInfo());
	}

    public void SetLogoutStartTime(long requestTime)
	{
		_logoutTime = requestTime;
	}

	bool ShouldLogOut(long currTime)
	{
		return (_logoutTime > 0 && currTime >= _logoutTime + 20);
	}

	void ProcessQueryCallbacks()
	{
		_queryProcessor.ProcessReadyCallbacks();
		_transactionCallbacks.ProcessReadyCallbacks();
		_queryHolderProcessor.ProcessReadyCallbacks();
	}

    public AccountData GetAccountData(AccountDataTypes type)
	{
		return _accountData[(int)type];
	}

}
