// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.VoidStorage;

internal class VoidItemSwapResponse : ServerPacket
{
    public ObjectGuid VoidItemA;
    public ObjectGuid VoidItemB;
    public uint VoidItemSlotA;
    public uint VoidItemSlotB;
    public VoidItemSwapResponse() : base(ServerOpcodes.VoidItemSwapResponse, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(VoidItemA);
        WorldPacket.WriteUInt32(VoidItemSlotA);
        WorldPacket.WritePackedGuid(VoidItemB);
        WorldPacket.WriteUInt32(VoidItemSlotB);
    }
}