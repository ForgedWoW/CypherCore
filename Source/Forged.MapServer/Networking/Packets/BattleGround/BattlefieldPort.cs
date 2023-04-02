// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.LFG;

namespace Forged.MapServer.Networking.Packets.BattleGround;

internal class BattlefieldPort : ClientPacket
{
    public bool AcceptedInvite;
    public RideTicket Ticket = new();
    public BattlefieldPort(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Ticket.Read(WorldPacket);
        AcceptedInvite = WorldPacket.HasBit();
    }
}