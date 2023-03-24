// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Quest;

public class UiMapQuestLinesResponse : ServerPacket
{
	public int UiMapID;
	public List<uint> QuestLineXQuestIDs = new();

	public UiMapQuestLinesResponse() : base(ServerOpcodes.UiMapQuestLinesResponse) { }

	public override void Write()
	{
		_worldPacket.Write(UiMapID);
		_worldPacket.WriteUInt32((uint)QuestLineXQuestIDs.Count);

		foreach (var item in QuestLineXQuestIDs)
			_worldPacket.Write(item);
	}
}
