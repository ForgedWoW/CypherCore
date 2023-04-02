// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class LFGProposalUpdate : ServerPacket
{
    public uint CompletedMask;
    public uint EncounterMask;
    public ulong InstanceID;
    public bool IsRequeue;
    public List<LFGProposalUpdatePlayer> Players = new();
    public uint ProposalID;
    public bool ProposalSilent;
    public uint Slot;
    public byte State;
    public RideTicket Ticket;
    public byte Unused;
    public bool ValidCompletedMask;
    public LFGProposalUpdate() : base(ServerOpcodes.LfgProposalUpdate) { }

    public override void Write()
    {
        Ticket.Write(WorldPacket);

        WorldPacket.WriteUInt64(InstanceID);
        WorldPacket.WriteUInt32(ProposalID);
        WorldPacket.WriteUInt32(Slot);
        WorldPacket.WriteUInt8(State);
        WorldPacket.WriteUInt32(CompletedMask);
        WorldPacket.WriteUInt32(EncounterMask);
        WorldPacket.WriteInt32(Players.Count);
        WorldPacket.WriteUInt8(Unused);
        WorldPacket.WriteBit(ValidCompletedMask);
        WorldPacket.WriteBit(ProposalSilent);
        WorldPacket.WriteBit(IsRequeue);
        WorldPacket.FlushBits();

        foreach (var player in Players)
            player.Write(WorldPacket);
    }
}