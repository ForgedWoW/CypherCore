// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.GameObject;

internal class GameObjectCustomAnim : ServerPacket
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