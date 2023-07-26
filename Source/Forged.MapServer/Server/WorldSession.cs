// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
using Forged.MapServer.LootManagement;
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
using Forged.MapServer.Networking.Packets.Pet;
using Forged.MapServer.OpCodeHandlers;
using Forged.MapServer.OutdoorPVP;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Realm;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Server;

public class WorldSession : IDisposable
{
    public long MuteTime;

    private readonly AccountData[] _accountData = new AccountData[(int)AccountDataTypes.Max];
    private readonly DosProtection _antiDos;
    private readonly AutoResetEvent _asyncMessageQueueSemaphore = new(false);
    private readonly BattlegroundManager _battlegroundManager;
    private readonly CancellationTokenSource _cancellationToken = new();
    private readonly CharacterDatabase _characterDatabase;
    private readonly ClassFactory _classFactory;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly GridDefines _gridDefines;
    private readonly GuildManager _guildManager;
    private readonly ConcurrentQueue<WorldPacket> _inPlaceQueue = new();
    private readonly InstanceLockManager _instanceLockManager;
    private readonly LoginDatabase _loginDatabase;
    private readonly MapManager _mapManager;
    private readonly OutdoorPvPManager _outdoorPvPManager;
    private readonly AsyncCallbackProcessor<ISqlCallback> _queryHolderProcessor = new();
    private readonly Realm _realm;
    private readonly ActionBlock<WorldPacket> _recvQueue;
    private readonly List<string> _registeredAddonPrefixes = new();
    private readonly ScriptManager _scriptManager;
    private readonly SocialManager _socialManager;
    private readonly ConcurrentQueue<WorldPacket> _threadSafeQueue = new();
    private readonly ConcurrentQueue<WorldPacket> _threadUnsafe = new();
    private readonly AsyncCallbackProcessor<TransactionCallback> _transactionCallbacks = new();
    private readonly uint[] _tutorials = new uint[SharedConst.MaxAccountTutorialValues];
    private uint _expireTime;
    private bool _filterAddonMessages;
    private bool _forceExit;
    private bool _inQueue;
    private ConnectToKey _instanceConnectKey;
    private string _loadingPlayerInfo;
    private long _logoutTime;
    private string _playerInfo;

    // code processed in LoginPlayer
    private bool _playerSave;

    private long _timeOutTime;
    private uint _timeSyncNextCounter;
    private uint _timeSyncTimer;
    private TutorialsFlag _tutorialsChanged;
    private Warden.Warden _warden;

    public WorldSession(uint id, string name, uint battlenetAccountId, WorldSocket sock, AccountTypes sec, Expansion expansion, long muteTime, string os, Locale locale, uint recruiter, bool isARecruiter, ClassFactory classFactory)
    {
        MuteTime = muteTime;
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
        PacketRouter = classFactory.Resolve<PacketRouter>();
        _gameObjectManager = classFactory.Resolve<GameObjectManager>();
        _scriptManager = classFactory.Resolve<ScriptManager>();
        _socialManager = classFactory.Resolve<SocialManager>();
        _guildManager = classFactory.Resolve<GuildManager>();
        _gridDefines = classFactory.Resolve<GridDefines>();
        var configuredExpansion = _configuration.GetDefaultValue("Player:OverrideExpansion", -1) == -1 ? Expansion.LevelCurrent : (Expansion)_configuration.GetDefaultValue("Player:OverrideExpansion", -1);
        AccountExpansion = Expansion.LevelCurrent == configuredExpansion ? expansion : configuredExpansion;
        Expansion = (Expansion)Math.Min((byte)expansion, _configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight));
        OS = os;
        SessionDbcLocale = classFactory.Resolve<WorldManager>().GetAvailableDbcLocale(locale);
        SessionDbLocaleIndex = locale;
        RecruiterId = recruiter;
        IsARecruiter = isARecruiter;
        _expireTime = 60000; // 1 min after socket loss, session is deleted
        BattlePetMgr = _classFactory.ResolveWithPositionalParameters<BattlePetMgr>(this);
        CollectionMgr = _classFactory.ResolveWithPositionalParameters<CollectionMgr>(this);
        BattlePayMgr = _classFactory.ResolveWithPositionalParameters<BattlepayManager>(this);
        CommandHandler = _classFactory.ResolveWithPositionalParameters<CommandHandler>(this);
        _antiDos = _classFactory.ResolveWithPositionalParameters<DosProtection>(this);

