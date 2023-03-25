// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class CriteriaUpdate : ServerPacket
{
	public uint CriteriaID;
	public ulong Quantity;
	public ObjectGuid PlayerGUID;
	public uint Flags;
	public long CurrentTime;
	public long ElapsedTime;
	public long CreationTime;
	public ulong? RafAcceptanceID;
	public CriteriaUpdate() : base(ServerOpcodes.CriteriaUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(CriteriaID);
		_worldPacket.WriteUInt64(Quantity);
		_worldPacket.WritePackedGuid(PlayerGUID);
		_worldPacket.WriteUInt32(Flags);
		_worldPacket.WritePackedTime(CurrentTime);
		_worldPacket.WriteInt64(ElapsedTime);
		_worldPacket.WriteInt64(CreationTime);
		_worldPacket.WriteBit(RafAcceptanceID.HasValue);
		_worldPacket.FlushBits();

		if (RafAcceptanceID.HasValue)
			_worldPacket.WriteUInt64(RafAcceptanceID.Value);
	}
}