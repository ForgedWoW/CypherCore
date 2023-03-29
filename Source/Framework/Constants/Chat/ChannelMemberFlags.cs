// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ChannelMemberFlags
{
    None = 0x00,
    Owner = 0x01,
    Moderator = 0x02,
    Voiced = 0x04,
    Muted = 0x08,
    Custom = 0x10,

    MicMuted = 0x20
    // 0x40
    // 0x80
}