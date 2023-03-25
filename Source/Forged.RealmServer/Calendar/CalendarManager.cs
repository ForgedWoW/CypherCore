// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Mails;
using Forged.RealmServer.Entities.Objects;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets.Calendar;

namespace Forged.RealmServer;

public class CalendarManager : Singleton<CalendarManager>
{
	readonly List<CalendarEvent> _events;
	readonly MultiMap<ulong, CalendarInvite> _invites;
	readonly List<ulong> _freeEventIds = new();
	readonly List<ulong> _freeInviteIds = new();
	ulong _maxEventId;
	ulong _maxInviteId;

	CalendarManager()
	{
		_events = new List<CalendarEvent>();
		_invites = new MultiMap<ulong, CalendarInvite>();
	}

	public void LoadFromDB()
	{
		var oldMSTime = Time.MSTime;

		uint count = 0;
		_maxEventId = 0;
		_maxInviteId = 0;

		//                                              0        1      2      3            4          5          6     7      8
		var result = DB.Characters.Query("SELECT EventID, Owner, Title, Description, EventType, TextureID, Date, Flags, LockDate FROM calendar_events");

		if (!result.IsEmpty())
			do
			{
				var eventID = result.Read<ulong>(0);
				var ownerGUID = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(1));
				var title = result.Read<string>(2);
				var description = result.Read<string>(3);
				var type = (CalendarEventType)result.Read<byte>(4);
				var textureID = result.Read<int>(5);
				var date = result.Read<long>(6);
				var flags = (CalendarFlags)result.Read<uint>(7);
				var lockDate = result.Read<long>(8);
				ulong guildID = 0;

				if (flags.HasAnyFlag(CalendarFlags.GuildEvent) || flags.HasAnyFlag(CalendarFlags.WithoutInvites))
					guildID = Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(ownerGUID);

				CalendarEvent calendarEvent = new(eventID, ownerGUID, guildID, type, textureID, date, flags, title, description, lockDate);
				_events.Add(calendarEvent);

				_maxEventId = Math.Max(_maxEventId, eventID);

				++count;
			} while (result.NextRow());

		Log.Logger.Information($"Loaded {count} calendar events in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
		count = 0;
		oldMSTime = Time.MSTime;

		//                                    0         1        2        3       4       5             6               7
		result = DB.Characters.Query("SELECT InviteID, EventID, Invitee, Sender, Status, ResponseTime, ModerationRank, Note FROM calendar_invites");

		if (!result.IsEmpty())
			do
			{
				var inviteId = result.Read<ulong>(0);
				var eventId = result.Read<ulong>(1);
				var invitee = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(2));
				var senderGUID = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(3));
				var status = (CalendarInviteStatus)result.Read<byte>(4);
				var responseTime = result.Read<long>(5);
				var rank = (CalendarModerationRank)result.Read<byte>(6);
				var note = result.Read<string>(7);

				CalendarInvite invite = new(inviteId, eventId, invitee, senderGUID, responseTime, status, rank, note);
				_invites.Add(eventId, invite);

				_maxInviteId = Math.Max(_maxInviteId, inviteId);

