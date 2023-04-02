// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildEventTabModified : ServerPacket
{
    public string Icon;
    public string Name;
    public int Tab;
    public GuildEventTabModified() : base(ServerOpcodes.GuildEventTabModified) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(Tab);

        WorldPacket.WriteBits(Name.GetByteCount(), 7);
        WorldPacket.WriteBits(Icon.GetByteCount(), 9);
        WorldPacket.FlushBits();

        WorldPacket.WriteString(Name);
        WorldPacket.WriteString(Icon);
    }
}