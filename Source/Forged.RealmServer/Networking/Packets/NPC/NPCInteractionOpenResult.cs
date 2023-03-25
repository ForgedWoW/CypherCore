// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class NPCInteractionOpenResult : ServerPacket
{
	public ObjectGuid Npc;
	public PlayerInteractionType InteractionType;
	public bool Success = true;
	public NPCInteractionOpenResult() : base(ServerOpcodes.NpcInteractionOpenResult) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Npc);
		_worldPacket.WriteInt32((int)InteractionType);
		_worldPacket.WriteBit(Success);
		_worldPacket.FlushBits();
	}
}