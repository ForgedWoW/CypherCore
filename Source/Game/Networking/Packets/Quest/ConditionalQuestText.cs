// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Game.Networking.Packets;

public class ConditionalQuestText
{
	public int PlayerConditionID;
	public int QuestGiverCreatureID;
	public string Text = "";

	public ConditionalQuestText(int playerConditionID, int questGiverCreatureID, string text)
	{
		PlayerConditionID = playerConditionID;
		QuestGiverCreatureID = questGiverCreatureID;
		Text = text;
	}

	public void Write(WorldPacket data)
	{
		data.WriteInt32(PlayerConditionID);
		data.WriteInt32(QuestGiverCreatureID);
		data.WriteBits(Text.GetByteCount(), 12);
		data.FlushBits();

		data.WriteString(Text);
	}
}