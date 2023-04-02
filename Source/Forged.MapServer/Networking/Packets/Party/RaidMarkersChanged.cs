// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Groups;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

internal class RaidMarkersChanged : ServerPacket
{
    public uint ActiveMarkers;
    public sbyte PartyIndex;
    public List<RaidMarker> RaidMarkers = new();
    public RaidMarkersChanged() : base(ServerOpcodes.RaidMarkersChanged) { }

    public override void Write()
    {
        WorldPacket.WriteInt8(PartyIndex);
        WorldPacket.WriteUInt32(ActiveMarkers);

        WorldPacket.WriteBits(RaidMarkers.Count, 4);
        WorldPacket.FlushBits();

        foreach (var raidMarker in RaidMarkers)
        {
            WorldPacket.WritePackedGuid(raidMarker.TransportGUID);
            WorldPacket.WriteUInt32(raidMarker.Location.MapId);
            WorldPacket.WriteXYZ(raidMarker.Location);
        }
    }
}