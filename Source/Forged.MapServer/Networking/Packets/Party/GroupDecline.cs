// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class GroupDecline : ServerPacket
{
    public string Name;

    public GroupDecline(string name) : base(ServerOpcodes.GroupDecline)
    {
        Name = name;
    }

    public override void Write()
    {
        WorldPacket.WriteBits(Name.GetByteCount(), 9);
        WorldPacket.FlushBits();
        WorldPacket.WriteString(Name);
    }
}