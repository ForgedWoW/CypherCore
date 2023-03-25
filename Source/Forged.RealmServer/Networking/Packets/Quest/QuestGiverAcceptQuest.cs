// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class QuestGiverAcceptQuest : ClientPacket
{
	public ObjectGuid QuestGiverGUID;
	public uint QuestID;
	public bool StartCheat;

	public QuestGiverAcceptQuest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		QuestGiverGUID = _worldPacket.ReadPackedGuid();
		QuestID = _worldPacket.ReadUInt32();
		StartCheat = _worldPacket.HasBit();
	}
}