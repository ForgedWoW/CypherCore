// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class QuestGiverChooseReward : ClientPacket
{
	public ObjectGuid QuestGiverGUID;
	public uint QuestID;
	public QuestChoiceItem Choice;
	public QuestGiverChooseReward(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		QuestGiverGUID = _worldPacket.ReadPackedGuid();
		QuestID = _worldPacket.ReadUInt32();
		Choice.Read(_worldPacket);
	}
}