// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class MoveSetCollisionHeightAck : ClientPacket
{
	public MovementAck Data;
	public UpdateCollisionHeightReason Reason;
	public uint MountDisplayID;
	public float Height = 1.0f;
	public MoveSetCollisionHeightAck(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		Data.Read(_worldPacket);
		Height = _worldPacket.ReadFloat();
		MountDisplayID = _worldPacket.ReadUInt32();
		Reason = (UpdateCollisionHeightReason)_worldPacket.ReadUInt8();
	}
}