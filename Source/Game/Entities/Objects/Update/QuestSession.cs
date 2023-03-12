// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Networking;

namespace Game.Entities;

public class QuestSession : BaseUpdateData<Player>
{
	public UpdateField<ObjectGuid> Owner = new(0, 1);
	public UpdateFieldArray<ulong> QuestCompleted = new(875, 2, 3);

	public QuestSession() : base(878) { }

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WritePackedGuid(Owner);

		for (var i = 0; i < 875; ++i)
			data.WriteUInt64(QuestCompleted[i]);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		var changesMask = ChangesMask;

		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlocksMask(0), 28);

		for (uint i = 0; i < 28; ++i)
			if (changesMask.GetBlock(i) != 0)
				data.WriteBits(changesMask.GetBlock(i), 32);

		data.FlushBits();

		if (changesMask[0])
			if (changesMask[1])
				data.WritePackedGuid(Owner);

		if (changesMask[2])
			for (var i = 0; i < 875; ++i)
				if (changesMask[3 + i])
					data.WriteUInt64(QuestCompleted[i]);
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Owner);
		ClearChangesMask(QuestCompleted);
		ChangesMask.ResetAll();
	}
}