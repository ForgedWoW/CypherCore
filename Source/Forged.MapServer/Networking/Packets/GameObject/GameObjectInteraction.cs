// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.GameObject;

class GameObjectInteraction : ServerPacket
{
	public ObjectGuid ObjectGUID;
	public PlayerInteractionType InteractionType;

	public GameObjectInteraction() : base(ServerOpcodes.GameObjectInteraction) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ObjectGUID);
		_worldPacket.WriteInt32((int)InteractionType);
	}
}