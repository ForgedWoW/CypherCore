// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class GameObjectCloseInteraction : ServerPacket
{
	public PlayerInteractionType InteractionType;

	public GameObjectCloseInteraction() : base(ServerOpcodes.GameObjectCloseInteraction) { }

	public override void Write()
	{
		_worldPacket.WriteInt32((int)InteractionType);
	}
}