// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game;

public enum ServerMessageType
{
	ShutdownTime = 1,
	RestartTime = 2,
	String = 3,
	ShutdownCancelled = 4,
	RestartCancelled = 5,
	BgShutdownTime = 6,
	BgRestartTime = 7,
	InstanceShutdownTime = 8,
	InstanceRestartTime = 9,
	ContentReady = 10,
	TicketServicedSoon = 11,
	WaitTimeUnavailable = 12,
	TicketWaitTime = 13,
}