// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Forged.MapServer.Events;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("event")]
internal class EventCommands
{
    [Command("activelist", RBACPermissions.CommandEventActivelist, true)]
    private static bool HandleEventActiveListCommand(CommandHandler handler)
    {
        uint counter = 0;
        var gameEventManager = handler.ClassFactory.Resolve<GameEventManager>();
        var events = gameEventManager.GetEventMap();
        var activeEvents = gameEventManager.GetActiveEventList();

        var active = handler.ObjectManager.CypherStringCache.GetCypherString(CypherStrings.Active);

        foreach (var eventId in activeEvents)
        {
            var eventData = events[eventId];

            if (handler.Session != null)
                handler.SendSysMessage(CypherStrings.EventEntryListChat, eventId, eventId, eventData.Description, active);
            else
                handler.SendSysMessage(CypherStrings.EventEntryListConsole, eventId, eventData.Description, active);

            ++counter;
        }

        if (counter == 0)
            handler.SendSysMessage(CypherStrings.Noeventfound);

        return true;
    }

    [Command("info", RBACPermissions.CommandEventInfo, true)]
    private static bool HandleEventInfoCommand(CommandHandler handler, ushort eventId)
    {
        var gameEventManager = handler.ClassFactory.Resolve<GameEventManager>();
        var events = gameEventManager.GetEventMap();

        if (eventId >= events.Length)
        {
            handler.SendSysMessage(CypherStrings.EventNotExist);

            return false;
        }

        var eventData = events[eventId];

        if (!eventData.IsValid())
        {
            handler.SendSysMessage(CypherStrings.EventNotExist);

            return false;
        }

        var activeEvents = gameEventManager.GetActiveEventList();
        var active = activeEvents.Contains(eventId);
        var activeStr = active ? handler.ObjectManager.CypherStringCache.GetCypherString(CypherStrings.Active) : "";

        var startTimeStr = Time.UnixTimeToDateTime(eventData.Start).ToLongDateString();
        var endTimeStr = Time.UnixTimeToDateTime(eventData.End).ToLongDateString();

        var delay = gameEventManager.NextCheck(eventId);
        var nextTime = GameTime.CurrentTime + delay;
        var nextStr = nextTime >= eventData.Start && nextTime < eventData.End ? Time.UnixTimeToDateTime(GameTime.CurrentTime + delay).ToShortTimeString() : "-";

        var occurenceStr = Time.SecsToTimeString(eventData.Occurence * Time.MINUTE);
        var lengthStr = Time.SecsToTimeString(eventData.Length * Time.MINUTE);

        handler.SendSysMessage(CypherStrings.EventInfo,
                               eventId,
                               eventData.Description,
                               activeStr,
                               startTimeStr,
                               endTimeStr,
                               occurenceStr,
                               lengthStr,
                               nextStr);

        return true;
    }

    [Command("start", RBACPermissions.CommandEventStart, true)]
    private static bool HandleEventStartCommand(CommandHandler handler, ushort eventId)
    {
        var gameEventManager = handler.ClassFactory.Resolve<GameEventManager>();
        var events = gameEventManager.GetEventMap();

        if (eventId < 1 || eventId >= events.Length)
        {
            handler.SendSysMessage(CypherStrings.EventNotExist);

            return false;
        }

        var eventData = events[eventId];

        if (!eventData.IsValid())
        {
            handler.SendSysMessage(CypherStrings.EventNotExist);

            return false;
        }

        var activeEvents = gameEventManager.GetActiveEventList();

        if (activeEvents.Contains(eventId))
        {
            handler.SendSysMessage(CypherStrings.EventAlreadyActive, eventId);

            return false;
        }

        gameEventManager.StartEvent(eventId, true);

        return true;
    }

    [Command("stop", RBACPermissions.CommandEventStop, true)]
    private static bool HandleEventStopCommand(CommandHandler handler, ushort eventId)
    {
        var gameEventManager = handler.ClassFactory.Resolve<GameEventManager>();
        var events = gameEventManager.GetEventMap();

        if (eventId < 1 || eventId >= events.Length)
        {
            handler.SendSysMessage(CypherStrings.EventNotExist);

            return false;
        }

        var eventData = events[eventId];

        if (!eventData.IsValid())
        {
            handler.SendSysMessage(CypherStrings.EventNotExist);

            return false;
        }

        var activeEvents = gameEventManager.GetActiveEventList();

        if (!activeEvents.Contains(eventId))
        {
            handler.SendSysMessage(CypherStrings.EventNotActive, eventId);

            return false;
        }

        gameEventManager.StopEvent(eventId, true);

        return true;
    }
}