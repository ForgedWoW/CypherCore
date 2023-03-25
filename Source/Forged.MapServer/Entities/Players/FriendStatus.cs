// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Players;

public enum FriendStatus
{
	Offline = 0x00,
	Online = 0x01,
	AFK = 0x02,
	DND = 0x04,
	RAF = 0x08
}