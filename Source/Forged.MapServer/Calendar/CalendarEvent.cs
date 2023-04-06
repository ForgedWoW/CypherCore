// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Calendar;

public class CalendarEvent
{
    public CalendarEvent(CalendarEvent calendarEvent, ulong eventId)
    {
        EventId = eventId;
        OwnerGuid = calendarEvent.OwnerGuid;
        GuildId = calendarEvent.GuildId;
        EventType = calendarEvent.EventType;
        TextureId = calendarEvent.TextureId;
        Date = calendarEvent.Date;
        Flags = calendarEvent.Flags;
        LockDate = calendarEvent.LockDate;
        Title = calendarEvent.Title;
        Description = calendarEvent.Description;
    }

    public CalendarEvent(ulong eventId, ObjectGuid ownerGuid, ulong guildId, CalendarEventType type, int textureId, long date, CalendarFlags flags, string title, string description, long lockDate)
    {
        EventId = eventId;
        OwnerGuid = ownerGuid;
        GuildId = guildId;
        EventType = type;
        TextureId = textureId;
        Date = date;
        Flags = flags;
        LockDate = lockDate;
        Title = title;
        Description = description;
    }

    public CalendarEvent()
    {
        EventId = 1;
        EventType = CalendarEventType.Other;
        TextureId = -1;
        Title = "";
        Description = "";
    }

    public long Date { get; set; }
    public string Description { get; set; }
    public ulong EventId { get; set; }
    public CalendarEventType EventType { get; set; }
    public CalendarFlags Flags { get; set; }
    public ulong GuildId { get; set; }
    public bool IsGuildAnnouncement => Flags.HasAnyFlag(CalendarFlags.WithoutInvites);
    public bool IsGuildEvent => Flags.HasAnyFlag(CalendarFlags.GuildEvent);
    public bool IsLocked => Flags.HasAnyFlag(CalendarFlags.InvitesLocked);
    public long LockDate { get; set; }
    public ObjectGuid OwnerGuid { get; set; }
    public int TextureId { get; set; }
    public string Title { get; set; }

    public static bool ModifyIsGuildAnnouncementFlags(uint flags)
    {
        return (flags & (uint)CalendarFlags.WithoutInvites) != 0;
    }

    public static bool ModifyIsGuildEventFlags(uint flags)
    {
        return (flags & (uint)CalendarFlags.GuildEvent) != 0;
    }

    public string BuildCalendarMailBody()
    {
        var now = Time.UnixTimeToDateTime(Date);
        var time = Convert.ToUInt32(((now.Year - 1900) - 100) << 24 | (now.Month - 1) << 20 | (now.Day - 1) << 14 | (int)now.DayOfWeek << 11 | now.Hour << 6 | now.Minute);

        return time.ToString();
    }

    public string BuildCalendarMailSubject(ObjectGuid remover)
    {
        return remover + ":" + Title;
    }
}