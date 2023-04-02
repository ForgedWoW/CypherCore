// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Who;

public class WhoIsResponse : ServerPacket
{
    public string AccountName;
    public WhoIsResponse() : base(ServerOpcodes.WhoIs) { }

    public override void Write()
    {
        WorldPacket.WriteBits(AccountName.GetByteCount(), 11);
        WorldPacket.WriteString(AccountName);
    }
}