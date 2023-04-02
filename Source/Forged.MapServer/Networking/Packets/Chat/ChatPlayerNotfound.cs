// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

internal class ChatPlayerNotfound : ServerPacket
{
    private readonly string Name;

    public ChatPlayerNotfound(string name) : base(ServerOpcodes.ChatPlayerNotfound)
    {
        Name = name;
    }

    public override void Write()
    {
        WorldPacket.WriteBits(Name.GetByteCount(), 9);
        WorldPacket.WriteString(Name);
    }
}