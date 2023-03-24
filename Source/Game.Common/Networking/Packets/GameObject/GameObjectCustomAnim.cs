// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.GameObject;

public class GameObjectCustomAnim : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public uint CustomAnim;
	public bool PlayAsDespawn;
	public GameObjectCustomAnim() : base(ServerOpcodes.GameObjectCustomAnim, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
		_worldPacket.WriteUInt32(CustomAnim);
		_worldPacket.WriteBit(PlayAsDespawn);
		_worldPacket.FlushBits();
	}
}
