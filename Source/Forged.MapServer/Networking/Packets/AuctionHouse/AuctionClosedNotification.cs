// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AuctionHouse;

internal class AuctionClosedNotification : ServerPacket
{
    public AuctionOwnerNotification Info;
    public float ProceedsMailDelay;
    public bool Sold = true;

    public AuctionClosedNotification() : base(ServerOpcodes.AuctionClosedNotification) { }

    public override void Write()
    {
        Info.Write(WorldPacket);
        WorldPacket.WriteFloat(ProceedsMailDelay);
        WorldPacket.WriteBit(Sold);
        WorldPacket.FlushBits();
    }
}