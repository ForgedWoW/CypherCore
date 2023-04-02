// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

public class AuctionReplicateResponse : ServerPacket
{
    public uint ChangeNumberCursor;
    public uint ChangeNumberGlobal;
    public uint ChangeNumberTombstone;
    public uint DesiredDelay;
    public List<AuctionItem> Items = new();
    public uint Result;
    public AuctionReplicateResponse() : base(ServerOpcodes.AuctionReplicateResponse) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(Result);
        WorldPacket.WriteUInt32(DesiredDelay);
        WorldPacket.WriteUInt32(ChangeNumberGlobal);
        WorldPacket.WriteUInt32(ChangeNumberCursor);
        WorldPacket.WriteUInt32(ChangeNumberTombstone);
        WorldPacket.WriteInt32(Items.Count);

        foreach (var item in Items)
            item.Write(WorldPacket);
    }
}