// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Taxi;

public class ShowTaxiNodes : ServerPacket
{
    public byte[] CanLandNodes = null;

    // Nodes known by player
    public byte[] CanUseNodes = null;

    public ShowTaxiNodesWindowInfo? WindowInfo;

    // Nodes available for use - this can temporarily disable a known node
    public ShowTaxiNodes() : base(ServerOpcodes.ShowTaxiNodes) { }

    public override void Write()
    {
        WorldPacket.WriteBit(WindowInfo.HasValue);
        WorldPacket.FlushBits();

        WorldPacket.WriteInt32(CanLandNodes.Length / 8); // client reads this in uint64 blocks, size is ensured to be divisible by 8 in TaxiMask constructor
        WorldPacket.WriteInt32(CanUseNodes.Length / 8);  // client reads this in uint64 blocks, size is ensured to be divisible by 8 in TaxiMask constructor

        if (WindowInfo.HasValue)
        {
            WorldPacket.WritePackedGuid(WindowInfo.Value.UnitGUID);
            WorldPacket.WriteInt32(WindowInfo.Value.CurrentNode);
        }

        foreach (var node in CanLandNodes)
            WorldPacket.WriteUInt8(node);

        foreach (var node in CanUseNodes)
            WorldPacket.WriteUInt8(node);
    }
}