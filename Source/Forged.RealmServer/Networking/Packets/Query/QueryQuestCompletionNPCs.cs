// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

class QueryQuestCompletionNPCs : ClientPacket
{
	public uint[] QuestCompletionNPCs;
	public QueryQuestCompletionNPCs(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var questCount = _worldPacket.ReadUInt32();
		QuestCompletionNPCs = new uint[questCount];

		for (uint i = 0; i < questCount; ++i)
			QuestCompletionNPCs[i] = _worldPacket.ReadUInt32();
	}
}