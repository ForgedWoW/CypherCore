// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Addon;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionReplicateItems : ClientPacket
{
    public ObjectGuid Auctioneer;
    public uint ChangeNumberCursor;
    public uint ChangeNumberGlobal;
    public uint ChangeNumberTombstone;
    public uint Count;
    public AddOnInfo? TaintedBy;

    public AuctionReplicateItems(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Auctioneer = _worldPacket.ReadPackedGuid();
        ChangeNumberGlobal = _worldPacket.ReadUInt32();
        ChangeNumberCursor = _worldPacket.ReadUInt32();
        ChangeNumberTombstone = _worldPacket.ReadUInt32();
        Count = _worldPacket.ReadUInt32();

        if (_worldPacket.HasBit())
        {
            TaintedBy = new AddOnInfo();
            TaintedBy.Value.Read(_worldPacket);
        }
    }
}