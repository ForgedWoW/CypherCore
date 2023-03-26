// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class LFGUpdateStatus : ServerPacket
{
	public RideTicket Ticket = new();
	public byte SubType;
	public byte Reason;
	public List<uint> Slots = new();
	public uint RequestedRoles;
	public List<ObjectGuid> SuspendedPlayers = new();
	public uint QueueMapID;
	public bool NotifyUI;
	public bool IsParty;
	public bool Joined;
	public bool LfgJoined;
	public bool Queued;
	public bool Unused;
	public LFGUpdateStatus() : base(ServerOpcodes.LfgUpdateStatus) { }

	public override void Write()
	{
		Ticket.Write(_worldPacket);

		_worldPacket.WriteUInt8(SubType);
		_worldPacket.WriteUInt8(Reason);
		_worldPacket.WriteInt32(Slots.Count);
		_worldPacket.WriteUInt32(RequestedRoles);
		_worldPacket.WriteInt32(SuspendedPlayers.Count);
		_worldPacket.WriteUInt32(QueueMapID);

		foreach (var slot in Slots)
			_worldPacket.WriteUInt32(slot);

		foreach (var player in SuspendedPlayers)
			_worldPacket.WritePackedGuid(player);

		_worldPacket.WriteBit(IsParty);
		_worldPacket.WriteBit(NotifyUI);
		_worldPacket.WriteBit(Joined);
		_worldPacket.WriteBit(LfgJoined);
		_worldPacket.WriteBit(Queued);
		_worldPacket.WriteBit(Unused);
		_worldPacket.FlushBits();
	}
}