// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.Objects.Update;

public class QuestLog : BaseUpdateData<Player>
{
	public UpdateField<uint> QuestID = new(0, 1);
	public UpdateField<uint> StateFlags = new(0, 2);
	public UpdateField<uint> EndTime = new(0, 3);
	public UpdateField<uint> AcceptTime = new(0, 4);
	public UpdateField<uint> ObjectiveFlags = new(0, 5);
	public UpdateFieldArray<ushort> ObjectiveProgress = new(24, 6, 7);

	public QuestLog() : base(31) { }

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteUInt32(QuestID);
		data.WriteUInt32(StateFlags);
		data.WriteUInt32(EndTime);
		data.WriteUInt32(AcceptTime);
		data.WriteUInt32(ObjectiveFlags);

		for (var i = 0; i < 24; ++i)
			data.WriteUInt16(ObjectiveProgress[i]);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		var changesMask = ChangesMask;

		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlocksMask(0), 1);

		if (changesMask.GetBlock(0) != 0)
			data.WriteBits(changesMask.GetBlock(0), 32);

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[1])
				data.WriteUInt32(QuestID);

			if (changesMask[2])
				data.WriteUInt32(StateFlags);

			if (changesMask[3])
				data.WriteUInt32(EndTime);

			if (changesMask[4])
				data.WriteUInt32(AcceptTime);

			if (changesMask[5])
				data.WriteUInt32(ObjectiveFlags);
		}

		if (changesMask[6])
			for (var i = 0; i < 24; ++i)
				if (changesMask[7 + i])
					data.WriteUInt16(ObjectiveProgress[i]);
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(QuestID);
		ClearChangesMask(StateFlags);
		ClearChangesMask(EndTime);
		ClearChangesMask(AcceptTime);
		ClearChangesMask(ObjectiveFlags);
		ClearChangesMask(ObjectiveProgress);
		ChangesMask.ResetAll();
	}
}