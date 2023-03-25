// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Cache;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Framework.Constants;
using Framework.Database;
using Game.Common.Handlers;
using System;

namespace Forged.RealmServer;

public class CalendarHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly Player _player;
    private readonly CalendarManager _calendarManager;
    private readonly CharacterCache _characterCache;
    private readonly GuildManager _guildManager;
    private readonly CharacterDatabase _characterDatabase;
    private readonly GameTime _gameTime;
    private readonly ObjectAccessor _objectAccessor;
    private readonly InstanceLockManager _instanceLockManager;
    private long _calendarEventCreationCooldown = 0;

    public CalendarHandler(WorldSession session, Player player, CalendarManager calendarManager, InstanceLockManager instanceLockManager,
        CharacterCache characterCache, GuildManager guildManager, ObjectAccessor objectAccessor, CharacterDatabase characterDatabase,
		GameTime gameTime)
    {
        _session = session;
        _player = player;
		_calendarManager = calendarManager;
        _instanceLockManager = instanceLockManager;
        _characterCache = characterCache;
        _guildManager = guildManager;
        _objectAccessor = objectAccessor;
		_characterDatabase = characterDatabase;
        _gameTime = gameTime;
    }

    [WorldPacketHandler(ClientOpcodes.CalendarGet)]
	void HandleCalendarGetCalendar(CalendarGetCalendar calendarGetCalendar)
	{
		var guid = _player.GUID;

		var currTime = _gameTime.GetGameTime;

		CalendarSendCalendar packet = new();
		packet.ServerTime = currTime;

		var invites = _calendarManager.GetPlayerInvites(guid);

		foreach (var invite in invites)
		{
			CalendarSendCalendarInviteInfo inviteInfo = new();
			inviteInfo.EventID = invite.EventId;
			inviteInfo.InviteID = invite.InviteId;
			inviteInfo.InviterGuid = invite.SenderGuid;
			inviteInfo.Status = invite.Status;
			inviteInfo.Moderator = invite.Rank;
			var calendarEvent = _calendarManager.GetEvent(invite.EventId);

			if (calendarEvent != null)
				inviteInfo.InviteType = (byte)(calendarEvent.IsGuildEvent && calendarEvent.GuildId == _player.GuildId ? 1 : 0);

			packet.Invites.Add(inviteInfo);
		}

		var playerEvents = _calendarManager.GetPlayerEvents(guid);

		foreach (var calendarEvent in playerEvents)
		{
			CalendarSendCalendarEventInfo eventInfo;
			eventInfo.EventID = calendarEvent.EventId;
			eventInfo.Date = calendarEvent.Date;
			eventInfo.EventClubID = calendarEvent.GuildId;
			eventInfo.EventName = calendarEvent.Title;
			eventInfo.EventType = calendarEvent.EventType;
			eventInfo.Flags = calendarEvent.Flags;
			eventInfo.OwnerGuid = calendarEvent.OwnerGuid;
			eventInfo.TextureID = calendarEvent.TextureId;

			packet.Events.Add(eventInfo);
		}

		foreach (var instanceLock in _instanceLockManager.GetInstanceLocksForPlayer(_player.GUID))
		{
			CalendarSendCalendarRaidLockoutInfo lockoutInfo = new();

			lockoutInfo.MapID = (int)instanceLock.GetMapId();
			lockoutInfo.DifficultyID = (uint)instanceLock.GetDifficultyId();
			lockoutInfo.ExpireTime = (int)Math.Max((instanceLock.GetEffectiveExpiryTime() - _gameTime.GetSystemTime).TotalSeconds, 0);
			lockoutInfo.InstanceID = instanceLock.GetInstanceId();

			packet.RaidLockouts.Add(lockoutInfo);
		}

        _session.SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarGetEvent)]
	void HandleCalendarGetEvent(CalendarGetEvent calendarGetEvent)
	{
		var calendarEvent = _calendarManager.GetEvent(calendarGetEvent.EventID);

		if (calendarEvent != null)
			_calendarManager.SendCalendarEvent(_player.GUID, calendarEvent, CalendarSendEventType.Get);
		else
			_calendarManager.SendCalendarCommandResult(_player.GUID, CalendarError.EventInvalid);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarCommunityInvite)]
	void HandleCalendarCommunityInvite(CalendarCommunityInviteRequest calendarCommunityInvite)
	{
		var guild = _guildManager.GetGuildById(_player.GuildId);

		if (guild)
			guild.MassInviteToEvent(_session, calendarCommunityInvite.MinLevel, calendarCommunityInvite.MaxLevel, (GuildRankOrder)calendarCommunityInvite.MaxRankOrder);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarAddEvent)]
	void HandleCalendarAddEvent(CalendarAddEvent calendarAddEvent)
	{
		var guid = _player.GUID;

		calendarAddEvent.EventInfo.Time = Time.LocalTimeToUTCTime(calendarAddEvent.EventInfo.Time);

		// prevent events in the past
		// To Do: properly handle timezones and remove the "- time_t(86400L)" hack
		if (calendarAddEvent.EventInfo.Time < (_session._gameTime.GetGameTime - 86400L))
		{
			_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventPassed);

			return;
		}

		// If the event is a guild event, check if the player is in a guild
		if (CalendarEvent.ModifyIsGuildEventFlags(calendarAddEvent.EventInfo.Flags) || CalendarEvent.ModifyIsGuildAnnouncementFlags(calendarAddEvent.EventInfo.Flags))
			if (_player.GuildId == 0)
			{
				_calendarManager.SendCalendarCommandResult(guid, CalendarError.GuildPlayerNotInGuild);

				return;
			}

		// Check if the player reached the max number of events allowed to create
		if (CalendarEvent.ModifyIsGuildEventFlags(calendarAddEvent.EventInfo.Flags) || CalendarEvent.ModifyIsGuildAnnouncementFlags(calendarAddEvent.EventInfo.Flags))
		{
			if (_calendarManager.GetGuildEvents(_player.GuildId).Count >= SharedConst.CalendarMaxGuildEvents)
			{
				_calendarManager.SendCalendarCommandResult(guid, CalendarError.GuildEventsExceeded);

				return;
			}
		}
		else
		{
			if (_calendarManager.GetEventsCreatedBy(guid).Count >= SharedConst.CalendarMaxEvents)
			{
				_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventsExceeded);

				return;
			}
		}

		if (_session.CalendarEventCreationCooldown > _gameTime.GetGameTime)
		{
			_calendarManager.SendCalendarCommandResult(guid, CalendarError.Internal);

			return;
		}

        _session.CalendarEventCreationCooldown = _gameTime.GetGameTime + SharedConst.CalendarCreateEventCooldown;

		CalendarEvent calendarEvent = new(_calendarManager.GetFreeEventId(),
										guid,
										0,
										(CalendarEventType)calendarAddEvent.EventInfo.EventType,
										calendarAddEvent.EventInfo.TextureID,
										calendarAddEvent.EventInfo.Time,
										(CalendarFlags)calendarAddEvent.EventInfo.Flags,
										calendarAddEvent.EventInfo.Title,
										calendarAddEvent.EventInfo.Description,
										0);

		if (calendarEvent.IsGuildEvent || calendarEvent.IsGuildAnnouncement)
			calendarEvent.GuildId = _player.GuildId;

		if (calendarEvent.IsGuildAnnouncement)
		{
			CalendarInvite invite = new(0, calendarEvent.EventId, ObjectGuid.Empty, guid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.NotSignedUp, CalendarModerationRank.Player, "");
			// WARNING: By passing pointer to a local variable, the underlying method(s) must NOT perform any kind
			// of storage of the pointer as it will lead to memory corruption
			_calendarManager.AddInvite(calendarEvent, invite);
		}
		else
		{
			SQLTransaction trans = null;

			if (calendarAddEvent.EventInfo.Invites.Length > 1)
				trans = new SQLTransaction();

			for (var i = 0; i < calendarAddEvent.EventInfo.Invites.Length; ++i)
			{
				CalendarInvite invite = new(_calendarManager.GetFreeInviteId(),
											calendarEvent.EventId,
											calendarAddEvent.EventInfo.Invites[i].Guid,
											guid,
											SharedConst.CalendarDefaultResponseTime,
											(CalendarInviteStatus)calendarAddEvent.EventInfo.Invites[i].Status,
											(CalendarModerationRank)calendarAddEvent.EventInfo.Invites[i].Moderator,
											"");

				_calendarManager.AddInvite(calendarEvent, invite, trans);
			}

			if (calendarAddEvent.EventInfo.Invites.Length > 1)
                _characterDatabase.CommitTransaction(trans);
		}

		_calendarManager.AddEvent(calendarEvent, CalendarSendEventType.Add);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarUpdateEvent)]
	void HandleCalendarUpdateEvent(CalendarUpdateEvent calendarUpdateEvent)
	{
		var guid = _player.GUID;
		long oldEventTime;

		calendarUpdateEvent.EventInfo.Time = Time.LocalTimeToUTCTime(calendarUpdateEvent.EventInfo.Time);

		// prevent events in the past
		// To Do: properly handle timezones and remove the "- time_t(86400L)" hack
		if (calendarUpdateEvent.EventInfo.Time < (_gameTime.GetGameTime - 86400L))
			return;

		var calendarEvent = _calendarManager.GetEvent(calendarUpdateEvent.EventInfo.EventID);

		if (calendarEvent != null)
		{
			oldEventTime = calendarEvent.Date;

			calendarEvent.EventType = (CalendarEventType)calendarUpdateEvent.EventInfo.EventType;
			calendarEvent.Flags = (CalendarFlags)calendarUpdateEvent.EventInfo.Flags;
			calendarEvent.Date = calendarUpdateEvent.EventInfo.Time;
			calendarEvent.TextureId = (int)calendarUpdateEvent.EventInfo.TextureID;
			calendarEvent.Title = calendarUpdateEvent.EventInfo.Title;
			calendarEvent.Description = calendarUpdateEvent.EventInfo.Description;

			_calendarManager.UpdateEvent(calendarEvent);
			_calendarManager.SendCalendarEventUpdateAlert(calendarEvent, oldEventTime);
		}
		else
		{
			_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarRemoveEvent)]
	void HandleCalendarRemoveEvent(CalendarRemoveEvent calendarRemoveEvent)
	{
		var guid = _player.GUID;
		_calendarManager.RemoveEvent(calendarRemoveEvent.EventID, guid);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarCopyEvent)]
	void HandleCalendarCopyEvent(CalendarCopyEvent calendarCopyEvent)
	{
		var guid = _player.GUID;

		calendarCopyEvent.Date = Time.LocalTimeToUTCTime(calendarCopyEvent.Date);

		// prevent events in the past
		// To Do: properly handle timezones and remove the "- time_t(86400L)" hack
		if (calendarCopyEvent.Date < (_gameTime.GetGameTime - 86400L))
		{
			_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventPassed);

			return;
		}

		var oldEvent = _calendarManager.GetEvent(calendarCopyEvent.EventID);

		if (oldEvent != null)
		{
			// Ensure that the player has access to the event
			if (oldEvent.IsGuildEvent || oldEvent.IsGuildAnnouncement)
			{
				if (oldEvent.GuildId != _player.GuildId)
				{
					_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);

					return;
				}
			}
			else
			{
				if (oldEvent.OwnerGuid != guid)
				{
					_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);

					return;
				}
			}

			// Check if the player reached the max number of events allowed to create
			if (oldEvent.IsGuildEvent || oldEvent.IsGuildAnnouncement)
			{
				if (_calendarManager.GetGuildEvents(_player.GuildId).Count >= SharedConst.CalendarMaxGuildEvents)
				{
					_calendarManager.SendCalendarCommandResult(guid, CalendarError.GuildEventsExceeded);

					return;
				}
			}
			else
			{
				if (_calendarManager.GetEventsCreatedBy(guid).Count >= SharedConst.CalendarMaxEvents)
				{
					_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventsExceeded);

					return;
				}
			}

			if (_session.CalendarEventCreationCooldown > _gameTime.GetGameTime)
			{
				_calendarManager.SendCalendarCommandResult(guid, CalendarError.Internal);

				return;
			}

            _session.CalendarEventCreationCooldown = _gameTime.GetGameTime + SharedConst.CalendarCreateEventCooldown;

			CalendarEvent newEvent = new(oldEvent, _calendarManager.GetFreeEventId());
			newEvent.Date = calendarCopyEvent.Date;
			_calendarManager.AddEvent(newEvent, CalendarSendEventType.Copy);

			var invites = _calendarManager.GetEventInvites(calendarCopyEvent.EventID);
			SQLTransaction trans = null;

			if (invites.Count > 1)
				trans = new SQLTransaction();

			foreach (var invite in invites)
				_calendarManager.AddInvite(newEvent, new CalendarInvite(invite, _calendarManager.GetFreeInviteId(), newEvent.EventId), trans);

			if (invites.Count > 1)
                _characterDatabase.CommitTransaction(trans);
			// should we change owner when somebody makes a copy of event owned by another person?
		}
		else
		{
			_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarInvite)]
	void HandleCalendarInvite(CalendarInvitePkt calendarInvite)
	{
		var playerGuid = _player.GUID;

		var inviteeGuid = ObjectGuid.Empty;
		TeamFaction inviteeTeam = 0;
		ulong inviteeGuildId = 0;

		if (!ObjectManager.NormalizePlayerName(ref calendarInvite.Name))
			return;

		var player = _objectAccessor.FindPlayerByName(calendarInvite.Name);

		if (player)
		{
			// Invitee is online
			inviteeGuid = player.GUID;
			inviteeTeam = player.Team;
			inviteeGuildId = player.GuildId;
		}
		else
		{
			// Invitee offline, get data from database
			var guid = _characterCache.GetCharacterGuidByName(calendarInvite.Name);

			if (!guid.IsEmpty)
			{
				var characterInfo = _characterCache.GetCharacterCacheByGuid(guid);

				if (characterInfo != null)
				{
					inviteeGuid = guid;
					inviteeTeam = Player.TeamForRace(characterInfo.RaceId);
					inviteeGuildId = characterInfo.GuildId;
				}
			}
		}

		if (inviteeGuid.IsEmpty)
		{
			_calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.PlayerNotFound);

			return;
		}

		if (_player.Team != inviteeTeam && !_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionCalendar))
		{
			_calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.NotAllied);

			return;
		}

		var result1 = _characterDatabase.Query("SELECT flags FROM character_social WHERE guid = {0} AND friend = {1}", inviteeGuid, playerGuid);

		if (!result1.IsEmpty())
			if (Convert.ToBoolean(result1.Read<byte>(0) & (byte)SocialFlag.Ignored))
			{
				_calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.IgnoringYouS, calendarInvite.Name);

				return;
			}

		if (!calendarInvite.Creating)
		{
			var calendarEvent = _calendarManager.GetEvent(calendarInvite.EventID);

			if (calendarEvent != null)
			{
				if (calendarEvent.IsGuildEvent && calendarEvent.GuildId == inviteeGuildId)
				{
					// we can't invite guild members to guild events
					_calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.NoGuildInvites);

					return;
				}

				CalendarInvite invite = new(_calendarManager.GetFreeInviteId(), calendarInvite.EventID, inviteeGuid, playerGuid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.Invited, CalendarModerationRank.Player, "");
				_calendarManager.AddInvite(calendarEvent, invite);
			}
			else
			{
				_calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.EventInvalid);
			}
		}
		else
		{
			if (calendarInvite.IsSignUp && inviteeGuildId == _player.GuildId)
			{
				_calendarManager.SendCalendarCommandResult(playerGuid, CalendarError.NoGuildInvites);

				return;
			}

			CalendarInvite invite = new(calendarInvite.EventID, 0, inviteeGuid, playerGuid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.Invited, CalendarModerationRank.Player, "");
			_calendarManager.SendCalendarEventInvite(invite);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarEventSignUp)]
	void HandleCalendarEventSignup(CalendarEventSignUp calendarEventSignUp)
	{
		var guid = _player.GUID;

		var calendarEvent = _calendarManager.GetEvent(calendarEventSignUp.EventID);

		if (calendarEvent != null)
		{
			if (calendarEvent.IsGuildEvent && calendarEvent.GuildId != _player.GuildId)
			{
				_calendarManager.SendCalendarCommandResult(guid, CalendarError.GuildPlayerNotInGuild);

				return;
			}

			var status = calendarEventSignUp.Tentative ? CalendarInviteStatus.Tentative : CalendarInviteStatus.SignedUp;
			CalendarInvite invite = new(_calendarManager.GetFreeInviteId(), calendarEventSignUp.EventID, guid, guid, _gameTime.GetGameTime, status, CalendarModerationRank.Player, "");
			_calendarManager.AddInvite(calendarEvent, invite);
			_calendarManager.SendCalendarClearPendingAction(guid);
		}
		else
		{
			_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarRsvp)]
	void HandleCalendarRsvp(HandleCalendarRsvp calendarRSVP)
	{
		var guid = _player.GUID;

		var calendarEvent = _calendarManager.GetEvent(calendarRSVP.EventID);

		if (calendarEvent != null)
		{
			// i think we still should be able to remove self from locked events
			if (calendarRSVP.Status != CalendarInviteStatus.Removed && calendarEvent.IsLocked)
			{
				_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventLocked);

				return;
			}

			var invite = _calendarManager.GetInvite(calendarRSVP.InviteID);

			if (invite != null)
			{
				invite.Status = calendarRSVP.Status;
				invite.ResponseTime = _gameTime.GetGameTime;

				_calendarManager.UpdateInvite(invite);
				_calendarManager.SendCalendarEventStatus(calendarEvent, invite);
				_calendarManager.SendCalendarClearPendingAction(guid);
			}
			else
			{
				_calendarManager.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
			}
		}
		else
		{
			_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarRemoveInvite)]
	void HandleCalendarEventRemoveInvite(CalendarRemoveInvite calendarRemoveInvite)
	{
		var guid = _player.GUID;

		var calendarEvent = _calendarManager.GetEvent(calendarRemoveInvite.EventID);

		if (calendarEvent != null)
		{
			if (calendarEvent.OwnerGuid == calendarRemoveInvite.Guid)
			{
				_calendarManager.SendCalendarCommandResult(guid, CalendarError.DeleteCreatorFailed);

				return;
			}

			_calendarManager.RemoveInvite(calendarRemoveInvite.InviteID, calendarRemoveInvite.EventID, guid);
		}
		else
		{
			_calendarManager.SendCalendarCommandResult(guid, CalendarError.NoInvite);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarStatus)]
	void HandleCalendarStatus(CalendarStatus calendarStatus)
	{
		var guid = _player.GUID;

		var calendarEvent = _calendarManager.GetEvent(calendarStatus.EventID);

		if (calendarEvent != null)
		{
			var invite = _calendarManager.GetInvite(calendarStatus.InviteID);

			if (invite != null)
			{
				invite.Status = (CalendarInviteStatus)calendarStatus.Status;

				_calendarManager.UpdateInvite(invite);
				_calendarManager.SendCalendarEventStatus(calendarEvent, invite);
				_calendarManager.SendCalendarClearPendingAction(calendarStatus.Guid);
			}
			else
			{
				_calendarManager.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
			}
		}
		else
		{
			_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarModeratorStatus)]
	void HandleCalendarModeratorStatus(CalendarModeratorStatusQuery calendarModeratorStatus)
	{
		var guid = _player.GUID;

		var calendarEvent = _calendarManager.GetEvent(calendarModeratorStatus.EventID);

		if (calendarEvent != null)
		{
			var invite = _calendarManager.GetInvite(calendarModeratorStatus.InviteID);

			if (invite != null)
			{
				invite.Rank = (CalendarModerationRank)calendarModeratorStatus.Status;
				_calendarManager.UpdateInvite(invite);
				_calendarManager.SendCalendarEventModeratorStatusAlert(calendarEvent, invite);
			}
			else
			{
				_calendarManager.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
			}
		}
		else
		{
			_calendarManager.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarComplain)]
	void HandleCalendarComplain(CalendarComplain calendarComplain)
	{
		// what to do with complains?
	}

	[WorldPacketHandler(ClientOpcodes.CalendarGetNumPending)]
	void HandleCalendarGetNumPending(CalendarGetNumPending calendarGetNumPending)
	{
		var guid = _player.GUID;
		var pending = _calendarManager.GetPlayerNumPending(guid);

        _session.SendPacket(new CalendarSendNumPending(pending));
	}

	[WorldPacketHandler(ClientOpcodes.SetSavedInstanceExtend)]
	void HandleSetSavedInstanceExtend(SetSavedInstanceExtend setSavedInstanceExtend)
	{
		// cannot modify locks currently in use
		if (_player.Location.MapId == setSavedInstanceExtend.MapID)
			return;

		var expiryTimes = _instanceLockManager.UpdateInstanceLockExtensionForPlayer(_player.GUID, new MapDb2Entries((uint)setSavedInstanceExtend.MapID, (Difficulty)setSavedInstanceExtend.DifficultyID), setSavedInstanceExtend.Extend);

		if (expiryTimes.Item1 == DateTime.MinValue)
			return;

		CalendarRaidLockoutUpdated calendarRaidLockoutUpdated = new();
		calendarRaidLockoutUpdated.ServerTime = _gameTime.GetGameTime;
		calendarRaidLockoutUpdated.MapID = setSavedInstanceExtend.MapID;
		calendarRaidLockoutUpdated.DifficultyID = setSavedInstanceExtend.DifficultyID;
		calendarRaidLockoutUpdated.OldTimeRemaining = (int)Math.Max((expiryTimes.Item1 - _gameTime.GetSystemTime).TotalSeconds, 0);
		calendarRaidLockoutUpdated.NewTimeRemaining = (int)Math.Max((expiryTimes.Item2 - _gameTime.GetSystemTime).TotalSeconds, 0);
        _session.SendPacket(calendarRaidLockoutUpdated);
	}

	void SendCalendarRaidLockoutRemoved(InstanceLock instanceLock)
	{
		CalendarRaidLockoutRemoved calendarRaidLockoutRemoved = new();
		calendarRaidLockoutRemoved.InstanceID = instanceLock.GetInstanceId();
		calendarRaidLockoutRemoved.MapID = (int)instanceLock.GetMapId();
		calendarRaidLockoutRemoved.DifficultyID = instanceLock.GetDifficultyId();
        _session.SendPacket(calendarRaidLockoutRemoved);
	}
}