// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Taxi;

internal class TaxiQueryAvailableNodes : ClientPacket
{
    public ObjectGuid Unit;
    public TaxiQueryAvailableNodes(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Unit = WorldPacket.ReadPackedGuid();
    }
}