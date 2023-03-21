// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer;

public class CalendarInvite
{
	public ulong InviteId { get; set; }
	public ulong EventId { get; set; }
	public ObjectGuid InviteeGuid { get; set; }
	public ObjectGuid SenderGuid { get; set; }
	public long ResponseTime { get; set; }
	public CalendarInviteStatus Status { get; set; }
	public CalendarModerationRank Rank { get; set; }
	public string Note { get; set; }

	public CalendarInvite()
	{
		InviteId = 1;
		ResponseTime = 0;
		Status = CalendarInviteStatus.Invited;
		Rank = CalendarModerationRank.Player;
		Note = "";
	}

	public CalendarInvite(CalendarInvite calendarInvite, ulong inviteId, ulong eventId)
	{
		InviteId = inviteId;
		EventId = eventId;
		InviteeGuid = calendarInvite.InviteeGuid;
		SenderGuid = calendarInvite.SenderGuid;
		ResponseTime = calendarInvite.ResponseTime;
		Status = calendarInvite.Status;
		Rank = calendarInvite.Rank;
		Note = calendarInvite.Note;
	}

	public CalendarInvite(ulong inviteId, ulong eventId, ObjectGuid invitee, ObjectGuid senderGUID, long responseTime, CalendarInviteStatus status, CalendarModerationRank rank, string note)
	{
		InviteId = inviteId;
		EventId = eventId;
		InviteeGuid = invitee;
		SenderGuid = senderGUID;
		ResponseTime = responseTime;

		Status = status;
		Rank = rank;
		Note = note;
	}

	~CalendarInvite()
	{
		if (InviteId != 0 && EventId != 0)
			Global.CalendarMgr.FreeInviteId(InviteId);
	}
}