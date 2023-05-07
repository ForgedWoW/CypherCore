// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Accounts;
using Forged.MapServer.Cache;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Channel;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Chat.Channels;

public class Channel
{
    private readonly AccountManager _accountManager;
    private readonly List<ObjectGuid> _bannedStore = new();
    private readonly string _channelName;
    private readonly TeamFaction _channelTeam;
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CliDB _cliDB;
    private readonly IConfiguration _configuration;
    private readonly GameObjectManager _gameObjectManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly Dictionary<ObjectGuid, PlayerInfo> _playersStore = new();
    private readonly WorldManager _worldManager;
    private string _channelPassword;
    private bool _isDirty; // whether the channel needs to be saved to DB
    private bool _isOwnerInvisible;
    private long _nextActivityUpdateTime;
    private ObjectGuid _ownerGuid;
    private bool _ownershipEnabled;

    public Channel(ObjectGuid guid, uint channelId, TeamFaction team, AreaTableRecord zoneEntry,
                   CliDB cliDB, GameObjectManager gameObjectManager, ObjectAccessor objectAccessor, CharacterCache characterCache,
                   IConfiguration configuration, AccountManager accountManager, CharacterDatabase characterDatabase, WorldManager worldManager)
    {
        Flags = ChannelFlags.General;
        ChannelId = channelId;
        _channelTeam = team;
        GUID = guid;
        ZoneEntry = zoneEntry;
        _cliDB = cliDB;
        _gameObjectManager = gameObjectManager;
        _objectAccessor = objectAccessor;
        _characterCache = characterCache;
        _configuration = configuration;
        _accountManager = accountManager;
        _characterDatabase = characterDatabase;
        _worldManager = worldManager;

        var channelEntry = _cliDB.ChatChannelsStorage.LookupByKey(channelId);

        if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Trade)) // for trade channel
            Flags |= ChannelFlags.Trade;

        if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly2)) // for city only channels
            Flags |= ChannelFlags.City;

        if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Lfg)) // for LFG channel
            Flags |= ChannelFlags.Lfg;
        else // for all other channels
            Flags |= ChannelFlags.NotLfg;
    }

    public Channel(ObjectGuid guid, string name, TeamFaction team = 0, string banList = "")
    {
        IsAnnounce = true;
        _ownershipEnabled = true;
        Flags = ChannelFlags.Custom;
        _channelTeam = team;
        GUID = guid;
        _channelName = name;

        StringArray tokens = new(banList, ' ');

        foreach (string token in tokens)
        {
            // legacy db content might not have 0x prefix, account for that
            var bannedGuidStr = token.Contains("0x") ? token[2..] : token;
            ObjectGuid banned = new();
            banned.SetRawValue(ulong.Parse(bannedGuidStr[..16]), ulong.Parse(bannedGuidStr[16..]));

            if (banned.IsEmpty)
                continue;

            Log.Logger.Debug($"Channel({name}) loaded player {banned} into bannedStore");
            _bannedStore.Add(banned);
        }
    }

    public uint ChannelId { get; }
    public ChannelFlags Flags { get; }
    public ObjectGuid GUID { get; }
    public bool IsConstant => ChannelId != 0;
    public bool IsLFG => Flags.HasAnyFlag(ChannelFlags.Lfg);
    public int NumPlayers => _playersStore.Count;
    public AreaTableRecord ZoneEntry { get; }
    private bool IsAnnounce { get; set; }

    public static void GetChannelName(ref string channelName, uint channelId, Locale locale, AreaTableRecord zoneEntry, CliDB cliDB, GameObjectManager objectManager)
    {
        if (channelId == 0)
            return;

        var channelEntry = cliDB.ChatChannelsStorage.LookupByKey(channelId);

        if (!channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Global))
        {
            if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly))
                channelName = string.Format(channelEntry.Name[locale].ConvertFormatSyntax(), objectManager.GetCypherString(CypherStrings.ChannelCity, locale));
            else
                channelName = string.Format(channelEntry.Name[locale].ConvertFormatSyntax(), zoneEntry.AreaName[locale]);
        }
        else
            channelName = channelEntry.Name[locale];
    }

    public void AddonSay(ObjectGuid guid, string prefix, string what, bool isLogged)
    {
        if (what.IsEmpty())
            return;

        if (!IsOn(guid))
        {
            NotMemberAppend appender;
            ChannelNameBuilder builder = new(this, appender, _worldManager);
            SendToOne(builder, guid);

            return;
        }

        var playerInfo = _playersStore.LookupByKey(guid);

        if (playerInfo.IsMuted)
        {
            MutedAppend appender;
            ChannelNameBuilder builder = new(this, appender, _worldManager);
            SendToOne(builder, guid);

            return;
        }

        var player = _objectAccessor.FindConnectedPlayer(guid);

        SendToAllWithAddon(new ChannelWhisperBuilder(this, isLogged ? Language.AddonLogged : Language.Addon, what, prefix, guid, _worldManager, _objectAccessor),
                           prefix,
                           !playerInfo.IsModerator ? guid : ObjectGuid.Empty,
                           !playerInfo.IsModerator && player ? player.Session.AccountGUID : ObjectGuid.Empty);
    }

    public void Announce(Player player)
    {
        var guid = player.GUID;

        if (!IsOn(guid))
        {
            ChannelNameBuilder builder = new(this, new NotMemberAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        var playerInfo = _playersStore.LookupByKey(guid);

        if (!playerInfo.IsModerator && !player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator))
        {
            ChannelNameBuilder builder = new(this, new NotModeratorAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        IsAnnounce = !IsAnnounce;

        if (IsAnnounce)
        {
            ChannelNameBuilder builder = new(this, new AnnouncementsOnAppend(guid), _worldManager);
            SendToAll(builder);
        }
        else
        {
            ChannelNameBuilder builder = new(this, new AnnouncementsOffAppend(guid), _worldManager);
            SendToAll(builder);
        }

        _isDirty = true;
    }

    public void Ban(Player player, string badname)
    {
        KickOrBan(player, badname, true);
    }

    public bool CheckPassword(string password)
    {
        return _channelPassword.IsEmpty() || _channelPassword == password;
    }

    public void DeclineInvite(Player player) { }

    public string GetName(Locale locale = Locale.enUS)
    {
        var result = _channelName;
        GetChannelName(ref result, ChannelId, locale, ZoneEntry, _cliDB, _gameObjectManager);

        return result;
    }

    public ChannelMemberFlags GetPlayerFlags(ObjectGuid guid)
    {
        var info = _playersStore.LookupByKey(guid);

        return info?.Flags ?? 0;
    }

    public void Invite(Player player, string newname)
    {
        var guid = player.GUID;

        if (!IsOn(guid))
        {
            ChannelNameBuilder builder = new(this, new NotMemberAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        var newp = _objectAccessor.FindPlayerByName(newname);

        if (!newp || !newp.IsGMVisible)
        {
            ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(newname), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        if (IsBanned(newp.GUID))
        {
            ChannelNameBuilder builder = new(this, new PlayerInviteBannedAppend(newname), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        if (newp.Team != player.Team &&
            (!player.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
             !newp.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel)))
        {
            ChannelNameBuilder builder = new(this, new InviteWrongFactionAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        if (IsOn(newp.GUID))
        {
            ChannelNameBuilder builder = new(this, new PlayerAlreadyMemberAppend(newp.GUID), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        if (!newp.Social.HasIgnore(guid, player.Session.AccountGUID))
        {
            ChannelNameBuilder builder = new(this, new InviteAppend(guid), _worldManager);
            SendToOne(builder, newp.GUID);
        }

        ChannelNameBuilder builder1 = new(this, new PlayerInvitedAppend(newp.GetName()), _worldManager);
        SendToOne(builder1, guid);
    }

    public void JoinChannel(Player player, string pass = "")
    {
        var guid = player.GUID;

        if (IsOn(guid))
        {
            // Do not send error message for built-in channels
            if (!IsConstant)
            {
                var builder = new ChannelNameBuilder(this, new PlayerAlreadyMemberAppend(guid), _worldManager);
                SendToOne(builder, guid);
            }

            return;
        }

        if (IsBanned(guid))
        {
            var builder = new ChannelNameBuilder(this, new BannedAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        if (!CheckPassword(pass))
        {
            var builder = new ChannelNameBuilder(this, new WrongPasswordAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        if (HasFlag(ChannelFlags.Lfg) &&
            _configuration.GetDefaultValue("Channel:RestrictedLfg", true) &&
            _accountManager.IsPlayerAccount(player.Session.Security) && //FIXME: Move to RBAC
            player.Group)
        {
            var builder = new ChannelNameBuilder(this, new NotInLFGAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        player.JoinedChannel(this);

        if (IsAnnounce && !player.Session.HasPermission(RBACPermissions.SilentlyJoinChannel))
        {
            var builder = new ChannelNameBuilder(this, new JoinedAppend(guid), _worldManager);
            SendToAll(builder);
        }

        var newChannel = _playersStore.Empty();

        if (newChannel)
            _nextActivityUpdateTime = 0; // force activity update on next channel tick

        PlayerInfo playerInfo = new();
        playerInfo.SetInvisible(!player.IsGMVisible);
        _playersStore[guid] = playerInfo;

        /*
        ChannelNameBuilder<YouJoinedAppend> builder = new ChannelNameBuilder(this, new YouJoinedAppend());
        SendToOne(builder, guid);
        */

        SendToOne(new ChannelNotifyJoinedBuilder(this, _worldManager), guid);

        JoinNotify(player);

        // Custom channel handling
        if (!IsConstant)
            // If the channel has no owner yet and ownership is allowed, set the new owner.
            // or if the owner was a GM with .gm visible off
            // don't do this if the new player is, too, an invis GM, unless the channel was empty
            if (_ownershipEnabled && (newChannel || !playerInfo.IsInvisible) && (_ownerGuid.IsEmpty || _isOwnerInvisible))
            {
                _isOwnerInvisible = playerInfo.IsInvisible;

                SetOwner(guid, !newChannel && !_isOwnerInvisible);
                _playersStore[guid].SetModerator(true);
            }
    }

    public void Kick(Player player, string badname)
    {
        KickOrBan(player, badname, false);
    }

    public void LeaveChannel(Player player, bool send = true, bool suspend = false)
    {
        var guid = player.GUID;

        if (!IsOn(guid))
        {
            if (send)
            {
                var builder = new ChannelNameBuilder(this, new NotMemberAppend(), _worldManager);
                SendToOne(builder, guid);
            }

            return;
        }

        player.LeftChannel(this);

        if (send)
            /*
            ChannelNameBuilder<YouLeftAppend> builder = new ChannelNameBuilder(this, new YouLeftAppend());
            SendToOne(builder, guid);
            */
            SendToOne(new ChannelNotifyLeftBuilder(this, suspend, _worldManager), guid);

        var info = _playersStore.LookupByKey(guid);
        var changeowner = info.IsOwner;
        _playersStore.Remove(guid);

        if (IsAnnounce && !player.Session.HasPermission(RBACPermissions.SilentlyJoinChannel))
        {
            var builder = new ChannelNameBuilder(this, new LeftAppend(guid), _worldManager);
            SendToAll(builder);
        }

        LeaveNotify(player);

        if (IsConstant)
            return;

        // If the channel owner left and there are still playersStore inside, pick a new owner
        // do not pick invisible gm owner unless there are only invisible gms in that channel (rare)
        if (!changeowner || !_ownershipEnabled || _playersStore.Empty())
            return;

        var newowner = ObjectGuid.Empty;

        foreach (var key in _playersStore.Keys.Where(key => !_playersStore[key].IsInvisible))
        {
            newowner = key;

            break;
        }

        if (newowner.IsEmpty)
            newowner = _playersStore.First().Key;

        _playersStore[newowner].SetModerator(true);

        SetOwner(newowner);

        // if the new owner is invisible gm, set Id to automatically choose a new owner
        if (_playersStore[newowner].IsInvisible)
            _isOwnerInvisible = true;
    }

    public void List(Player player)
    {
        var guid = player.GUID;

        if (!IsOn(guid))
        {
            ChannelNameBuilder builder = new(this, new NotMemberAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        var channelName = GetName(player.Session.SessionDbcLocale);
        Log.Logger.Debug("SMSG_CHANNEL_LIST {0} Channel: {1}", player.Session.GetPlayerInfo(), channelName);

        ChannelListResponse list = new()
        {
            Display = true, // always true?
            Channel = channelName,
            ChannelFlags = Flags
        };

        var gmLevelInWhoList = _configuration.GetDefaultValue("GM:InWhoList:Level", (int)AccountTypes.Administrator);

        foreach (var pair in _playersStore)
        {
            var member = _objectAccessor.FindConnectedPlayer(pair.Key);

            // PLAYER can't see MODERATOR, GAME MASTER, ADMINISTRATOR characters
            // MODERATOR, GAME MASTER, ADMINISTRATOR can see all
            if (member &&
                (player.Session.HasPermission(RBACPermissions.WhoSeeAllSecLevels) ||
                 member.Session.Security <= (AccountTypes)gmLevelInWhoList) &&
                member.IsVisibleGloballyFor(player))
                list.Members.Add(new ChannelListResponse.ChannelPlayer(pair.Key, WorldManager.Realm.Id.VirtualRealmAddress, pair.Value.Flags));
        }

        player.SendPacket(list);
    }

    public void Password(Player player, string pass)
    {
        var guid = player.GUID;

        if (!IsOn(guid))
        {
            ChannelNameBuilder builder = new(this, new NotMemberAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        var info = _playersStore.LookupByKey(guid);

        if (!info.IsModerator && !player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator))
        {
            ChannelNameBuilder builder = new(this, new NotModeratorAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        _channelPassword = pass;

        ChannelNameBuilder builder1 = new(this, new PasswordChangedAppend(guid), _worldManager);
        SendToAll(builder1);

        _isDirty = true;
    }

    public void Say(ObjectGuid guid, string what, Language lang)
    {
        if (string.IsNullOrEmpty(what))
            return;

        // TODO: Add proper RBAC check
        if (_configuration.GetDefaultValue("AllowTwoSide:Interaction:Channel", false))
            lang = Language.Universal;

        if (!IsOn(guid))
        {
            ChannelNameBuilder builder = new(this, new NotMemberAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        var playerInfo = _playersStore.LookupByKey(guid);

        if (playerInfo.IsMuted)
        {
            ChannelNameBuilder builder = new(this, new MutedAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        var player = _objectAccessor.FindConnectedPlayer(guid);
        SendToAll(new ChannelSayBuilder(this, lang, what, guid, GUID, _worldManager, _objectAccessor), !playerInfo.IsModerator ? guid : ObjectGuid.Empty, !playerInfo.IsModerator && player ? player.Session.AccountGUID : ObjectGuid.Empty);
    }

    public void SendWhoOwner(Player player)
    {
        var guid = player.GUID;

        if (IsOn(guid))
        {
            ChannelNameBuilder builder = new(this, new ChannelOwnerAppend(this, _ownerGuid, _characterCache), _worldManager);
            SendToOne(builder, guid);
        }
        else
        {
            ChannelNameBuilder builder = new(this, new NotMemberAppend(), _worldManager);
            SendToOne(builder, guid);
        }
    }

    public void SetAnnounce(bool announce)
    {
        IsAnnounce = announce;
    }

    // will be saved to DB on next channel save interval
    public void SetDirty()
    {
        _isDirty = true;
    }

    public void SetInvisible(Player player, bool on)
    {
        if (!_playersStore.TryGetValue(player.GUID, out var playerInfo))
            return;

        playerInfo.SetInvisible(on);

        // we happen to be owner too, update Id
        if (_ownerGuid == player.GUID)
            _isOwnerInvisible = on;
    }

    public void SetModerator(Player player, string newname)
    {
        SetMode(player, newname, true, true);
    }

    public void SetMute(Player player, string newname)
    {
        SetMode(player, newname, false, true);
    }

    public void SetOwner(Player player, string newname)
    {
        var guid = player.GUID;

        if (!IsOn(guid))
        {
            ChannelNameBuilder builder = new(this, new NotMemberAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        if (!player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator) && guid != _ownerGuid)
        {
            ChannelNameBuilder builder = new(this, new NotOwnerAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        var newp = _objectAccessor.FindPlayerByName(newname);
        var victim = newp ? newp.GUID : ObjectGuid.Empty;

        if (newp == null ||
            victim.IsEmpty ||
            !IsOn(victim) ||
            (player.Team != newp.Team &&
             (!player.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
              !newp.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel))))
        {
            ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(newname), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        _playersStore[victim].SetModerator(true);
        SetOwner(victim);
    }

    public void SetOwner(ObjectGuid guid, bool exclaim = true)
    {
        if (_playersStore.TryGetValue(_ownerGuid, out var playerInfo) && !_ownerGuid.IsEmpty)
            playerInfo.SetOwner(false);

        _ownerGuid = guid;

        if (_ownerGuid.IsEmpty)
            return;

        var oldFlag = GetPlayerFlags(_ownerGuid);

        if (playerInfo == null && !_playersStore.TryGetValue(_ownerGuid, out playerInfo))
            return;

        playerInfo.SetModerator(true);
        playerInfo.SetOwner(true);

        ChannelNameBuilder builder = new(this, new ModeChangeAppend(_ownerGuid, oldFlag, GetPlayerFlags(_ownerGuid)), _worldManager);
        SendToAll(builder);

        if (exclaim)
        {
            ChannelNameBuilder ownerBuilder = new(this, new OwnerChangedAppend(_ownerGuid), _worldManager);
            SendToAll(ownerBuilder);
        }

        _isDirty = true;
    }

    public void SetOwnership(bool ownership)
    {
        _ownershipEnabled = ownership;
    }

    public void SetPassword(string npassword)
    {
        _channelPassword = npassword;
    }

    public void SilenceAll(Player player, string name) { }

    public void UnBan(Player player, string badname)
    {
        var good = player.GUID;

        if (!IsOn(good))
        {
            ChannelNameBuilder builder = new(this, new NotMemberAppend(), _worldManager);
            SendToOne(builder, good);

            return;
        }

        var info = _playersStore.LookupByKey(good);

        if (!info.IsModerator && !player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator))
        {
            ChannelNameBuilder builder = new(this, new NotModeratorAppend(), _worldManager);
            SendToOne(builder, good);

            return;
        }

        var bad = _objectAccessor.FindPlayerByName(badname);
        var victim = bad ? bad.GUID : ObjectGuid.Empty;

        if (victim.IsEmpty || !IsBanned(victim))
        {
            ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(badname), _worldManager);
            SendToOne(builder, good);

            return;
        }

        _bannedStore.Remove(victim);

        ChannelNameBuilder builder1 = new(this, new PlayerUnbannedAppend(good, victim), _worldManager);
        SendToAll(builder1);

        _isDirty = true;
    }

    public void UnsetModerator(Player player, string newname)
    {
        SetMode(player, newname, true, false);
    }

    public void UnsetMute(Player player, string newname)
    {
        SetMode(player, newname, false, false);
    }

    public void UnsilenceAll(Player player, string name) { }

    public void UpdateChannelInDB()
    {
        var now = GameTime.CurrentTime;

        if (_isDirty)
        {
            var banlist = _bannedStore.Aggregate("", (current, iter) => current + (iter.GetRawValue().ToHexString() + ' '));

            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHANNEL);
            stmt.AddValue(0, _channelName);
            stmt.AddValue(1, (uint)_channelTeam);
            stmt.AddValue(2, IsAnnounce);
            stmt.AddValue(3, _ownershipEnabled);
            stmt.AddValue(4, _channelPassword);
            stmt.AddValue(5, banlist);
            _characterDatabase.Execute(stmt);
        }
        else if (_nextActivityUpdateTime <= now)
        {
            if (!_playersStore.Empty())
            {
                var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_CHANNEL_USAGE);
                stmt.AddValue(0, _channelName);
                stmt.AddValue(1, (uint)_channelTeam);
                _characterDatabase.Execute(stmt);
            }
        }
        else
            return;

        _isDirty = false;
        _nextActivityUpdateTime = now + RandomHelper.URand(1 * Time.MINUTE, 6 * Time.MINUTE) * Math.Max(1u, _configuration.GetDefaultValue("PreserveCustomChannelInterval", 5));
    }

    private bool HasFlag(ChannelFlags flag)
    {
        return Flags.HasAnyFlag(flag);
    }

    private bool IsBanned(ObjectGuid guid)
    {
        return _bannedStore.Contains(guid);
    }

    private bool IsOn(ObjectGuid who)
    {
        return _playersStore.ContainsKey(who);
    }

    private void JoinNotify(Player player)
    {
        var guid = player.GUID;

        if (IsConstant)
            SendToAllButOne(new ChannelUserlistAddBuilder(this, guid, _worldManager), guid);
        else
            SendToAll(new ChannelUserlistUpdateBuilder(this, guid, _worldManager));
    }

    private void KickOrBan(Player player, string badname, bool ban)
    {
        var good = player.GUID;

        if (!IsOn(good))
        {
            ChannelNameBuilder builder = new(this, new NotMemberAppend(), _worldManager);
            SendToOne(builder, good);

            return;
        }

        var info = _playersStore.LookupByKey(good);

        if (!info.IsModerator && !player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator))
        {
            ChannelNameBuilder builder = new(this, new NotModeratorAppend(), _worldManager);
            SendToOne(builder, good);

            return;
        }

        var bad = _objectAccessor.FindPlayerByName(badname);
        var victim = bad ? bad.GUID : ObjectGuid.Empty;

        if (bad == null || victim.IsEmpty || !IsOn(victim))
        {
            ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(badname), _worldManager);
            SendToOne(builder, good);

            return;
        }

        var changeowner = _ownerGuid == victim;

        if (!player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator) && changeowner && good != _ownerGuid)
        {
            ChannelNameBuilder builder = new(this, new NotOwnerAppend(), _worldManager);
            SendToOne(builder, good);

            return;
        }

        if (ban && !IsBanned(victim))
        {
            _bannedStore.Add(victim);
            _isDirty = true;

            if (!player.Session.HasPermission(RBACPermissions.SilentlyJoinChannel))
            {
                ChannelNameBuilder builder = new(this, new PlayerBannedAppend(good, victim), _worldManager);
                SendToAll(builder);
            }
        }
        else if (!player.Session.HasPermission(RBACPermissions.SilentlyJoinChannel))
        {
            ChannelNameBuilder builder = new(this, new PlayerKickedAppend(good, victim), _worldManager);
            SendToAll(builder);
        }

        _playersStore.Remove(victim);
        bad.LeftChannel(this);

        if (!changeowner || !_ownershipEnabled || _playersStore.Empty())
            return;

        info.SetModerator(true);
        SetOwner(good);
    }

    private void LeaveNotify(Player player)
    {
        var guid = player.GUID;

        var builder = new ChannelUserlistRemoveBuilder(this, guid, _worldManager);

        if (IsConstant)
            SendToAllButOne(builder, guid);
        else
            SendToAll(builder);
    }

    private void SendToAll(MessageBuilder builder, ObjectGuid guid = default, ObjectGuid accountGuid = default)
    {
        LocalizedDo localizer = new(builder);

        foreach (var pair in _playersStore)
        {
            var player = _objectAccessor.FindConnectedPlayer(pair.Key);

            if (!player)
                continue;

            if (guid.IsEmpty || !player.Social.HasIgnore(guid, accountGuid))
                localizer.Invoke(player);
        }
    }

    private void SendToAllButOne(MessageBuilder builder, ObjectGuid who)
    {
        LocalizedDo localizer = new(builder);

        foreach (var pair in _playersStore)
            if (pair.Key != who)
            {
                var player = _objectAccessor.FindConnectedPlayer(pair.Key);

                if (player)
                    localizer.Invoke(player);
            }
    }

    private void SendToAllWithAddon(MessageBuilder builder, string addonPrefix, ObjectGuid guid = default, ObjectGuid accountGuid = default)
    {
        LocalizedDo localizer = new(builder);

        foreach (var pair in _playersStore)
        {
            var player = _objectAccessor.FindConnectedPlayer(pair.Key);

            if (!player)
                continue;

            if (player.Session.IsAddonRegistered(addonPrefix) && (guid.IsEmpty || !player.Social.HasIgnore(guid, accountGuid)))
                localizer.Invoke(player);
        }
    }

    private void SendToOne(MessageBuilder builder, ObjectGuid who)
    {
        LocalizedDo localizer = new(builder);

        var player = _objectAccessor.FindConnectedPlayer(who);

        if (player)
            localizer.Invoke(player);
    }

    private void SetMode(Player player, string p2N, bool mod, bool set)
    {
        var guid = player.GUID;

        if (!IsOn(guid))
        {
            ChannelNameBuilder builder = new(this, new NotMemberAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        var info = _playersStore.LookupByKey(guid);

        if (!info.IsModerator && !player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator))
        {
            ChannelNameBuilder builder = new(this, new NotModeratorAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        if (guid == _ownerGuid && p2N == player.GetName() && mod)
            return;

        var newp = _objectAccessor.FindPlayerByName(p2N);
        var victim = newp ? newp.GUID : ObjectGuid.Empty;

        if (newp == null ||
            victim.IsEmpty ||
            !IsOn(victim) ||
            (player.Team != newp.Team &&
             (!player.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
              !newp.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel))))
        {
            ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(p2N), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        if (_ownerGuid == victim && _ownerGuid != guid)
        {
            ChannelNameBuilder builder = new(this, new NotOwnerAppend(), _worldManager);
            SendToOne(builder, guid);

            return;
        }

        if (mod)
            SetModerator(newp.GUID, set);
        else
            SetMute(newp.GUID, set);
    }

    private void SetModerator(ObjectGuid guid, bool set)
    {
        if (!IsOn(guid))
            return;

        var playerInfo = _playersStore.LookupByKey(guid);

        if (playerInfo.IsModerator == set)
            return;

        var oldFlag = _playersStore[guid].Flags;
        playerInfo.SetModerator(set);

        ChannelNameBuilder builder = new(this, new ModeChangeAppend(guid, oldFlag, playerInfo.Flags), _worldManager);
        SendToAll(builder);
    }

    private void SetMute(ObjectGuid guid, bool set)
    {
        if (!IsOn(guid))
            return;

        var playerInfo = _playersStore.LookupByKey(guid);

        if (playerInfo.IsMuted == set)
            return;

        var oldFlag = _playersStore[guid].Flags;
        playerInfo.SetMuted(set);

        ChannelNameBuilder builder = new(this, new ModeChangeAppend(guid, oldFlag, playerInfo.Flags), _worldManager);
        SendToAll(builder);
    }

    public class PlayerInfo
    {
        public ChannelMemberFlags Flags { get; private set; }

        public bool IsInvisible { get; private set; }

        public bool IsModerator => HasFlag(ChannelMemberFlags.Moderator);

        public bool IsMuted => HasFlag(ChannelMemberFlags.Muted);

        public bool IsOwner => HasFlag(ChannelMemberFlags.Owner);

        public bool HasFlag(ChannelMemberFlags flag)
        {
            return Flags.HasAnyFlag(flag);
        }

        public void RemoveFlag(ChannelMemberFlags flag)
        {
            Flags &= ~flag;
        }

        public void SetFlag(ChannelMemberFlags flag)
        {
            Flags |= flag;
        }

        public void SetInvisible(bool on)
        {
            IsInvisible = on;
        }

        public void SetModerator(bool state)
        {
            if (state)
                SetFlag(ChannelMemberFlags.Moderator);
            else
                RemoveFlag(ChannelMemberFlags.Moderator);
        }

        public void SetMuted(bool state)
        {
            if (state)
                SetFlag(ChannelMemberFlags.Muted);
            else
                RemoveFlag(ChannelMemberFlags.Muted);
        }

        public void SetOwner(bool state)
        {
            if (state)
                SetFlag(ChannelMemberFlags.Owner);
            else
                RemoveFlag(ChannelMemberFlags.Owner);
        }
    }
}