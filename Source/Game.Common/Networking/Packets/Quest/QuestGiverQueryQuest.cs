// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class QuestGiverQueryQuest : ClientPacket
{
	public ObjectGuid QuestGiverGUID;
	public uint QuestID;
	public bool RespondToGiver;
	public QuestGiverQueryQuest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		QuestGiverGUID = _worldPacket.ReadPackedGuid();
		QuestID = _worldPacket.ReadUInt32();
		RespondToGiver = _worldPacket.HasBit();
	}
}