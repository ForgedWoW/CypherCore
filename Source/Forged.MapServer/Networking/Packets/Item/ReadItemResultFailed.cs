// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class ReadItemResultFailed : ServerPacket
{
    public uint Delay;
    public ObjectGuid Item;
    public byte Subcode;
    public ReadItemResultFailed() : base(ServerOpcodes.ReadItemResultFailed) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Item);
        WorldPacket.WriteUInt32(Delay);
        WorldPacket.WriteBits(Subcode, 2);
        WorldPacket.FlushBits();
    }
}