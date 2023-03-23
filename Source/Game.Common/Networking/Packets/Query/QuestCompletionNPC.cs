// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Common.Networking.Packets.Query;

public class QuestCompletionNPC
{
	public uint QuestID;
	public List<uint> NPCs = new();
}
