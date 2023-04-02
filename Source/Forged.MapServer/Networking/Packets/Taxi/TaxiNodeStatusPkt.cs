// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Taxi;

internal class TaxiNodeStatusPkt : ServerPacket
{
    public TaxiNodeStatus Status; // replace with TaxiStatus enum
    public ObjectGuid Unit;
    public TaxiNodeStatusPkt() : base(ServerOpcodes.TaxiNodeStatus) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Unit);
        WorldPacket.WriteBits(Status, 2);
        WorldPacket.FlushBits();
    }
}