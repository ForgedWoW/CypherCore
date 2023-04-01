// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Entities.Players;

[Flags]
public enum FriendStatus
{
    Offline = 0x00,
    Online = 0x01,
    Afk = 0x02,
    Dnd = 0x04,
    Raf = 0x08
}