// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class ConversationLineStarted : ClientPacket
{
	public ObjectGuid ConversationGUID;
	public uint LineID;

	public ConversationLineStarted(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ConversationGUID = _worldPacket.ReadPackedGuid();
		LineID = _worldPacket.ReadUInt32();
	}
}