// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.LFG;

internal class DFProposalResponse : ClientPacket
{
    public bool Accepted;
    public ulong InstanceID;
    public uint ProposalID;
    public RideTicket Ticket = new();
    public DFProposalResponse(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Ticket.Read(WorldPacket);
        InstanceID = WorldPacket.ReadUInt64();
        ProposalID = WorldPacket.ReadUInt32();
        Accepted = WorldPacket.HasBit();
    }
}