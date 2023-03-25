// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class MoveSetCollisionHeight : ServerPacket
{
	public float Scale = 1.0f;
	public ObjectGuid MoverGUID;
	public uint MountDisplayID;
	public UpdateCollisionHeightReason Reason;
	public uint SequenceIndex;
	public int ScaleDuration;
	public float Height = 1.0f;
	public MoveSetCollisionHeight() : base(ServerOpcodes.MoveSetCollisionHeight) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(MoverGUID);
		_worldPacket.WriteUInt32(SequenceIndex);
		_worldPacket.WriteFloat(Height);
		_worldPacket.WriteFloat(Scale);
		_worldPacket.WriteUInt8((byte)Reason);
		_worldPacket.WriteUInt32(MountDisplayID);
		_worldPacket.WriteInt32(ScaleDuration);
	}
}