				++count;
			} while (result.NextRow());

		Log.Logger.Information($"Loaded {count} calendar invites in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");

		for (ulong i = 1; i < _maxEventId; ++i)
			if (GetEvent(i) == null)
				_freeEventIds.Add(i);

		for (ulong i = 1; i < _maxInviteId; ++i)
			if (GetInvite(i) == null)
				_freeInviteIds.Add(i);
	}

	public void AddEvent(CalendarEvent calendarEvent, CalendarSendEventType sendType)
	{
		_events.Add(calendarEvent);
		UpdateEvent(calendarEvent);
		SendCalendarEvent(calendarEvent.OwnerGuid, calendarEvent, sendType);
	}

	public void AddInvite(CalendarEvent calendarEvent, CalendarInvite invite, SQLTransaction trans = null)
	{
		if (!calendarEvent.IsGuildAnnouncement && calendarEvent.OwnerGuid != invite.InviteeGuid)
			SendCalendarEventInvite(invite);

		if (!calendarEvent.IsGuildEvent || invite.InviteeGuid == calendarEvent.OwnerGuid)
			SendCalendarEventInviteAlert(calendarEvent, invite);

		if (!calendarEvent.IsGuildAnnouncement)
		{
			_invites.Add(invite.EventId, invite);
			UpdateInvite(invite, trans);
		}
	}

	public void RemoveEvent(ulong eventId, ObjectGuid remover)
	{
		var calendarEvent = GetEvent(eventId);

		if (calendarEvent == null)
		{
			SendCalendarCommandResult(remover, CalendarError.EventInvalid);

			return;
		}

		RemoveEvent(calendarEvent, remover);
	}

	public void RemoveInvite(ulong inviteId, ulong eventId, ObjectGuid remover)
	{
		var calendarEvent = GetEvent(eventId);

		if (calendarEvent == null)
			return;

		CalendarInvite calendarInvite = null;

		foreach (var invite in _invites[eventId])
			if (invite.InviteId == inviteId)
			{
				calendarInvite = invite;

				break;
			}

		if (calendarInvite == null)
			return;

		SQLTransaction trans = new();
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CALENDAR_INVITE);
		stmt.AddValue(0, calendarInvite.InviteId);
		trans.Append(stmt);
		DB.Characters.CommitTransaction(trans);

		if (!calendarEvent.IsGuildEvent)
			SendCalendarEventInviteRemoveAlert(calendarInvite.InviteeGuid, calendarEvent, CalendarInviteStatus.Removed);

		SendCalendarEventInviteRemove(calendarEvent, calendarInvite, (uint)calendarEvent.Flags);

		// we need to find out how to use CALENDAR_INVITE_REMOVED_MAIL_SUBJECT to force client to display different mail
		//if (itr._invitee != remover)
		//    MailDraft(calendarEvent.BuildCalendarMailSubject(remover), calendarEvent.BuildCalendarMailBody())
		//        .SendMailTo(trans, MailReceiver(itr.GetInvitee()), calendarEvent, MAIL_CHECK_MASK_COPIED);

		_invites.Remove(eventId, calendarInvite);
	}

	public void UpdateEvent(CalendarEvent calendarEvent)
	{
		SQLTransaction trans = new();
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CALENDAR_EVENT);
		stmt.AddValue(0, calendarEvent.EventId);
		stmt.AddValue(1, calendarEvent.OwnerGuid.Counter);
		stmt.AddValue(2, calendarEvent.Title);
		stmt.AddValue(3, calendarEvent.Description);
		stmt.AddValue(4, (byte)calendarEvent.EventType);
		stmt.AddValue(5, calendarEvent.TextureId);
		stmt.AddValue(6, calendarEvent.Date);
		stmt.AddValue(7, (uint)calendarEvent.Flags);
		stmt.AddValue(8, calendarEvent.LockDate);
		trans.Append(stmt);
		DB.Characters.CommitTransaction(trans);
	}

	public void UpdateInvite(CalendarInvite invite, SQLTransaction trans = null)
	{
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_CALENDAR_INVITE);
		stmt.AddValue(0, invite.InviteId);
		stmt.AddValue(1, invite.EventId);
		stmt.AddValue(2, invite.InviteeGuid.Counter);
		stmt.AddValue(3, invite.SenderGuid.Counter);
		stmt.AddValue(4, (byte)invite.Status);
		stmt.AddValue(5, invite.ResponseTime);
		stmt.AddValue(6, (byte)invite.Rank);
		stmt.AddValue(7, invite.Note);
		DB.Characters.ExecuteOrAppend(trans, stmt);
	}

	public void RemoveAllPlayerEventsAndInvites(ObjectGuid guid)
	{
		foreach (var calendarEvent in _events)
			if (calendarEvent.OwnerGuid == guid)
				RemoveEvent(calendarEvent.EventId, ObjectGuid.Empty); // don't send mail if removing a character

		var playerInvites = GetPlayerInvites(guid);

		foreach (var calendarInvite in playerInvites)
			RemoveInvite(calendarInvite.InviteId, calendarInvite.EventId, guid);
	}

	public void RemovePlayerGuildEventsAndSignups(ObjectGuid guid, ulong guildId)
	{
		foreach (var calendarEvent in _events)
			if (calendarEvent.OwnerGuid == guid && (calendarEvent.IsGuildEvent || calendarEvent.IsGuildAnnouncement))
				RemoveEvent(calendarEvent.EventId, guid);

		var playerInvites = GetPlayerInvites(guid);

		foreach (var playerCalendarEvent in playerInvites)
		{
			var calendarEvent = GetEvent(playerCalendarEvent.EventId);

			if (calendarEvent != null)
				if (calendarEvent.IsGuildEvent && calendarEvent.GuildId == guildId)
					RemoveInvite(playerCalendarEvent.InviteId, playerCalendarEvent.EventId, guid);
		}
	}

	public CalendarEvent GetEvent(ulong eventId)
	{
		foreach (var calendarEvent in _events)
			if (calendarEvent.EventId == eventId)
				return calendarEvent;

		Log.Logger.Debug("CalendarMgr:GetEvent: {0} not found!", eventId);

		return null;
	}

	public CalendarInvite GetInvite(ulong inviteId)
	{
		foreach (var calendarEvent in _invites.Values)
			if (calendarEvent.InviteId == inviteId)
				return calendarEvent;

		Log.Logger.Debug("CalendarMgr:GetInvite: {0} not found!", inviteId);

		return null;
	}

	public ulong GetFreeEventId()
	{
		if (_freeEventIds.Empty())
			return ++_maxEventId;

		var eventId = _freeEventIds.FirstOrDefault();
		_freeEventIds.RemoveAt(0);

		return eventId;
	}

	public void FreeInviteId(ulong id)
	{
		if (id == _maxInviteId)
			--_maxInviteId;
		else
			_freeInviteIds.Add(id);
	}

	public ulong GetFreeInviteId()
	{
		if (_freeInviteIds.Empty())
			return ++_maxInviteId;

		var inviteId = _freeInviteIds.FirstOrDefault();
		_freeInviteIds.RemoveAt(0);

		return inviteId;
	}

	public void DeleteOldEvents()
	{
		var oldEventsTime = _gameTime.GetGameTime - SharedConst.CalendarOldEventsDeletionTime;

		foreach (var calendarEvent in _events)
			if (calendarEvent.Date < oldEventsTime)
				RemoveEvent(calendarEvent, ObjectGuid.Empty);
	}

	public List<CalendarEvent> GetEventsCreatedBy(ObjectGuid guid, bool includeGuildEvents = false)
	{
		List<CalendarEvent> result = new();

		foreach (var calendarEvent in _events)
			if (calendarEvent.OwnerGuid == guid && (includeGuildEvents || (!calendarEvent.IsGuildEvent && !calendarEvent.IsGuildAnnouncement)))
				result.Add(calendarEvent);

		return result;
	}

	public List<CalendarEvent> GetGuildEvents(ulong guildId)
	{
		List<CalendarEvent> result = new();

		if (guildId == 0)
			return result;

		foreach (var calendarEvent in _events)
			if (calendarEvent.IsGuildEvent || calendarEvent.IsGuildAnnouncement)
				if (calendarEvent.GuildId == guildId)
					result.Add(calendarEvent);

		return result;
	}

	public List<CalendarEvent> GetPlayerEvents(ObjectGuid guid)
	{
		List<CalendarEvent> events = new();

		foreach (var pair in _invites.KeyValueList)
			if (pair.Value.InviteeGuid == guid)
			{
				var Event = GetEvent(pair.Key);

				if (Event != null) // null check added as attempt to fix #11512
					events.Add(Event);
			}

		var player = Global.ObjAccessor.FindPlayer(guid);

		if (player?.GuildId != 0)
			foreach (var calendarEvent in _events)
				if (calendarEvent.GuildId == player.GuildId)
					events.Add(calendarEvent);

		return events;
	}

	public List<CalendarInvite> GetEventInvites(ulong eventId)
	{
		return _invites[eventId];
	}

	public List<CalendarInvite> GetPlayerInvites(ObjectGuid guid)
	{
		List<CalendarInvite> invites = new();

		foreach (var calendarEvent in _invites.Values)
			if (calendarEvent.InviteeGuid == guid)
				invites.Add(calendarEvent);

		return invites;
	}

	public uint GetPlayerNumPending(ObjectGuid guid)
	{
		var invites = GetPlayerInvites(guid);

		uint pendingNum = 0;

		foreach (var calendarEvent in invites)
			switch (calendarEvent.Status)
			{
				case CalendarInviteStatus.Invited:
				case CalendarInviteStatus.Tentative:
				case CalendarInviteStatus.NotSignedUp:
					++pendingNum;

					break;
				default:
					break;
			}

		return pendingNum;
	}

	public void SendCalendarEventInvite(CalendarInvite invite)
	{
		var calendarEvent = GetEvent(invite.EventId);

		var invitee = invite.InviteeGuid;
		var player = Global.ObjAccessor.FindPlayer(invitee);

		var level = player ? player.Level : Global.CharacterCacheStorage.GetCharacterLevelByGuid(invitee);

		CalendarInviteAdded packet = new();
		packet.EventID = calendarEvent != null ? calendarEvent.EventId : 0;
		packet.InviteGuid = invitee;
		packet.InviteID = calendarEvent != null ? invite.InviteId : 0;
		packet.Level = (byte)level;
		packet.ResponseTime = invite.ResponseTime;
		packet.Status = invite.Status;
		packet.Type = (byte)(calendarEvent != null ? calendarEvent.IsGuildEvent ? 1 : 0 : 0); // Correct ?
		packet.ClearPending = calendarEvent == null || !calendarEvent.IsGuildEvent;           // Correct ?

		if (calendarEvent == null) // Pre-invite
		{
			player = Global.ObjAccessor.FindPlayer(invite.SenderGuid);

			if (player)
				player.SendPacket(packet);
		}
		else
		{
			if (calendarEvent.OwnerGuid != invite.InviteeGuid) // correct?
				SendPacketToAllEventRelatives(packet, calendarEvent);
		}
	}

	public void SendCalendarEventUpdateAlert(CalendarEvent calendarEvent, long originalDate)
	{
		CalendarEventUpdatedAlert packet = new();
		packet.ClearPending = true; // FIXME
		packet.Date = calendarEvent.Date;
		packet.Description = calendarEvent.Description;
		packet.EventID = calendarEvent.EventId;
		packet.EventName = calendarEvent.Title;
		packet.EventType = calendarEvent.EventType;
		packet.Flags = calendarEvent.Flags;
		packet.LockDate = calendarEvent.LockDate; // Always 0 ?
		packet.OriginalDate = originalDate;
		packet.TextureID = calendarEvent.TextureId;

		SendPacketToAllEventRelatives(packet, calendarEvent);
	}

	public void SendCalendarEventStatus(CalendarEvent calendarEvent, CalendarInvite invite)
	{
		CalendarInviteStatusPacket packet = new();
		packet.ClearPending = true; // FIXME
		packet.Date = calendarEvent.Date;
		packet.EventID = calendarEvent.EventId;
		packet.Flags = calendarEvent.Flags;
		packet.InviteGuid = invite.InviteeGuid;
		packet.ResponseTime = invite.ResponseTime;
		packet.Status = invite.Status;

		SendPacketToAllEventRelatives(packet, calendarEvent);
	}

	public void SendCalendarEventModeratorStatusAlert(CalendarEvent calendarEvent, CalendarInvite invite)
	{
		CalendarModeratorStatus packet = new();
		packet.ClearPending = true; // FIXME
		packet.EventID = calendarEvent.EventId;
		packet.InviteGuid = invite.InviteeGuid;
		packet.Status = invite.Status;

		SendPacketToAllEventRelatives(packet, calendarEvent);
	}

	public void SendCalendarEvent(ObjectGuid guid, CalendarEvent calendarEvent, CalendarSendEventType sendType)
	{
		var player = Global.ObjAccessor.FindPlayer(guid);

		if (!player)
			return;

		var eventInviteeList = _invites[calendarEvent.EventId];

		CalendarSendEvent packet = new();
		packet.Date = calendarEvent.Date;
		packet.Description = calendarEvent.Description;
		packet.EventID = calendarEvent.EventId;
		packet.EventName = calendarEvent.Title;
		packet.EventType = sendType;
		packet.Flags = calendarEvent.Flags;
		packet.GetEventType = calendarEvent.EventType;
		packet.LockDate = calendarEvent.LockDate; // Always 0 ?
		packet.OwnerGuid = calendarEvent.OwnerGuid;
		packet.TextureID = calendarEvent.TextureId;

		var guild = Global.GuildMgr.GetGuildById(calendarEvent.GuildId);
		packet.EventGuildID = (guild ? guild.GetGUID() : ObjectGuid.Empty);

		foreach (var calendarInvite in eventInviteeList)
		{
			var inviteeGuid = calendarInvite.InviteeGuid;
			var invitee = Global.ObjAccessor.FindPlayer(inviteeGuid);

			var inviteeLevel = invitee ? invitee.Level : Global.CharacterCacheStorage.GetCharacterLevelByGuid(inviteeGuid);
			var inviteeGuildId = invitee ? invitee.GuildId : Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(inviteeGuid);

			CalendarEventInviteInfo inviteInfo = new();
			inviteInfo.Guid = inviteeGuid;
			inviteInfo.Level = (byte)inviteeLevel;
			inviteInfo.Status = calendarInvite.Status;
			inviteInfo.Moderator = calendarInvite.Rank;
			inviteInfo.InviteType = (byte)(calendarEvent.IsGuildEvent && calendarEvent.GuildId == inviteeGuildId ? 1 : 0);
			inviteInfo.InviteID = calendarInvite.InviteId;
			inviteInfo.ResponseTime = calendarInvite.ResponseTime;
			inviteInfo.Notes = calendarInvite.Note;

			packet.Invites.Add(inviteInfo);
		}

		player.SendPacket(packet);
	}

	public void SendCalendarClearPendingAction(ObjectGuid guid)
	{
		var player = Global.ObjAccessor.FindPlayer(guid);

		if (player)
			player.SendPacket(new CalendarClearPendingAction());
	}

	public void SendCalendarCommandResult(ObjectGuid guid, CalendarError err, string param = null)
	{
		var player = Global.ObjAccessor.FindPlayer(guid);

		if (player)
		{
			CalendarCommandResult packet = new();
			packet.Command = 1; // FIXME
			packet.Result = err;

			switch (err)
			{
				case CalendarError.OtherInvitesExceeded:
				case CalendarError.AlreadyInvitedToEventS:
				case CalendarError.IgnoringYouS:
					packet.Name = param;

					break;
			}

			player.SendPacket(packet);
		}
	}

	void RemoveEvent(CalendarEvent calendarEvent, ObjectGuid remover)
	{
		if (calendarEvent == null)
		{
			SendCalendarCommandResult(remover, CalendarError.EventInvalid);

			return;
		}

		SendCalendarEventRemovedAlert(calendarEvent);

		SQLTransaction trans = new();
		PreparedStatement stmt;
		MailDraft mail = new(calendarEvent.BuildCalendarMailSubject(remover), calendarEvent.BuildCalendarMailBody());

		var eventInvites = _invites[calendarEvent.EventId];

		for (var i = 0; i < eventInvites.Count; ++i)
		{
			var invite = eventInvites[i];
			stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CALENDAR_INVITE);
			stmt.AddValue(0, invite.InviteId);
			trans.Append(stmt);

			// guild events only? check invite status here?
			// When an event is deleted, all invited (accepted/declined? - verify) guildies are notified via in-game mail. (wowwiki)
			if (!remover.IsEmpty && invite.InviteeGuid != remover)
				mail.SendMailTo(trans, new MailReceiver(invite.InviteeGuid.Counter), new MailSender(calendarEvent), MailCheckMask.Copied);
		}

		_invites.Remove(calendarEvent.EventId);

		stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CALENDAR_EVENT);
		stmt.AddValue(0, calendarEvent.EventId);
		trans.Append(stmt);
		DB.Characters.CommitTransaction(trans);

		_events.Remove(calendarEvent);
	}

	void FreeEventId(ulong id)
	{
		if (id == _maxEventId)
			--_maxEventId;
		else
			_freeEventIds.Add(id);
	}

	void SendCalendarEventRemovedAlert(CalendarEvent calendarEvent)
	{
		CalendarEventRemovedAlert packet = new();
		packet.ClearPending = true; // FIXME
		packet.Date = calendarEvent.Date;
		packet.EventID = calendarEvent.EventId;

		SendPacketToAllEventRelatives(packet, calendarEvent);
	}

	void SendCalendarEventInviteRemove(CalendarEvent calendarEvent, CalendarInvite invite, uint flags)
	{
		CalendarInviteRemoved packet = new();
		packet.ClearPending = true; // FIXME
		packet.EventID = calendarEvent.EventId;
		packet.Flags = flags;
		packet.InviteGuid = invite.InviteeGuid;

		SendPacketToAllEventRelatives(packet, calendarEvent);
	}

	void SendCalendarEventInviteAlert(CalendarEvent calendarEvent, CalendarInvite invite)
	{
		CalendarInviteAlert packet = new();
		packet.Date = calendarEvent.Date;
		packet.EventID = calendarEvent.EventId;
		packet.EventName = calendarEvent.Title;
		packet.EventType = calendarEvent.EventType;
		packet.Flags = calendarEvent.Flags;
		packet.InviteID = invite.InviteId;
		packet.InvitedByGuid = invite.SenderGuid;
		packet.ModeratorStatus = invite.Rank;
		packet.OwnerGuid = calendarEvent.OwnerGuid;
		packet.Status = invite.Status;
		packet.TextureID = calendarEvent.TextureId;

		var guild = Global.GuildMgr.GetGuildById(calendarEvent.GuildId);
		packet.EventGuildID = guild ? guild.GetGUID() : ObjectGuid.Empty;

		if (calendarEvent.IsGuildEvent || calendarEvent.IsGuildAnnouncement)
		{
			guild = Global.GuildMgr.GetGuildById(calendarEvent.GuildId);

			if (guild)
				guild.BroadcastPacket(packet);
		}
		else
		{
			var player = Global.ObjAccessor.FindPlayer(invite.InviteeGuid);

			if (player)
				player.SendPacket(packet);
		}
	}

	void SendCalendarEventInviteRemoveAlert(ObjectGuid guid, CalendarEvent calendarEvent, CalendarInviteStatus status)
	{
		var player = Global.ObjAccessor.FindPlayer(guid);

		if (player)
		{
			CalendarInviteRemovedAlert packet = new();
			packet.Date = calendarEvent.Date;
			packet.EventID = calendarEvent.EventId;
			packet.Flags = calendarEvent.Flags;
			packet.Status = status;

			player.SendPacket(packet);
		}
	}

	void SendPacketToAllEventRelatives(ServerPacket packet, CalendarEvent calendarEvent)
	{
		// Send packet to all guild members
		if (calendarEvent.IsGuildEvent || calendarEvent.IsGuildAnnouncement)
		{
			var guild = Global.GuildMgr.GetGuildById(calendarEvent.GuildId);

			if (guild)
				guild.BroadcastPacket(packet);
		}

		// Send packet to all invitees if event is non-guild, in other case only to non-guild invitees (packet was broadcasted for them)
		var invites = _invites[calendarEvent.EventId];

		foreach (var playerCalendarEvent in invites)
		{
			var player = Global.ObjAccessor.FindPlayer(playerCalendarEvent.InviteeGuid);

			if (player)
				if (!calendarEvent.IsGuildEvent || player.GuildId != calendarEvent.GuildId)
					player.SendPacket(packet);
		}
	}
}