﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Runtime.Serialization;
using Forged.MapServer.Calendar;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps.Instances;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Calendar;
using Framework.Constants;
using Framework.Database;
using Game.Common.Handlers;

namespace Forged.MapServer.OpCodeHandlers;

public class CalendarHandler : IWorldSessionHandler
{
	public void SendCalendarRaidLockoutAdded(InstanceLock instanceLock)
	{
		CalendarRaidLockoutAdded calendarRaidLockoutAdded = new();
		calendarRaidLockoutAdded.InstanceID = instanceLock.GetInstanceId();
		calendarRaidLockoutAdded.ServerTime = (uint)GameTime.GetGameTime();
		calendarRaidLockoutAdded.MapID = (int)instanceLock.GetMapId();
		calendarRaidLockoutAdded.DifficultyID = instanceLock.GetDifficultyId();
		calendarRaidLockoutAdded.TimeRemaining = (int)(instanceLock.GetEffectiveExpiryTime() - GameTime.GetSystemTime()).TotalSeconds;
		_session.SendPacket(calendarRaidLockoutAdded);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarGet)]
	void HandleCalendarGetCalendar(CalendarGetCalendar calendarGetCalendar)
	{
		var guid = Player.GUID;

		var currTime = GameTime.GetGameTime();

		CalendarSendCalendar packet = new();
		packet.ServerTime = currTime;

		var invites = Global.CalendarMgr.GetPlayerInvites(guid);

		foreach (var invite in invites)
		{
			CalendarSendCalendarInviteInfo inviteInfo = new();
			inviteInfo.EventID = invite.EventId;
			inviteInfo.InviteID = invite.InviteId;
			inviteInfo.InviterGuid = invite.SenderGuid;
			inviteInfo.Status = invite.Status;
			inviteInfo.Moderator = invite.Rank;
			var calendarEvent = Global.CalendarMgr.GetEvent(invite.EventId);

			if (calendarEvent != null)
				inviteInfo.InviteType = (byte)(calendarEvent.IsGuildEvent && calendarEvent.GuildId == _player.GuildId ? 1 : 0);

			packet.Invites.Add(inviteInfo);
		}

		var playerEvents = Global.CalendarMgr.GetPlayerEvents(guid);

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

		foreach (var instanceLock in Global.InstanceLockMgr.GetInstanceLocksForPlayer(_player.GUID))
		{
			CalendarSendCalendarRaidLockoutInfo lockoutInfo = new();

			lockoutInfo.MapID = (int)instanceLock.GetMapId();
			lockoutInfo.DifficultyID = (uint)instanceLock.GetDifficultyId();
			lockoutInfo.ExpireTime = (int)Math.Max((instanceLock.GetEffectiveExpiryTime() - GameTime.GetSystemTime()).TotalSeconds, 0);
			lockoutInfo.InstanceID = instanceLock.GetInstanceId();

			packet.RaidLockouts.Add(lockoutInfo);
		}

		_session.SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarGetEvent)]
	void HandleCalendarGetEvent(CalendarGetEvent calendarGetEvent)
	{
		var calendarEvent = Global.CalendarMgr.GetEvent(calendarGetEvent.EventID);

		if (calendarEvent != null)
			Global.CalendarMgr.SendCalendarEvent(Player.GUID, calendarEvent, CalendarSendEventType.Get);
		else
			Global.CalendarMgr.SendCalendarCommandResult(Player.GUID, CalendarError.EventInvalid);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarCommunityInvite)]
	void HandleCalendarCommunityInvite(CalendarCommunityInviteRequest calendarCommunityInvite)
	{
		var guild = Global.GuildMgr.GetGuildById(Player.GuildId);

		if (guild)
			guild.MassInviteToEvent(this, calendarCommunityInvite.MinLevel, calendarCommunityInvite.MaxLevel, (GuildRankOrder)calendarCommunityInvite.MaxRankOrder);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarAddEvent)]
	void HandleCalendarAddEvent(CalendarAddEvent calendarAddEvent)
	{
		var guid = Player.GUID;

		calendarAddEvent.EventInfo.Time = Time.LocalTimeToUTCTime(calendarAddEvent.EventInfo.Time);

		// prevent events in the past
		// To Do: properly handle timezones and remove the "- time_t(86400L)" hack
		if (calendarAddEvent.EventInfo.Time < (GameTime.GetGameTime() - 86400L))
		{
			Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventPassed);

			return;
		}

		// If the event is a guild event, check if the player is in a guild
		if (CalendarEvent.ModifyIsGuildEventFlags(calendarAddEvent.EventInfo.Flags) || CalendarEvent.ModifyIsGuildAnnouncementFlags(calendarAddEvent.EventInfo.Flags))
			if (_player.GuildId == 0)
			{
				Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.GuildPlayerNotInGuild);

				return;
			}

		// Check if the player reached the max number of events allowed to create
		if (CalendarEvent.ModifyIsGuildEventFlags(calendarAddEvent.EventInfo.Flags) || CalendarEvent.ModifyIsGuildAnnouncementFlags(calendarAddEvent.EventInfo.Flags))
		{
			if (Global.CalendarMgr.GetGuildEvents(_player.GuildId).Count >= SharedConst.CalendarMaxGuildEvents)
			{
				Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.GuildEventsExceeded);

				return;
			}
		}
		else
		{
			if (Global.CalendarMgr.GetEventsCreatedBy(guid).Count >= SharedConst.CalendarMaxEvents)
			{
				Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventsExceeded);

				return;
			}
		}

		if (CalendarEventCreationCooldown > GameTime.GetGameTime())
		{
			Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.Internal);

			return;
		}

		CalendarEventCreationCooldown = GameTime.GetGameTime() + SharedConst.CalendarCreateEventCooldown;

		CalendarEvent calendarEvent = new(Global.CalendarMgr.GetFreeEventId(),
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
			Global.CalendarMgr.AddInvite(calendarEvent, invite);
		}
		else
		{
			SQLTransaction trans = null;

			if (calendarAddEvent.EventInfo.Invites.Length > 1)
				trans = new SQLTransaction();

			for (var i = 0; i < calendarAddEvent.EventInfo.Invites.Length; ++i)
			{
				CalendarInvite invite = new(Global.CalendarMgr.GetFreeInviteId(),
											calendarEvent.EventId,
											calendarAddEvent.EventInfo.Invites[i].Guid,
											guid,
											SharedConst.CalendarDefaultResponseTime,
											(CalendarInviteStatus)calendarAddEvent.EventInfo.Invites[i].Status,
											(CalendarModerationRank)calendarAddEvent.EventInfo.Invites[i].Moderator,
											"");

				Global.CalendarMgr.AddInvite(calendarEvent, invite, trans);
			}

			if (calendarAddEvent.EventInfo.Invites.Length > 1)
				DB.Characters.CommitTransaction(trans);
		}

		Global.CalendarMgr.AddEvent(calendarEvent, CalendarSendEventType.Add);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarUpdateEvent)]
	void HandleCalendarUpdateEvent(CalendarUpdateEvent calendarUpdateEvent)
	{
		var guid = Player.GUID;
		long oldEventTime;

		calendarUpdateEvent.EventInfo.Time = Time.LocalTimeToUTCTime(calendarUpdateEvent.EventInfo.Time);

		// prevent events in the past
		// To Do: properly handle timezones and remove the "- time_t(86400L)" hack
		if (calendarUpdateEvent.EventInfo.Time < (GameTime.GetGameTime() - 86400L))
			return;

		var calendarEvent = Global.CalendarMgr.GetEvent(calendarUpdateEvent.EventInfo.EventID);

		if (calendarEvent != null)
		{
			oldEventTime = calendarEvent.Date;

			calendarEvent.EventType = (CalendarEventType)calendarUpdateEvent.EventInfo.EventType;
			calendarEvent.Flags = (CalendarFlags)calendarUpdateEvent.EventInfo.Flags;
			calendarEvent.Date = calendarUpdateEvent.EventInfo.Time;
			calendarEvent.TextureId = (int)calendarUpdateEvent.EventInfo.TextureID;
			calendarEvent.Title = calendarUpdateEvent.EventInfo.Title;
			calendarEvent.Description = calendarUpdateEvent.EventInfo.Description;

			Global.CalendarMgr.UpdateEvent(calendarEvent);
			Global.CalendarMgr.SendCalendarEventUpdateAlert(calendarEvent, oldEventTime);
		}
		else
		{
			Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarRemoveEvent)]
	void HandleCalendarRemoveEvent(CalendarRemoveEvent calendarRemoveEvent)
	{
		var guid = Player.GUID;
		Global.CalendarMgr.RemoveEvent(calendarRemoveEvent.EventID, guid);
	}

	[WorldPacketHandler(ClientOpcodes.CalendarCopyEvent)]
	void HandleCalendarCopyEvent(CalendarCopyEvent calendarCopyEvent)
	{
		var guid = Player.GUID;

		calendarCopyEvent.Date = Time.LocalTimeToUTCTime(calendarCopyEvent.Date);

		// prevent events in the past
		// To Do: properly handle timezones and remove the "- time_t(86400L)" hack
		if (calendarCopyEvent.Date < (GameTime.GetGameTime() - 86400L))
		{
			Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventPassed);

			return;
		}

		var oldEvent = Global.CalendarMgr.GetEvent(calendarCopyEvent.EventID);

		if (oldEvent != null)
		{
			// Ensure that the player has access to the event
			if (oldEvent.IsGuildEvent || oldEvent.IsGuildAnnouncement)
			{
				if (oldEvent.GuildId != _player.GuildId)
				{
					Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);

					return;
				}
			}
			else
			{
				if (oldEvent.OwnerGuid != guid)
				{
					Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);

					return;
				}
			}

			// Check if the player reached the max number of events allowed to create
			if (oldEvent.IsGuildEvent || oldEvent.IsGuildAnnouncement)
			{
				if (Global.CalendarMgr.GetGuildEvents(_player.GuildId).Count >= SharedConst.CalendarMaxGuildEvents)
				{
					Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.GuildEventsExceeded);

					return;
				}
			}
			else
			{
				if (Global.CalendarMgr.GetEventsCreatedBy(guid).Count >= SharedConst.CalendarMaxEvents)
				{
					Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventsExceeded);

					return;
				}
			}

			if (CalendarEventCreationCooldown > GameTime.GetGameTime())
			{
				Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.Internal);

				return;
			}

			CalendarEventCreationCooldown = GameTime.GetGameTime() + SharedConst.CalendarCreateEventCooldown;

			CalendarEvent newEvent = new(oldEvent, Global.CalendarMgr.GetFreeEventId());
			newEvent.Date = calendarCopyEvent.Date;
			Global.CalendarMgr.AddEvent(newEvent, CalendarSendEventType.Copy);

			var invites = Global.CalendarMgr.GetEventInvites(calendarCopyEvent.EventID);
			SQLTransaction trans = null;

			if (invites.Count > 1)
				trans = new SQLTransaction();

			foreach (var invite in invites)
				Global.CalendarMgr.AddInvite(newEvent, new CalendarInvite(invite, Global.CalendarMgr.GetFreeInviteId(), newEvent.EventId), trans);

			if (invites.Count > 1)
				DB.Characters.CommitTransaction(trans);
			// should we change owner when somebody makes a copy of event owned by another person?
		}
		else
		{
			Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarInvite)]
	void HandleCalendarInvite(CalendarInvitePkt calendarInvite)
	{
		var playerGuid = Player.GUID;

		var inviteeGuid = ObjectGuid.Empty;
		TeamFaction inviteeTeam = 0;
		ulong inviteeGuildId = 0;

		if (!ObjectManager.NormalizePlayerName(ref calendarInvite.Name))
			return;

		var player = Global.ObjAccessor.FindPlayerByName(calendarInvite.Name);

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
			var guid = Global.CharacterCacheStorage.GetCharacterGuidByName(calendarInvite.Name);

			if (!guid.IsEmpty)
			{
				var characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(guid);

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
			Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.PlayerNotFound);

			return;
		}

		if (Player.Team != inviteeTeam && !WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionCalendar))
		{
			Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.NotAllied);

			return;
		}

		var result1 = DB.Characters.Query("SELECT flags FROM character_social WHERE guid = {0} AND friend = {1}", inviteeGuid, playerGuid);

		if (!result1.IsEmpty())
			if (Convert.ToBoolean(result1.Read<byte>(0) & (byte)SocialFlag.Ignored))
			{
				Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.IgnoringYouS, calendarInvite.Name);

				return;
			}

		if (!calendarInvite.Creating)
		{
			var calendarEvent = Global.CalendarMgr.GetEvent(calendarInvite.EventID);

			if (calendarEvent != null)
			{
				if (calendarEvent.IsGuildEvent && calendarEvent.GuildId == inviteeGuildId)
				{
					// we can't invite guild members to guild events
					Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.NoGuildInvites);

					return;
				}

				CalendarInvite invite = new(Global.CalendarMgr.GetFreeInviteId(), calendarInvite.EventID, inviteeGuid, playerGuid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.Invited, CalendarModerationRank.Player, "");
				Global.CalendarMgr.AddInvite(calendarEvent, invite);
			}
			else
			{
				Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.EventInvalid);
			}
		}
		else
		{
			if (calendarInvite.IsSignUp && inviteeGuildId == Player.GuildId)
			{
				Global.CalendarMgr.SendCalendarCommandResult(playerGuid, CalendarError.NoGuildInvites);

				return;
			}

			CalendarInvite invite = new(calendarInvite.EventID, 0, inviteeGuid, playerGuid, SharedConst.CalendarDefaultResponseTime, CalendarInviteStatus.Invited, CalendarModerationRank.Player, "");
			Global.CalendarMgr.SendCalendarEventInvite(invite);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarEventSignUp)]
	void HandleCalendarEventSignup(CalendarEventSignUp calendarEventSignUp)
	{
		var guid = Player.GUID;

		var calendarEvent = Global.CalendarMgr.GetEvent(calendarEventSignUp.EventID);

		if (calendarEvent != null)
		{
			if (calendarEvent.IsGuildEvent && calendarEvent.GuildId != Player.GuildId)
			{
				Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.GuildPlayerNotInGuild);

				return;
			}

			var status = calendarEventSignUp.Tentative ? CalendarInviteStatus.Tentative : CalendarInviteStatus.SignedUp;
			CalendarInvite invite = new(Global.CalendarMgr.GetFreeInviteId(), calendarEventSignUp.EventID, guid, guid, GameTime.GetGameTime(), status, CalendarModerationRank.Player, "");
			Global.CalendarMgr.AddInvite(calendarEvent, invite);
			Global.CalendarMgr.SendCalendarClearPendingAction(guid);
		}
		else
		{
			Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarRsvp)]
	void HandleCalendarRsvp(HandleCalendarRsvp calendarRSVP)
	{
		var guid = Player.GUID;

		var calendarEvent = Global.CalendarMgr.GetEvent(calendarRSVP.EventID);

		if (calendarEvent != null)
		{
			// i think we still should be able to remove self from locked events
			if (calendarRSVP.Status != CalendarInviteStatus.Removed && calendarEvent.IsLocked)
			{
				Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventLocked);

				return;
			}

			var invite = Global.CalendarMgr.GetInvite(calendarRSVP.InviteID);

			if (invite != null)
			{
				invite.Status = calendarRSVP.Status;
				invite.ResponseTime = GameTime.GetGameTime();

				Global.CalendarMgr.UpdateInvite(invite);
				Global.CalendarMgr.SendCalendarEventStatus(calendarEvent, invite);
				Global.CalendarMgr.SendCalendarClearPendingAction(guid);
			}
			else
			{
				Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
			}
		}
		else
		{
			Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarRemoveInvite)]
	void HandleCalendarEventRemoveInvite(CalendarRemoveInvite calendarRemoveInvite)
	{
		var guid = Player.GUID;

		var calendarEvent = Global.CalendarMgr.GetEvent(calendarRemoveInvite.EventID);

		if (calendarEvent != null)
		{
			if (calendarEvent.OwnerGuid == calendarRemoveInvite.Guid)
			{
				Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.DeleteCreatorFailed);

				return;
			}

			Global.CalendarMgr.RemoveInvite(calendarRemoveInvite.InviteID, calendarRemoveInvite.EventID, guid);
		}
		else
		{
			Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarStatus)]
	void HandleCalendarStatus(CalendarStatus calendarStatus)
	{
		var guid = Player.GUID;

		var calendarEvent = Global.CalendarMgr.GetEvent(calendarStatus.EventID);

		if (calendarEvent != null)
		{
			var invite = Global.CalendarMgr.GetInvite(calendarStatus.InviteID);

			if (invite != null)
			{
				invite.Status = (CalendarInviteStatus)calendarStatus.Status;

				Global.CalendarMgr.UpdateInvite(invite);
				Global.CalendarMgr.SendCalendarEventStatus(calendarEvent, invite);
				Global.CalendarMgr.SendCalendarClearPendingAction(calendarStatus.Guid);
			}
			else
			{
				Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
			}
		}
		else
		{
			Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
		}
	}

	[WorldPacketHandler(ClientOpcodes.CalendarModeratorStatus)]
	void HandleCalendarModeratorStatus(CalendarModeratorStatusQuery calendarModeratorStatus)
	{
		var guid = Player.GUID;

		var calendarEvent = Global.CalendarMgr.GetEvent(calendarModeratorStatus.EventID);

		if (calendarEvent != null)
		{
			var invite = Global.CalendarMgr.GetInvite(calendarModeratorStatus.InviteID);

			if (invite != null)
			{
				invite.Rank = (CalendarModerationRank)calendarModeratorStatus.Status;
				Global.CalendarMgr.UpdateInvite(invite);
				Global.CalendarMgr.SendCalendarEventModeratorStatusAlert(calendarEvent, invite);
			}
			else
			{
				Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.NoInvite); // correct?
			}
		}
		else
		{
			Global.CalendarMgr.SendCalendarCommandResult(guid, CalendarError.EventInvalid);
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
		var guid = Player.GUID;
		var pending = Global.CalendarMgr.GetPlayerNumPending(guid);

		_session.SendPacket(new CalendarSendNumPending(pending));
	}

	[WorldPacketHandler(ClientOpcodes.SetSavedInstanceExtend)]
	void HandleSetSavedInstanceExtend(SetSavedInstanceExtend setSavedInstanceExtend)
	{
		// cannot modify locks currently in use
		if (_player.Location.MapId == setSavedInstanceExtend.MapID)
			return;

		var expiryTimes = Global.InstanceLockMgr.UpdateInstanceLockExtensionForPlayer(_player.GUID, new MapDb2Entries((uint)setSavedInstanceExtend.MapID, (Difficulty)setSavedInstanceExtend.DifficultyID), setSavedInstanceExtend.Extend);

		if (expiryTimes.Item1 == DateTime.MinValue)
			return;

		CalendarRaidLockoutUpdated calendarRaidLockoutUpdated = new();
		calendarRaidLockoutUpdated.ServerTime = GameTime.GetGameTime();
		calendarRaidLockoutUpdated.MapID = setSavedInstanceExtend.MapID;
		calendarRaidLockoutUpdated.DifficultyID = setSavedInstanceExtend.DifficultyID;
		calendarRaidLockoutUpdated.OldTimeRemaining = (int)Math.Max((expiryTimes.Item1 - GameTime.GetSystemTime()).TotalSeconds, 0);
		calendarRaidLockoutUpdated.NewTimeRemaining = (int)Math.Max((expiryTimes.Item2 - GameTime.GetSystemTime()).TotalSeconds, 0);
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