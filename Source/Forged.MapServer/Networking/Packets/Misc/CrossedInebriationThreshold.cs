// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

internal class CrossedInebriationThreshold : ServerPacket
{
    public ObjectGuid Guid;
    public uint ItemID;
    public uint Threshold;
    public CrossedInebriationThreshold() : base(ServerOpcodes.CrossedInebriationThreshold) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Guid);
        WorldPacket.WriteUInt32(Threshold);
        WorldPacket.WriteUInt32(ItemID);
    }
}