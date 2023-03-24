// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Query;

public class QuestPOIQuery : ClientPacket
{
	public int MissingQuestCount;
	public uint[] MissingQuestPOIs = new uint[125];
	public QuestPOIQuery(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		MissingQuestCount = _worldPacket.ReadInt32();

		for (byte i = 0; i < MissingQuestCount; ++i)
			MissingQuestPOIs[i] = _worldPacket.ReadUInt32();
	}
}
