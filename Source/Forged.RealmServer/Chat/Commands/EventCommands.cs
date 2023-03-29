// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Chat;

[CommandGroup("event")]
class EventCommands
{
	[Command("info", RBACPermissions.CommandEventInfo, true)]
	static bool HandleEventInfoCommand(CommandHandler handler, ushort eventId)
	{
		var events = _gameEventManager.GetEventMap();

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

		var activeEvents = _gameEventManager.GetActiveEventList();
		var active = activeEvents.Contains(eventId);
		var activeStr = active ? Global.ObjectMgr.GetCypherString(CypherStrings.Active) : "";

		var startTimeStr = Time.UnixTimeToDateTime(eventData.start).ToLongDateString();
		var endTimeStr = Time.UnixTimeToDateTime(eventData.end).ToLongDateString();

		var delay = _gameEventManager.NextCheck(eventId);
		var nextTime = _gameTime.GetGameTime + delay;
		var nextStr = nextTime >= eventData.start && nextTime < eventData.end ? Time.UnixTimeToDateTime(_gameTime.GetGameTime + delay).ToShortTimeString() : "-";

		var occurenceStr = Time.secsToTimeString(eventData.occurence * Time.Minute);
		var lengthStr = Time.secsToTimeString(eventData.length * Time.Minute);

		handler.SendSysMessage(CypherStrings.EventInfo,
								eventId,
								eventData.description,
								activeStr,
								startTimeStr,
								endTimeStr,
								occurenceStr,
								lengthStr,
								nextStr);

		return true;
	}

	[Command("activelist", RBACPermissions.CommandEventActivelist, true)]
	static bool HandleEventActiveListCommand(CommandHandler handler)
	{
		uint counter = 0;

		var events = _gameEventManager.GetEventMap();
		var activeEvents = _gameEventManager.GetActiveEventList();

		var active = Global.ObjectMgr.GetCypherString(CypherStrings.Active);

		foreach (var eventId in activeEvents)
		{
			var eventData = events[eventId];

			if (handler.Session != null)
				handler.SendSysMessage(CypherStrings.EventEntryListChat, eventId, eventId, eventData.description, active);
			else
				handler.SendSysMessage(CypherStrings.EventEntryListConsole, eventId, eventData.description, active);

			++counter;
		}

		if (counter == 0)
			handler.SendSysMessage(CypherStrings.Noeventfound);

		return true;
	}

	[Command("start", RBACPermissions.CommandEventStart, true)]
	static bool HandleEventStartCommand(CommandHandler handler, ushort eventId)
	{
		var events = _gameEventManager.GetEventMap();

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

		var activeEvents = _gameEventManager.GetActiveEventList();

		if (activeEvents.Contains(eventId))
		{
			handler.SendSysMessage(CypherStrings.EventAlreadyActive, eventId);

			return false;
		}

		_gameEventManager.StartEvent(eventId, true);

		return true;
	}

	[Command("stop", RBACPermissions.CommandEventStop, true)]
	static bool HandleEventStopCommand(CommandHandler handler, ushort eventId)
	{
		var events = _gameEventManager.GetEventMap();

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

		var activeEvents = _gameEventManager.GetActiveEventList();

		if (!activeEvents.Contains(eventId))
		{
			handler.SendSysMessage(CypherStrings.EventNotActive, eventId);

			return false;
		}

		_gameEventManager.StopEvent(eventId, true);

		return true;
	}
}