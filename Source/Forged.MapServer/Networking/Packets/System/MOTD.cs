// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.System;

public class MOTD : ServerPacket
{
    public List<string> Text;
    public MOTD() : base(ServerOpcodes.Motd) { }

    public override void Write()
    {
        WorldPacket.WriteBits(Text.Count, 4);
        WorldPacket.FlushBits();

        foreach (var line in Text)
        {
            WorldPacket.WriteBits(line.GetByteCount(), 7);
            WorldPacket.FlushBits();
            WorldPacket.WriteString(line);
        }
    }
}