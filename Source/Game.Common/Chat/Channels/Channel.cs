// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Common.DataStorage.Structs.A;
using Game.Common.DoWork;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Players;
using Game.Common.Networking.Packets.Channel;
using Game.Common.Server;
using Game.Common.Text;

namespace Game.Common.Chat.Channels;

public class Channel
{
	readonly ChannelFlags _channelFlags;
	readonly uint _channelId;
	readonly TeamFaction _channelTeam;
	readonly string _channelName;
	readonly Dictionary<ObjectGuid, PlayerInfo> _playersStore = new();
	readonly List<ObjectGuid> _bannedStore = new();
	readonly AreaTableRecord _zoneEntry;
	readonly ObjectGuid _channelGuid;

	bool _isDirty; // whether the channel needs to be saved to DB
	long _nextActivityUpdateTime;

	bool _announceEnabled;
	bool _ownershipEnabled;
	bool _isOwnerInvisible;
	ObjectGuid _ownerGuid;
	string _channelPassword;

	public Channel(ObjectGuid guid, uint channelId, TeamFaction team = 0, AreaTableRecord zoneEntry = null)
	{
		_channelFlags = ChannelFlags.General;
		_channelId = channelId;
		_channelTeam = team;
		_channelGuid = guid;
		_zoneEntry = zoneEntry;

		var channelEntry = CliDB.ChatChannelsStorage.LookupByKey(channelId);

		if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Trade)) // for trade channel
			_channelFlags |= ChannelFlags.Trade;

		if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly2)) // for city only channels
			_channelFlags |= ChannelFlags.City;

		if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Lfg)) // for LFG channel
			_channelFlags |= ChannelFlags.Lfg;
		else // for all other channels
			_channelFlags |= ChannelFlags.NotLfg;
	}

	public Channel(ObjectGuid guid, string name, TeamFaction team = 0, string banList = "")
	{
		_announceEnabled = true;
		_ownershipEnabled = true;
		_channelFlags = ChannelFlags.Custom;
		_channelTeam = team;
		_channelGuid = guid;
		_channelName = name;

		StringArray tokens = new(banList, ' ');

		foreach (string token in tokens)
		{
			// legacy db content might not have 0x prefix, account for that
			var bannedGuidStr = token.Contains("0x") ? token.Substring(2) : token;
			ObjectGuid banned = new();
			banned.SetRawValue(ulong.Parse(bannedGuidStr.Substring(0, 16)), ulong.Parse(bannedGuidStr.Substring(16)));

			if (banned.IsEmpty)
				continue;

			Log.outDebug(LogFilter.ChatSystem, $"Channel({name}) loaded player {banned} into bannedStore");
			_bannedStore.Add(banned);
		}
	}

	public static void GetChannelName(ref string channelName, uint channelId, Locale locale, AreaTableRecord zoneEntry)
	{
		if (channelId != 0)
		{
			var channelEntry = CliDB.ChatChannelsStorage.LookupByKey(channelId);

			if (!channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.Global))
			{
				if (channelEntry.Flags.HasAnyFlag(ChannelDBCFlags.CityOnly))
					channelName = string.Format(channelEntry.Name[locale].ConvertFormatSyntax(), Global.ObjectMgr.GetCypherString(CypherStrings.ChannelCity, locale));
				else
					channelName = string.Format(channelEntry.Name[locale].ConvertFormatSyntax(), zoneEntry.AreaName[locale]);
			}
			else
			{
				channelName = channelEntry.Name[locale];
			}
		}
	}

	public string GetName(Locale locale = Locale.enUS)
	{
		var result = _channelName;
		GetChannelName(ref result, _channelId, locale, _zoneEntry);

		return result;
	}

	public void UpdateChannelInDB()
	{
		var now = GameTime.GetGameTime();

		if (_isDirty)
		{
			var banlist = "";

			foreach (var iter in _bannedStore)
				banlist += iter.GetRawValue().ToHexString() + ' ';

			var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL);
			stmt.AddValue(0, _channelName);
			stmt.AddValue(1, (uint)_channelTeam);
			stmt.AddValue(2, _announceEnabled);
			stmt.AddValue(3, _ownershipEnabled);
			stmt.AddValue(4, _channelPassword);
			stmt.AddValue(5, banlist);
			DB.Characters.Execute(stmt);
		}
		else if (_nextActivityUpdateTime <= now)
		{
			if (!_playersStore.Empty())
			{
				var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHANNEL_USAGE);
				stmt.AddValue(0, _channelName);
				stmt.AddValue(1, (uint)_channelTeam);
				DB.Characters.Execute(stmt);
			}
		}
		else
		{
			return;
		}

		_isDirty = false;
		_nextActivityUpdateTime = now + RandomHelper.URand(1 * Time.Minute, 6 * Time.Minute) * Math.Max(1u, WorldConfig.GetUIntValue(WorldCfg.PreserveCustomChannelInterval));
	}

	public void JoinChannel(Player player, string pass = "")
	{
		var guid = player.GUID;

		if (IsOn(guid))
		{
			// Do not send error message for built-in channels
			if (!IsConstant())
			{
				var builder = new ChannelNameBuilder(this, new PlayerAlreadyMemberAppend(guid));
				SendToOne(builder, guid);
			}

			return;
		}

		if (IsBanned(guid))
		{
			var builder = new ChannelNameBuilder(this, new BannedAppend());
			SendToOne(builder, guid);

			return;
		}

		if (!CheckPassword(pass))
		{
			var builder = new ChannelNameBuilder(this, new WrongPasswordAppend());
			SendToOne(builder, guid);

			return;
		}

		if (HasFlag(ChannelFlags.Lfg) &&
			WorldConfig.GetBoolValue(WorldCfg.RestrictedLfgChannel) &&
			Global.AccountMgr.IsPlayerAccount(player.Session.Security) && //FIXME: Move to RBAC
			player.Group)
		{
			var builder = new ChannelNameBuilder(this, new NotInLFGAppend());
			SendToOne(builder, guid);

			return;
		}

		player.JoinedChannel(this);

		if (_announceEnabled && !player.Session.HasPermission(RBACPermissions.SilentlyJoinChannel))
		{
			var builder = new ChannelNameBuilder(this, new JoinedAppend(guid));
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

		SendToOne(new ChannelNotifyJoinedBuilder(this), guid);

		JoinNotify(player);

		// Custom channel handling
		if (!IsConstant())
			// If the channel has no owner yet and ownership is allowed, set the new owner.
			// or if the owner was a GM with .gm visible off
			// don't do this if the new player is, too, an invis GM, unless the channel was empty
			if (_ownershipEnabled && (newChannel || !playerInfo.IsInvisible()) && (_ownerGuid.IsEmpty || _isOwnerInvisible))
			{
				_isOwnerInvisible = playerInfo.IsInvisible();

				SetOwner(guid, !newChannel && !_isOwnerInvisible);
				_playersStore[guid].SetModerator(true);
			}
	}

	public void LeaveChannel(Player player, bool send = true, bool suspend = false)
	{
		var guid = player.GUID;

		if (!IsOn(guid))
		{
			if (send)
			{
				var builder = new ChannelNameBuilder(this, new NotMemberAppend());
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
			SendToOne(new ChannelNotifyLeftBuilder(this, suspend), guid);

		var info = _playersStore.LookupByKey(guid);
		var changeowner = info.IsOwner();
		_playersStore.Remove(guid);

		if (_announceEnabled && !player.Session.HasPermission(RBACPermissions.SilentlyJoinChannel))
		{
			var builder = new ChannelNameBuilder(this, new LeftAppend(guid));
			SendToAll(builder);
		}

		LeaveNotify(player);

		if (!IsConstant())
			// If the channel owner left and there are still playersStore inside, pick a new owner
			// do not pick invisible gm owner unless there are only invisible gms in that channel (rare)
			if (changeowner && _ownershipEnabled && !_playersStore.Empty())
			{
				var newowner = ObjectGuid.Empty;

				foreach (var key in _playersStore.Keys)
					if (!_playersStore[key].IsInvisible())
					{
						newowner = key;

						break;
					}

				if (newowner.IsEmpty)
					newowner = _playersStore.First().Key;

				_playersStore[newowner].SetModerator(true);

				SetOwner(newowner);

				// if the new owner is invisible gm, set flag to automatically choose a new owner
				if (_playersStore[newowner].IsInvisible())
					_isOwnerInvisible = true;
			}
	}

	public void UnBan(Player player, string badname)
	{
		var good = player.GUID;

		if (!IsOn(good))
		{
			ChannelNameBuilder builder = new(this, new NotMemberAppend());
			SendToOne(builder, good);

			return;
		}

		var info = _playersStore.LookupByKey(good);

		if (!info.IsModerator() && !player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator))
		{
			ChannelNameBuilder builder = new(this, new NotModeratorAppend());
			SendToOne(builder, good);

			return;
		}

		var bad = Global.ObjAccessor.FindPlayerByName(badname);
		var victim = bad ? bad.GUID : ObjectGuid.Empty;

		if (victim.IsEmpty || !IsBanned(victim))
		{
			ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(badname));
			SendToOne(builder, good);

			return;
		}

		_bannedStore.Remove(victim);

		ChannelNameBuilder builder1 = new(this, new PlayerUnbannedAppend(good, victim));
		SendToAll(builder1);

		_isDirty = true;
	}

	public void Password(Player player, string pass)
	{
		var guid = player.GUID;

		if (!IsOn(guid))
		{
			ChannelNameBuilder builder = new(this, new NotMemberAppend());
			SendToOne(builder, guid);

			return;
		}

		var info = _playersStore.LookupByKey(guid);

		if (!info.IsModerator() && !player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator))
		{
			ChannelNameBuilder builder = new(this, new NotModeratorAppend());
			SendToOne(builder, guid);

			return;
		}

		_channelPassword = pass;

		ChannelNameBuilder builder1 = new(this, new PasswordChangedAppend(guid));
		SendToAll(builder1);

		_isDirty = true;
	}

	public void SetInvisible(Player player, bool on)
	{
		var playerInfo = _playersStore.LookupByKey(player.GUID);

		if (playerInfo == null)
			return;

		playerInfo.SetInvisible(on);

		// we happen to be owner too, update flag
		if (_ownerGuid == player.GUID)
			_isOwnerInvisible = on;
	}

	public void SetOwner(Player player, string newname)
	{
		var guid = player.GUID;

		if (!IsOn(guid))
		{
			ChannelNameBuilder builder = new(this, new NotMemberAppend());
			SendToOne(builder, guid);

			return;
		}

		if (!player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator) && guid != _ownerGuid)
		{
			ChannelNameBuilder builder = new(this, new NotOwnerAppend());
			SendToOne(builder, guid);

			return;
		}

		var newp = Global.ObjAccessor.FindPlayerByName(newname);
		var victim = newp ? newp.GUID : ObjectGuid.Empty;

		if (newp == null ||
			victim.IsEmpty ||
			!IsOn(victim) ||
			(player.Team != newp.Team &&
			(!player.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
			!newp.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel))))
		{
			ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(newname));
			SendToOne(builder, guid);

			return;
		}

		_playersStore[victim].SetModerator(true);
		SetOwner(victim);
	}

	public void SendWhoOwner(Player player)
	{
		var guid = player.GUID;

		if (IsOn(guid))
		{
			ChannelNameBuilder builder = new(this, new ChannelOwnerAppend(this, _ownerGuid));
			SendToOne(builder, guid);
		}
		else
		{
			ChannelNameBuilder builder = new(this, new NotMemberAppend());
			SendToOne(builder, guid);
		}
	}

	public void List(Player player)
	{
		var guid = player.GUID;

		if (!IsOn(guid))
		{
			ChannelNameBuilder builder = new(this, new NotMemberAppend());
			SendToOne(builder, guid);

			return;
		}

		var channelName = GetName(player.Session.SessionDbcLocale);
		Log.outDebug(LogFilter.ChatSystem, "SMSG_CHANNEL_LIST {0} Channel: {1}", player.Session.GetPlayerInfo(), channelName);

		ChannelListResponse list = new();
		list.Display = true; // always true?
		list.Channel = channelName;
		list.ChannelFlags = GetFlags();

		var gmLevelInWhoList = WorldConfig.GetUIntValue(WorldCfg.GmLevelInWhoList);

		foreach (var pair in _playersStore)
		{
			var member = Global.ObjAccessor.FindConnectedPlayer(pair.Key);

			// PLAYER can't see MODERATOR, GAME MASTER, ADMINISTRATOR characters
			// MODERATOR, GAME MASTER, ADMINISTRATOR can see all
			if (member &&
				(player.Session.HasPermission(RBACPermissions.WhoSeeAllSecLevels) ||
				member.Session.Security <= (AccountTypes)gmLevelInWhoList) &&
				member.IsVisibleGloballyFor(player))
				list.Members.Add(new ChannelListResponse.ChannelPlayer(pair.Key, Global.WorldMgr.VirtualRealmAddress, pair.Value.GetFlags()));
		}

		player.SendPacket(list);
	}

	public void Announce(Player player)
	{
		var guid = player.GUID;

		if (!IsOn(guid))
		{
			ChannelNameBuilder builder = new(this, new NotMemberAppend());
			SendToOne(builder, guid);

			return;
		}

		var playerInfo = _playersStore.LookupByKey(guid);

		if (!playerInfo.IsModerator() && !player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator))
		{
			ChannelNameBuilder builder = new(this, new NotModeratorAppend());
			SendToOne(builder, guid);

			return;
		}

		_announceEnabled = !_announceEnabled;

		if (_announceEnabled)
		{
			ChannelNameBuilder builder = new(this, new AnnouncementsOnAppend(guid));
			SendToAll(builder);
		}
		else
		{
			ChannelNameBuilder builder = new(this, new AnnouncementsOffAppend(guid));
			SendToAll(builder);
		}

		_isDirty = true;
	}

	public void Say(ObjectGuid guid, string what, Language lang)
	{
		if (string.IsNullOrEmpty(what))
			return;

		// TODO: Add proper RBAC check
		if (WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionChannel))
			lang = Language.Universal;

		if (!IsOn(guid))
		{
			ChannelNameBuilder builder = new(this, new NotMemberAppend());
			SendToOne(builder, guid);

			return;
		}

		var playerInfo = _playersStore.LookupByKey(guid);

		if (playerInfo.IsMuted())
		{
			ChannelNameBuilder builder = new(this, new MutedAppend());
			SendToOne(builder, guid);

			return;
		}

		var player = Global.ObjAccessor.FindConnectedPlayer(guid);
		SendToAll(new ChannelSayBuilder(this, lang, what, guid, _channelGuid), !playerInfo.IsModerator() ? guid : ObjectGuid.Empty, !playerInfo.IsModerator() && player ? player.Session.AccountGUID : ObjectGuid.Empty);
	}

	public void AddonSay(ObjectGuid guid, string prefix, string what, bool isLogged)
	{
		if (what.IsEmpty())
			return;

		if (!IsOn(guid))
		{
			NotMemberAppend appender;
			ChannelNameBuilder builder = new(this, appender);
			SendToOne(builder, guid);

			return;
		}

		var playerInfo = _playersStore.LookupByKey(guid);

		if (playerInfo.IsMuted())
		{
			MutedAppend appender;
			ChannelNameBuilder builder = new(this, appender);
			SendToOne(builder, guid);

			return;
		}

		var player = Global.ObjAccessor.FindConnectedPlayer(guid);

		SendToAllWithAddon(new ChannelWhisperBuilder(this, isLogged ? Language.AddonLogged : Language.Addon, what, prefix, guid),
							prefix,
							!playerInfo.IsModerator() ? guid : ObjectGuid.Empty,
							!playerInfo.IsModerator() && player ? player.Session.AccountGUID : ObjectGuid.Empty);
	}

	public void Invite(Player player, string newname)
	{
		var guid = player.GUID;

		if (!IsOn(guid))
		{
			ChannelNameBuilder builder = new(this, new NotMemberAppend());
			SendToOne(builder, guid);

			return;
		}

		var newp = Global.ObjAccessor.FindPlayerByName(newname);

		if (!newp || !newp.IsGMVisible)
		{
			ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(newname));
			SendToOne(builder, guid);

			return;
		}

		if (IsBanned(newp.GUID))
		{
			ChannelNameBuilder builder = new(this, new PlayerInviteBannedAppend(newname));
			SendToOne(builder, guid);

			return;
		}

		if (newp.Team != player.Team &&
			(!player.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
			!newp.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel)))
		{
			ChannelNameBuilder builder = new(this, new InviteWrongFactionAppend());
			SendToOne(builder, guid);

			return;
		}

		if (IsOn(newp.GUID))
		{
			ChannelNameBuilder builder = new(this, new PlayerAlreadyMemberAppend(newp.GUID));
			SendToOne(builder, guid);

			return;
		}

		if (!newp.Social.HasIgnore(guid, player.Session.AccountGUID))
		{
			ChannelNameBuilder builder = new(this, new InviteAppend(guid));
			SendToOne(builder, newp.GUID);
		}

		ChannelNameBuilder builder1 = new(this, new PlayerInvitedAppend(newp.GetName()));
		SendToOne(builder1, guid);
	}

	public void SetOwner(ObjectGuid guid, bool exclaim = true)
	{
		if (!_ownerGuid.IsEmpty)
		{
			// [] will re-add player after it possible removed
			var playerInfo = _playersStore.LookupByKey(_ownerGuid);

			if (playerInfo != null)
				playerInfo.SetOwner(false);
		}

		_ownerGuid = guid;

		if (!_ownerGuid.IsEmpty)
		{
			var oldFlag = GetPlayerFlags(_ownerGuid);
			var playerInfo = _playersStore.LookupByKey(_ownerGuid);

			if (playerInfo == null)
				return;

			playerInfo.SetModerator(true);
			playerInfo.SetOwner(true);

			ChannelNameBuilder builder = new(this, new ModeChangeAppend(_ownerGuid, oldFlag, GetPlayerFlags(_ownerGuid)));
			SendToAll(builder);

			if (exclaim)
			{
				ChannelNameBuilder ownerBuilder = new(this, new OwnerChangedAppend(_ownerGuid));
				SendToAll(ownerBuilder);
			}

			_isDirty = true;
		}
	}

	public void SilenceAll(Player player, string name) { }

	public void UnsilenceAll(Player player, string name) { }

	public void DeclineInvite(Player player) { }

	public uint GetChannelId()
	{
		return _channelId;
	}

	public bool IsConstant()
	{
		return _channelId != 0;
	}

	public ObjectGuid GetGUID()
	{
		return _channelGuid;
	}

	public bool IsLFG()
	{
		return GetFlags().HasAnyFlag(ChannelFlags.Lfg);
	}

	public void SetAnnounce(bool announce)
	{
		_announceEnabled = announce;
	}

	// will be saved to DB on next channel save interval
	public void SetDirty()
	{
		_isDirty = true;
	}

	public void SetPassword(string npassword)
	{
		_channelPassword = npassword;
	}

	public bool CheckPassword(string password)
	{
		return _channelPassword.IsEmpty() || (_channelPassword == password);
	}

	public uint GetNumPlayers()
	{
		return (uint)_playersStore.Count;
	}

	public ChannelFlags GetFlags()
	{
		return _channelFlags;
	}

	public AreaTableRecord GetZoneEntry()
	{
		return _zoneEntry;
	}

	public void Kick(Player player, string badname)
	{
		KickOrBan(player, badname, false);
	}

	public void Ban(Player player, string badname)
	{
		KickOrBan(player, badname, true);
	}

	public void SetModerator(Player player, string newname)
	{
		SetMode(player, newname, true, true);
	}

	public void UnsetModerator(Player player, string newname)
	{
		SetMode(player, newname, true, false);
	}

	public void SetMute(Player player, string newname)
	{
		SetMode(player, newname, false, true);
	}

	public void UnsetMute(Player player, string newname)
	{
		SetMode(player, newname, false, false);
	}

	public void SetOwnership(bool ownership)
	{
		_ownershipEnabled = ownership;
	}

	public ChannelMemberFlags GetPlayerFlags(ObjectGuid guid)
	{
		var info = _playersStore.LookupByKey(guid);

		return info != null ? info.GetFlags() : 0;
	}

	void KickOrBan(Player player, string badname, bool ban)
	{
		var good = player.GUID;

		if (!IsOn(good))
		{
			ChannelNameBuilder builder = new(this, new NotMemberAppend());
			SendToOne(builder, good);

			return;
		}

		var info = _playersStore.LookupByKey(good);

		if (!info.IsModerator() && !player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator))
		{
			ChannelNameBuilder builder = new(this, new NotModeratorAppend());
			SendToOne(builder, good);

			return;
		}

		var bad = Global.ObjAccessor.FindPlayerByName(badname);
		var victim = bad ? bad.GUID : ObjectGuid.Empty;

		if (bad == null || victim.IsEmpty || !IsOn(victim))
		{
			ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(badname));
			SendToOne(builder, good);

			return;
		}

		var changeowner = _ownerGuid == victim;

		if (!player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator) && changeowner && good != _ownerGuid)
		{
			ChannelNameBuilder builder = new(this, new NotOwnerAppend());
			SendToOne(builder, good);

			return;
		}

		if (ban && !IsBanned(victim))
		{
			_bannedStore.Add(victim);
			_isDirty = true;

			if (!player.Session.HasPermission(RBACPermissions.SilentlyJoinChannel))
			{
				ChannelNameBuilder builder = new(this, new PlayerBannedAppend(good, victim));
				SendToAll(builder);
			}
		}
		else if (!player.Session.HasPermission(RBACPermissions.SilentlyJoinChannel))
		{
			ChannelNameBuilder builder = new(this, new PlayerKickedAppend(good, victim));
			SendToAll(builder);
		}

		_playersStore.Remove(victim);
		bad.LeftChannel(this);

		if (changeowner && _ownershipEnabled && !_playersStore.Empty())
		{
			info.SetModerator(true);
			SetOwner(good);
		}
	}

	void SetMode(Player player, string p2n, bool mod, bool set)
	{
		var guid = player.GUID;

		if (!IsOn(guid))
		{
			ChannelNameBuilder builder = new(this, new NotMemberAppend());
			SendToOne(builder, guid);

			return;
		}

		var info = _playersStore.LookupByKey(guid);

		if (!info.IsModerator() && !player.Session.HasPermission(RBACPermissions.ChangeChannelNotModerator))
		{
			ChannelNameBuilder builder = new(this, new NotModeratorAppend());
			SendToOne(builder, guid);

			return;
		}

		if (guid == _ownerGuid && p2n == player.GetName() && mod)
			return;

		var newp = Global.ObjAccessor.FindPlayerByName(p2n);
		var victim = newp ? newp.GUID : ObjectGuid.Empty;

		if (newp == null ||
			victim.IsEmpty ||
			!IsOn(victim) ||
			(player.Team != newp.Team &&
			(!player.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel) ||
			!newp.Session.HasPermission(RBACPermissions.TwoSideInteractionChannel))))
		{
			ChannelNameBuilder builder = new(this, new PlayerNotFoundAppend(p2n));
			SendToOne(builder, guid);

			return;
		}

		if (_ownerGuid == victim && _ownerGuid != guid)
		{
			ChannelNameBuilder builder = new(this, new NotOwnerAppend());
			SendToOne(builder, guid);

			return;
		}

		if (mod)
			SetModerator(newp.GUID, set);
		else
			SetMute(newp.GUID, set);
	}

	void JoinNotify(Player player)
	{
		var guid = player.GUID;

		if (IsConstant())
			SendToAllButOne(new ChannelUserlistAddBuilder(this, guid), guid);
		else
			SendToAll(new ChannelUserlistUpdateBuilder(this, guid));
	}

	void LeaveNotify(Player player)
	{
		var guid = player.GUID;

		var builder = new ChannelUserlistRemoveBuilder(this, guid);

		if (IsConstant())
			SendToAllButOne(builder, guid);
		else
			SendToAll(builder);
	}

	void SetModerator(ObjectGuid guid, bool set)
	{
		if (!IsOn(guid))
			return;

		var playerInfo = _playersStore.LookupByKey(guid);

		if (playerInfo.IsModerator() != set)
		{
			var oldFlag = _playersStore[guid].GetFlags();
			playerInfo.SetModerator(set);

			ChannelNameBuilder builder = new(this, new ModeChangeAppend(guid, oldFlag, playerInfo.GetFlags()));
			SendToAll(builder);
		}
	}

	void SetMute(ObjectGuid guid, bool set)
	{
		if (!IsOn(guid))
			return;

		var playerInfo = _playersStore.LookupByKey(guid);

		if (playerInfo.IsMuted() != set)
		{
			var oldFlag = _playersStore[guid].GetFlags();
			playerInfo.SetMuted(set);

			ChannelNameBuilder builder = new(this, new ModeChangeAppend(guid, oldFlag, playerInfo.GetFlags()));
			SendToAll(builder);
		}
	}

	void SendToAll(MessageBuilder builder, ObjectGuid guid = default, ObjectGuid accountGuid = default)
	{
		LocalizedDo localizer = new(builder);

		foreach (var pair in _playersStore)
		{
			var player = Global.ObjAccessor.FindConnectedPlayer(pair.Key);

			if (player)
				if (guid.IsEmpty || !player.Social.HasIgnore(guid, accountGuid))
					localizer.Invoke(player);
		}
	}

	void SendToAllButOne(MessageBuilder builder, ObjectGuid who)
	{
		LocalizedDo localizer = new(builder);

		foreach (var pair in _playersStore)
			if (pair.Key != who)
			{
				var player = Global.ObjAccessor.FindConnectedPlayer(pair.Key);

				if (player)
					localizer.Invoke(player);
			}
	}

	void SendToOne(MessageBuilder builder, ObjectGuid who)
	{
		LocalizedDo localizer = new(builder);

		var player = Global.ObjAccessor.FindConnectedPlayer(who);

		if (player)
			localizer.Invoke(player);
	}

	void SendToAllWithAddon(MessageBuilder builder, string addonPrefix, ObjectGuid guid = default, ObjectGuid accountGuid = default)
	{
		LocalizedDo localizer = new(builder);

		foreach (var pair in _playersStore)
		{
			var player = Global.ObjAccessor.FindConnectedPlayer(pair.Key);

			if (player)
				if (player.Session.IsAddonRegistered(addonPrefix) && (guid.IsEmpty || !player.Social.HasIgnore(guid, accountGuid)))
					localizer.Invoke(player);
		}
	}

	bool IsAnnounce()
	{
		return _announceEnabled;
	}

	bool HasFlag(ChannelFlags flag)
	{
		return _channelFlags.HasAnyFlag(flag);
	}

	bool IsOn(ObjectGuid who)
	{
		return _playersStore.ContainsKey(who);
	}

	bool IsBanned(ObjectGuid guid)
	{
		return _bannedStore.Contains(guid);
	}

	public class PlayerInfo
	{
		ChannelMemberFlags flags;
		bool _invisible;

		public ChannelMemberFlags GetFlags()
		{
			return flags;
		}

		public bool IsInvisible()
		{
			return _invisible;
		}

		public void SetInvisible(bool on)
		{
			_invisible = on;
		}

		public bool HasFlag(ChannelMemberFlags flag)
		{
			return flags.HasAnyFlag(flag);
		}

		public void SetFlag(ChannelMemberFlags flag)
		{
			flags |= flag;
		}

		public void RemoveFlag(ChannelMemberFlags flag)
		{
			flags &= ~flag;
		}

		public bool IsOwner()
		{
			return HasFlag(ChannelMemberFlags.Owner);
		}

		public void SetOwner(bool state)
		{
			if (state)
				SetFlag(ChannelMemberFlags.Owner);
			else
				RemoveFlag(ChannelMemberFlags.Owner);
		}

		public bool IsModerator()
		{
			return HasFlag(ChannelMemberFlags.Moderator);
		}

		public void SetModerator(bool state)
		{
			if (state)
				SetFlag(ChannelMemberFlags.Moderator);
			else
				RemoveFlag(ChannelMemberFlags.Moderator);
		}

		public bool IsMuted()
		{
			return HasFlag(ChannelMemberFlags.Muted);
		}

		public void SetMuted(bool state)
		{
			if (state)
				SetFlag(ChannelMemberFlags.Muted);
			else
				RemoveFlag(ChannelMemberFlags.Muted);
		}
	}
}
