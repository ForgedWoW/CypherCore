// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Quest;

public class QuestGiverCompleteQuest : ClientPacket
{
	public ObjectGuid QuestGiverGUID; // NPC / GameObject guid for normal quest completion. Player guid for self-completed quests
	public uint QuestID;
	public bool FromScript; // 0 - standart complete quest mode with npc, 1 - auto-complete mode
	public QuestGiverCompleteQuest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		QuestGiverGUID = _worldPacket.ReadPackedGuid();
		QuestID = _worldPacket.ReadUInt32();
		FromScript = _worldPacket.HasBit();
	}
}
