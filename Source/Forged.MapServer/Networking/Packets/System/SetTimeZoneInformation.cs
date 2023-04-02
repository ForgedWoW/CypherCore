// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.System;

public class SetTimeZoneInformation : ServerPacket
{
    public string GameTimeTZ;
    public string ServerRegionalTZ;
    public string ServerTimeTZ;
    public SetTimeZoneInformation() : base(ServerOpcodes.SetTimeZoneInformation) { }

    public override void Write()
    {
        WorldPacket.WriteBits(ServerTimeTZ.GetByteCount(), 7);
        WorldPacket.WriteBits(GameTimeTZ.GetByteCount(), 7);
        WorldPacket.WriteBits(ServerRegionalTZ.GetByteCount(), 7);
        WorldPacket.FlushBits();

        WorldPacket.WriteString(ServerTimeTZ);
        WorldPacket.WriteString(GameTimeTZ);
        WorldPacket.WriteString(ServerRegionalTZ);
    }
}