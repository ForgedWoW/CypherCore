// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects.Update;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class ReplayedQuest : BaseUpdateData<Player>
{
	public UpdateField<int> QuestID = new(0, 1);
	public UpdateField<uint> ReplayTime = new(0, 2);

	public ReplayedQuest() : base(3) { }

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteInt32(QuestID);
		data.WriteUInt32(ReplayTime);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		var changesMask = ChangesMask;

		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlock(0), 3);

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[1])
				data.WriteInt32(QuestID);

			if (changesMask[2])
				data.WriteUInt32(ReplayTime);
		}
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(QuestID);
		ClearChangesMask(ReplayTime);
		ChangesMask.ResetAll();
	}
}