        _recvQueue = new ActionBlock<WorldPacket>(ProcessQueue,
                                                  new ExecutionDataflowBlockOptions
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

    // Remains NULL if Warden system is not enabled by config
    public Expansion AccountExpansion { get; }

    public ObjectGuid AccountGUID => ObjectGuid.Create(HighGuid.WowAccount, AccountId);
    public uint AccountId { get; }
    public string AccountName { get; }
    public ObjectGuid BattlenetAccountGUID => ObjectGuid.Create(HighGuid.BNetAccount, BattlenetAccountId);
    public uint BattlenetAccountId { get; }
    public BattlepayManager BattlePayMgr { get; }

    // Battle Pets
    public BattlePetMgr BattlePetMgr { get; }

    // Packets cooldown
    public long CalendarEventCreationCooldown { get; set; }

    public bool CanSpeak => MuteTime <= GameTime.CurrentTime;
    public CollectionMgr CollectionMgr { get; }
    public CommandHandler CommandHandler { get; private set; }
    public ulong ConnectToInstanceKey => _instanceConnectKey.Raw;
    public Expansion Expansion { get; }
    public bool IsARecruiter { get; }
    public bool IsLogingOut => _logoutTime != 0 || PlayerLogout;
    public uint Latency { get; set; }
    public string OS { get; }
    public PacketRouter PacketRouter { get; }
    public Player Player { get; set; }
    public bool PlayerDisconnected => Socket is not { IsOpen: true };
    public bool PlayerLoading => !PlayerLoadingGuid.IsEmpty;
    public ObjectGuid PlayerLoadingGuid { get; set; }
    public bool PlayerLogout { get; private set; }

    public bool PlayerLogoutWithSave => PlayerLogout && _playerSave;

    // Packets cooldown
    public string PlayerName => Player != null ? Player.GetName() : "Unknown";

    public bool PlayerRecentlyLoggedOut { get; private set; }
    public AsyncCallbackProcessor<QueryCallback> QueryProcessor { get; } = new();
    public RBACData RBACData { get; private set; }

    public Dictionary<uint, byte> RealmCharacterCounts { get; } = new();

    // Battlenet
    public Array<byte> RealmListSecret { get; set; } = new(32);

    public uint RecruiterId { get; }
    public string RemoteAddress { get; }
    public AccountTypes Security { get; }
    public Locale SessionDbcLocale { get; }
    public Locale SessionDbLocaleIndex { get; }
    public WorldSocket Socket { get; set; }
    private bool IsConnectionIdle => _timeOutTime < GameTime.CurrentTime && !_inQueue;

    public SQLQueryHolderCallback<TR> AddQueryHolderCallback<TR>(SQLQueryHolderCallback<TR> callback)
    {
        return (SQLQueryHolderCallback<TR>)_queryHolderProcessor.AddCallback(callback);
    }

    public bool CanAccessAlliedRaces()
    {
        if (_configuration.GetDefaultValue("CharacterCreating:DisableAlliedRaceAchievementRequirement", false))
            return true;

        return AccountExpansion >= Expansion.BattleForAzeroth;
    }

    public void ClearRegisteredAddons()
    {
        _registeredAddonPrefixes.Clear();
    }

    public bool DisallowHyperlinksAndMaybeKick(string str)
    {
        if (!str.Contains('|'))
            return true;

        Log.Logger.Error($"Player {Player.GetName()} ({Player.GUID}) sent a message which illegally contained a hyperlink:\n{str}");

        if (_configuration.GetDefaultValue("ChatStrictLinkChecking:Kick", 0) != 0)
            KickPlayer("WorldSession::DisallowHyperlinksAndMaybeKick Illegal chat link");

        return false;
    }

    public void Dispose()
    {
        _cancellationToken.Cancel();

        // unload player if not unloaded
        if (Player != null)
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

    public void DoLootRelease(Loot loot)
    {
        var lguid = loot.OwnerGuid;
        var player = Player;

        if (player.GetLootGUID() == lguid)
            player.SetLootGUID(ObjectGuid.Empty);

        //Player is not looking at loot list, he doesn't need to see updates on the loot list
        loot.RemoveLooter(player.GUID);
        player.SendLootRelease(lguid);
        player.GetAELootView().Remove(loot.Guid);

        if (player.GetAELootView().Empty())
            player.RemoveUnitFlag(UnitFlags.Looting);

        if (!player.Location.IsInWorld)
            return;

        if (lguid.IsGameObject)
        {
            var go = player.Location.Map.GetGameObject(lguid);

            // not check distance for GO in case owned GO (fishing bobber case, for example) or Fishing hole GO
            if (go == null || (go.OwnerGUID != player.GUID && go.GoType != GameObjectTypes.FishingHole && !go.IsWithinDistInMap(player)))
                return;

            if (loot.IsLooted() || go.GoType is GameObjectTypes.FishingNode or GameObjectTypes.FishingHole)
            {
                switch (go.GoType)
                {
                    case GameObjectTypes.FishingNode:
                        go.SetLootState(LootState.JustDeactivated);

                        break;

                    case GameObjectTypes.FishingHole:
                    {
                        // The fishing hole used once more
                        // if the max usage is reached, will be despawned in next tick
                        go.AddUse();
                        go.SetLootState(go.UseCount >= go.GoValue.FishingHole.MaxOpens ? LootState.JustDeactivated : LootState.Ready);

                        break;
                    }
                    default:
                    {
                        if (go.GoType != GameObjectTypes.GatheringNode && go.IsFullyLooted)
                            go.SetLootState(LootState.JustDeactivated);

                        break;
                    }
                }

                go.OnLootRelease(player);
            }
            else
                // not fully looted object
                go.SetLootState(LootState.Activated, player);
        }
        else if (lguid.IsCorpse) // ONLY remove insignia at BG
        {
            var corpse = ObjectAccessor.GetCorpse(player, lguid);

            if (corpse == null || !corpse.Location.IsWithinDistInMap(player, SharedConst.InteractionDistance))
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

            if (pItem == null)
                return;

            var proto = pItem.Template;

            // destroy only 5 items from stack in case prospecting and milling
            if (loot.LootType is LootType.Prospecting or LootType.Milling)
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

    public AccountData GetAccountData(AccountDataTypes type)
    {
        return _accountData[(int)type];
    }

    public string GetPlayerInfo()
    {
        if (!PlayerLoadingGuid.IsEmpty && !string.IsNullOrEmpty(_loadingPlayerInfo))
            return _loadingPlayerInfo;

        if (Player != null && !string.IsNullOrEmpty(_playerInfo))
            return _playerInfo;

        StringBuilder ss = new();
        ss.Append("[Player: ");

        if (!PlayerLoadingGuid.IsEmpty)
            ss.Append($"Logging in: {PlayerLoadingGuid.ToString()}, ");
        else if (Player != null)
            ss.Append($"{Player.GetName()} {Player.GUID.ToString()}, ");

        ss.Append($"Account: {AccountId}]");

        var infp = ss.ToString();

        if (!PlayerLoadingGuid.IsEmpty)
            _loadingPlayerInfo = infp;
        else if (Player != null)
            _playerInfo = infp;

        return infp;
    }

    public uint GetTutorialInt(byte index)
    {
        return _tutorials[index];
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

    public void InitializeSession()
    {
        var realmHolder = _classFactory.Resolve<AccountInfoQueryHolderPerRealm>();
        realmHolder.Initialize(AccountId, BattlenetAccountId);

        var holder = _classFactory.Resolve<AccountInfoQueryHolder>();
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

    public void InvalidateRBACData()
    {
        Log.Logger.Debug("WorldSession:Invalidaterbac:RBACData [AccountId: {0}, Name: {1}, realmId: {2}]",
                         RBACData.Id,
                         RBACData.Name,
                         _realm.Id.Index);

        RBACData = null;
    }

    public bool IsAddonRegistered(string prefix)
    {
        if (!_filterAddonMessages) // if we have hit the softcap (64) nothing should be filtered
            return true;

        return !_registeredAddonPrefixes.Empty() && _registeredAddonPrefixes.Contains(prefix);
    }

    public void KickPlayer(string reason)
    {
        Log.Logger.Information($"Account: {AccountId} Character: '{(Player != null ? Player.GetName() : "<none>")}' {(Player != null ? Player.GUID : "")} kicked with reason: {reason}");

        if (Socket == null)
            return;

        Socket.CloseSocket();
        _forceExit = true;
    }

    public void LoadAccountData(SQLResult result, AccountDataTypes mask)
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

    public void LoadPermissions()
    {
        var secLevel = Security;

        Log.Logger.Debug("WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
                         AccountId,
                         AccountName,
                         _realm.Id.Index,
                         secLevel);

        RBACData = new RBACData(AccountId, AccountName, (int)_realm.Id.Index, _classFactory.Resolve<AccountManager>(), _classFactory.Resolve<LoginDatabase>(), (byte)secLevel);
        RBACData.LoadFromDB();
    }

    public QueryCallback LoadPermissionsAsync()
    {
        var secLevel = Security;

        Log.Logger.Debug("WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
                         AccountId,
                         AccountName,
                         _realm.Id.Index,
                         secLevel);

        RBACData = new RBACData(AccountId, AccountName, (int)_realm.Id.Index, _classFactory.Resolve<AccountManager>(), _classFactory.Resolve<LoginDatabase>(), (byte)secLevel);

        return RBACData.LoadFromDBAsync();
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

    public void LogoutPlayer(bool save)
    {
        if (PlayerLogout)
            return;

        // finish pending transfers before starting the logout
        while (Player is { IsBeingTeleportedFar: true })
            HandleMoveWorldportAck();

        PlayerLogout = true;
        _playerSave = save;

        if (Player != null)
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

            //drop a Id if player is carrying it
            var bg = Player.Battleground;

            bg?.EventPlayerLoggedOut(Player);

            // Teleport to home if the player is in an invalid instance
            if (!Player.InstanceValid && !Player.IsGameMaster)
                Player.TeleportTo(Player.Homebind);

            _outdoorPvPManager.HandlePlayerLeaveZone(Player, Player.Location.Zone);

            for (uint i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            {
                var bgQueueTypeId = Player.GetBattlegroundQueueTypeId(i);

                if (bgQueueTypeId == default)
                    continue;

                Player.RemoveBattlegroundQueueId(bgQueueTypeId);
                var queue = _battlegroundManager.GetBattlegroundQueue(bgQueueTypeId);
                queue.RemovePlayer(Player.GUID, true);
            }

            // Repop at GraveYard or other player far teleport will prevent saving player because of not present map
            // Teleport player immediately for correct player save
            while (Player.IsBeingTeleportedFar)
                HandleMoveWorldportAck();

            // If the player is in a guild, update the guild roster and broadcast a logout message to other guild members
            var guild = _guildManager.GetGuildById(Player.GuildId);

            guild?.HandleMemberLogout(this);

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

            map?.RemovePlayerFromMap(Player, true);

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

    public void QueuePacket(WorldPacket packet)
    {
        _recvQueue.Post(packet);
    }

    public void ResetTimeOutTime(bool onlyActive)
    {
        if (Player != null)
            _timeOutTime = GameTime.CurrentTime + _configuration.GetDefaultValue("SocketTimeOutTimeActive", 60);
        else if (!onlyActive)
            _timeOutTime = GameTime.CurrentTime + _configuration.GetDefaultValue("SocketTimeOutTime", 900);
    }

    public void ResetTimeSync()
    {
        _timeSyncNextCounter = 0;
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

        // now has, set Id so next save uses update query
        if (!hasTutorialsInDB)
            _tutorialsChanged |= TutorialsFlag.LoadedFromDB;

        _tutorialsChanged &= ~TutorialsFlag.Changed;
    }

    public void SendAccountDataTimes(ObjectGuid playerGuid, AccountDataTypes mask)
    {
        AccountDataTimes accountDataTimes = new()
        {
            PlayerGuid = playerGuid,
            ServerTime = GameTime.CurrentTime
        };

        for (var i = 0; i < (int)AccountDataTypes.Max; ++i)
            if (((int)mask & (1 << i)) != 0)
                accountDataTimes.AccountTimes[i] = GetAccountData((AccountDataTypes)i).Time;

        SendPacket(accountDataTimes);
    }

    public void SendConnectToInstance(ConnectToSerial serial)
    {
        var instanceAddress = _realm.GetAddressForClient(IPAddress.Parse(RemoteAddress));

        _instanceConnectKey.AccountId = AccountId;
        _instanceConnectKey.ConnectionType = ConnectionType.Instance;
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

        if (instanceAddress.AddressFamily == AddressFamily.InterNetwork)
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

    public void SendPacket(ServerPacket packet)
    {
        if (packet == null)
            return;

        if (packet.Opcode is ServerOpcodes.Unknown or ServerOpcodes.Max)
        {
            Log.Logger.Error("Prevented sending of UnknownOpcode to {0}", GetPlayerInfo());

            return;
        }

        if (Socket == null)
        {
            Log.Logger.Verbose("Prevented sending of {0} to non existent socket to {1}", packet.Opcode, GetPlayerInfo());

            return;
        }

        Socket.SendPacket(packet);
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

    public void SendTutorialsData()
    {
        TutorialFlags packet = new();
        Array.Copy(_tutorials, packet.TutorialData, SharedConst.MaxAccountTutorialValues);
        SendPacket(packet);
    }

    public void SetInQueue(bool state)
    {
        _inQueue = state;
    }

    public void SetLogoutStartTime(long requestTime)
    {
        _logoutTime = requestTime;
    }

    public void SetTutorialInt(byte index, uint value)
    {
        if (_tutorials[index] == value)
            return;

        _tutorials[index] = value;
        _tutorialsChanged |= TutorialsFlag.Changed;
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

        if (Socket is { IsOpen: true } && _warden != null)
            _warden.Update(diff);

        // If necessary, log the player out
        if (ShouldLogOut(currentTime) && PlayerLoadingGuid.IsEmpty)
            LogoutPlayer(true);

        //- Cleanup socket if need
        if (Socket == null || Socket.IsOpen)
            return Socket != null;

        if (Player != null && _warden != null)
            _warden.Update(diff);

        _expireTime -= _expireTime > diff ? diff : _expireTime;

        if (_expireTime >= diff && !_forceExit && Player != null)
            return Socket != null;

        if (Socket == null)
            return Socket != null;

        Socket.CloseSocket();
        Socket = null;

        return Socket != null;
        //Will remove this session from the world session map
    }

    public bool ValidateHyperlinksAndMaybeKick(string str)
    {
        if (Hyperlink.CheckAllLinks(str))
            return true;

        Log.Logger.Error($"Player {Player.GetName()} {Player.GUID} sent a message with an invalid link:\n{str}");

        if (_configuration.GetDefaultValue("ChatStrictLinkChecking:Kick", 0) != 0)
            KickPlayer("WorldSession::ValidateHyperlinksAndMaybeKick Invalid chat link");

        return false;
    }

    public void SendPetStableResult(StableResult result)
    {
        PetStableResult petStableResult = new();
        petStableResult.Result = result;
        SendPacket(petStableResult);
    }

    internal TransactionCallback AddTransactionCallback(TransactionCallback callback)
    {
        return _transactionCallbacks.AddCallback(callback);
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
        var currentTime = GameTime.CurrentTime;

        //Check for any packets they was not recived yet.
        while (Socket != null && !queue.IsEmpty && queue.TryPeek(out var packet) && packet != firstDelayedPacket && queue.TryDequeue(out packet))
        {
            try
            {
                if (!PacketRouter.TryGetProcessor((ClientOpcodes)packet.Opcode, out var packetProcessor))
                {
                    Log.Logger.Error("Unknown opcode {0} from {1}", packet.Opcode, GetPlayerInfo());

                    continue;
                }

                switch (packetProcessor.SessionStatus)
                {
                    case SessionStatus.Loggedin:
                        if (Player == null)
                        {
                            if (!PlayerRecentlyLoggedOut)
                            {
                                firstDelayedPacket ??= packet;

                                QueuePacket(packet);
                                Log.Logger.Debug("Re-enqueueing packet with opcode {0} with with status OpcodeStatus.Loggedin. Player is currently not in world yet.", (ClientOpcodes)packet.Opcode);
                            }

                            break;
                        }

                        if (Player.Location.IsInWorld && _antiDos.EvaluateOpcode(packet, currentTime))
                            packetProcessor.Invoke(packet);

                        break;

                    case SessionStatus.LoggedinOrRecentlyLogout:
                        if (Player == null && !PlayerRecentlyLoggedOut && !PlayerLogout)
                            LogUnexpectedOpcode(packet, packetProcessor.SessionStatus, "the player has not logged in yet and not recently logout");
                        else if (_antiDos.EvaluateOpcode(packet, currentTime))
                            packetProcessor.Invoke(packet);

                        break;

                    case SessionStatus.Transfer:
                        if (Player == null)
                            LogUnexpectedOpcode(packet, packetProcessor.SessionStatus, "the player has not logged in yet");
                        else if (Player.Location.IsInWorld)
                            LogUnexpectedOpcode(packet, packetProcessor.SessionStatus, "the player is still in world");
                        else if (_antiDos.EvaluateOpcode(packet, currentTime))
                            packetProcessor.Invoke(packet);

                        break;

                    case SessionStatus.Authed:
                        // prevent cheating with skip queue wait
                        if (_inQueue)
                        {
                            LogUnexpectedOpcode(packet, packetProcessor.SessionStatus, "the player not pass queue yet");

                            break;
                        }

                        if ((ClientOpcodes)packet.Opcode == ClientOpcodes.EnumCharacters)
                            PlayerRecentlyLoggedOut = false;

                        if (_antiDos.EvaluateOpcode(packet, currentTime))
                            packetProcessor.Invoke(packet);

                        break;

                    default:
                        Log.Logger.Error("Received not handled opcode {0} from {1}", (ClientOpcodes)packet.Opcode, GetPlayerInfo());

                        break;
                }
            }
            catch (InternalBufferOverflowException ex)
            {
                Log.Logger.Error("InternalBufferOverflowException: {0} while parsing {1} from {2}.", ex.Message, (ClientOpcodes)packet.Opcode, GetPlayerInfo());
            }
            catch (EndOfStreamException)
            {
                Log.Logger.Error("WorldSession:Update EndOfStreamException occured while parsing a packet (opcode: {0}) from client {1}, accountid={2}. Skipped packet.",
                                 (ClientOpcodes)packet.Opcode,
                                 RemoteAddress,
                                 AccountId);
            }

            processedPackets++;

            if (processedPackets > 100)
                break;
        }

        return currentTime;
    }

    private void HandleMoveWorldportAck()
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
        if (!_gridDefines.IsValidMapCoord(loc))
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

        transport?.RemovePassenger(player);

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

        player.Location.ResetMap();
        player.Location.Map = newMap;

        SendPacket(new ResumeToken()
        {
            SequenceIndex = player.MovementCounter,
            Reason = seamlessTeleport ? 2 : 1u
        });

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
            Log.Logger.Error($"WORLD: failed to teleport player {player.GetName()} ({player.GUID}) to map {loc.MapId} ({newMap.MapName}) because of unknown reason!");
            player.Location.ResetMap();
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
            else if (player.Battleground != null && player.IsInvitedForBattlegroundInstance(player.BattlegroundId))
                player.Battleground.AddPlayer(player);
        }

        if (!seamlessTeleport)
            player.SendInitialPacketsAfterAddToMap();
        else
        {
            player.UpdateVisibilityForPlayer();
            var garrison = player.Garrison;

            garrison?.SendRemoteInfo();
        }

        // flight fast teleport case
        if (player.IsInFlight)
        {
            if (!player.InBattleground)
            {
                if (seamlessTeleport)
                    return;

                // short preparations to continue flight
                var movementGenerator = player.MotionMaster.GetCurrentMovementGenerator();
                movementGenerator.Initialize(player);

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
            MapDb2Entries entries = new(mapEntry.Id, newMap.DifficultyID, _cliDB, _db2Manager);

            if (entries.MapDifficulty.HasResetSchedule())
            {
                RaidInstanceMessage raidInstanceMessage = new()
                {
                    Type = InstanceResetWarningType.Welcome,
                    MapID = mapEntry.Id,
                    DifficultyID = newMap.DifficultyID
                };

                var playerLock = _instanceLockManager.FindActiveInstanceLock(Player.GUID, entries);

                if (playerLock != null)
                {
                    raidInstanceMessage.Locked = !playerLock.IsExpired;
                    raidInstanceMessage.Extended = playerLock.IsExtended;
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
        player.UpdateZone(player.Location.Zone, player.Location.Area);

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

    private void InitializeSessionCallback(SQLQueryHolder<AccountInfoQueryLoad> holder, SQLQueryHolder<AccountInfoQueryLoad> realmHolder)
    {
        if (!PacketRouter.TryGetOpCodeHandler(out AuthenticationHandler authenticationHandler))
        {
            Log.Logger.Error("WORLD: failed to get AuthenticationHandler!"); // should NEVER happen
            SendPacket(new ConnectionStatus()
            {
                State = 0
            });

            Socket?.CloseSocket();
            Socket = null;
            return;
        }

        LoadAccountData(realmHolder.GetResult(AccountInfoQueryLoad.GlobalAccountDataIndexPerRealm), AccountDataTypes.GlobalCacheMask);
        LoadTutorialsData(realmHolder.GetResult(AccountInfoQueryLoad.TutorialsIndexPerRealm));
        CollectionMgr.LoadAccountToys(holder.GetResult(AccountInfoQueryLoad.GlobalAccountToys));
        CollectionMgr.LoadAccountHeirlooms(holder.GetResult(AccountInfoQueryLoad.GlobalAccountHeirlooms));
        CollectionMgr.LoadAccountMounts(holder.GetResult(AccountInfoQueryLoad.Mounts));
        CollectionMgr.LoadAccountItemAppearances(holder.GetResult(AccountInfoQueryLoad.ItemAppearances), holder.GetResult(AccountInfoQueryLoad.ItemFavoriteAppearances));
        CollectionMgr.LoadAccountTransmogIllusions(holder.GetResult(AccountInfoQueryLoad.TransmogIllusions));

        if (!_inQueue)
            authenticationHandler.SendAuthResponse(BattlenetRpcErrorCode.Ok, false);
        else
            authenticationHandler.SendAuthWaitQueue(0);

        SetInQueue(false);
        ResetTimeOutTime(false);

        authenticationHandler.SendSetTimeZoneInformation();
        authenticationHandler.SendFeatureSystemStatusGlueScreen();
        authenticationHandler.SendClientCacheVersion(_configuration.GetDefaultValue("ClientCacheVersion", 0u));
        SendAvailableHotfixes();
        SendAccountDataTimes(ObjectGuid.Empty, AccountDataTypes.GlobalCacheMask);
        SendTutorialsData();

        var result = holder.GetResult(AccountInfoQueryLoad.GlobalRealmCharacterCounts);

        if (!result.IsEmpty())
            do
            {
                RealmCharacterCounts[new RealmId(result.Read<byte>(3), result.Read<byte>(4), result.Read<uint>(2)).VirtualRealmAddress] = result.Read<byte>(1);
            } while (result.NextRow());

        ConnectionStatus bnetConnected = new()
        {
            State = 1
        };

        SendPacket(bnetConnected);

        BattlePetMgr.LoadFromDB(holder.GetResult(AccountInfoQueryLoad.BattlePets), holder.GetResult(AccountInfoQueryLoad.BattlePetSlot));
    }

    private void LogUnexpectedOpcode(WorldPacket packet, SessionStatus status, string reason)
    {
        Log.Logger.Error("Received unexpected opcode {0} Status: {1} Reason: {2} from {3}", (ClientOpcodes)packet.Opcode, status, reason, GetPlayerInfo());
    }

    private void ProcessInPlace()
    {
        while (!_cancellationToken.IsCancellationRequested)
        {
            _asyncMessageQueueSemaphore.WaitOne(500);
            DrainQueue(_inPlaceQueue);
        }
    }

    private void ProcessQueryCallbacks()
    {
        QueryProcessor.ProcessReadyCallbacks();
        _transactionCallbacks.ProcessReadyCallbacks();
        _queryHolderProcessor.ProcessReadyCallbacks();
    }

    private void ProcessQueue(WorldPacket packet)
    {
        if (!PacketRouter.TryGetProcessor((ClientOpcodes)packet.Opcode, out var packetProcessor))
            return;

        if (packetProcessor.ProcessingPlace != PacketProcessing.Inplace)
        {
            if (packetProcessor.ProcessingPlace == PacketProcessing.ThreadSafe)
                _threadSafeQueue.Enqueue(packet);
            else
                _threadUnsafe.Enqueue(packet);
        }
        else
        {
            _inPlaceQueue.Enqueue(packet);
            _asyncMessageQueueSemaphore.Set();
        }
    }

    private void SendAvailableHotfixes()
    {
        SendPacket(new AvailableHotfixes(_realm.Id.VirtualRealmAddress, _db2Manager.GetHotfixData()));
    }

    private bool ShouldLogOut(long currTime)
    {
        return _logoutTime > 0 && currTime >= _logoutTime + 20;
    }
}