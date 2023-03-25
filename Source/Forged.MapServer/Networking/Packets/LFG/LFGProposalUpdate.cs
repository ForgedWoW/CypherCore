// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

class LFGProposalUpdate : ServerPacket
{
	public RideTicket Ticket;
	public ulong InstanceID;
	public uint ProposalID;
	public uint Slot;
	public byte State;
	public uint CompletedMask;
	public uint EncounterMask;
	public byte Unused;
	public bool ValidCompletedMask;
	public bool ProposalSilent;
	public bool IsRequeue;
	public List<LFGProposalUpdatePlayer> Players = new();
	public LFGProposalUpdate() : base(ServerOpcodes.LfgProposalUpdate) { }

	public override void Write()
	{
		Ticket.Write(_worldPacket);

		_worldPacket.WriteUInt64(InstanceID);
		_worldPacket.WriteUInt32(ProposalID);
		_worldPacket.WriteUInt32(Slot);
		_worldPacket.WriteUInt8(State);
		_worldPacket.WriteUInt32(CompletedMask);
		_worldPacket.WriteUInt32(EncounterMask);
		_worldPacket.WriteInt32(Players.Count);
		_worldPacket.WriteUInt8(Unused);
		_worldPacket.WriteBit(ValidCompletedMask);
		_worldPacket.WriteBit(ProposalSilent);
		_worldPacket.WriteBit(IsRequeue);
		_worldPacket.FlushBits();

		foreach (var player in Players)
			player.Write(_worldPacket);
	}
}