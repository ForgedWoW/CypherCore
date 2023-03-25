// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Networking.Packets;

class RaidInstanceMessage : ServerPacket
{
	public InstanceResetWarningType Type;
	public uint MapID;
	public Difficulty DifficultyID;
	public bool Locked;
	public bool Extended;
	public RaidInstanceMessage() : base(ServerOpcodes.RaidInstanceMessage) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Type);
		_worldPacket.WriteUInt32(MapID);
		_worldPacket.WriteUInt32((uint)DifficultyID);
		_worldPacket.WriteBit(Locked);
		_worldPacket.WriteBit(Extended);
		_worldPacket.FlushBits();
	}
}