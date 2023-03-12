// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Dynamic;

public class RepeatEvent : BasicEvent
{
	readonly Func<TimeSpan> _event;
	readonly EventSystem _eventSystem;

	public RepeatEvent(EventSystem eventSystem, Func<TimeSpan> func) : base()
	{
		_event = func;
		_eventSystem = eventSystem;
	}

	public override bool Execute(ulong etime, uint pTime)
	{
		var ts = _event.Invoke();

		if (ts != default)
			_eventSystem.AddEventAtOffset(this, ts);

		return true;
	}
}