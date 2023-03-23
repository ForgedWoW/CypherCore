// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Entities;

public class SocketedGem : BaseUpdateData<Item>
{
	public UpdateField<uint> ItemId = new(0, 1);
	public UpdateField<byte> Context = new(0, 2);
	public UpdateFieldArray<ushort> BonusListIDs = new(16, 3, 4);

	public SocketedGem() : base(20) { }

	public void WriteCreate(WorldPacket data, Item owner, Player receiver)
	{
		data.WriteUInt32(ItemId);

		for (var i = 0; i < 16; ++i)
			data.WriteUInt16(BonusListIDs[i]);

		data.WriteUInt8(Context);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
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
				data.WriteUInt32(ItemId);

			if (changesMask[2])
				data.WriteUInt8(Context);
		}

		if (changesMask[3])
			for (var i = 0; i < 16; ++i)
				if (changesMask[4 + i])
					data.WriteUInt16(BonusListIDs[i]);
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(ItemId);
		ClearChangesMask(Context);
		ClearChangesMask(BonusListIDs);
		ChangesMask.ResetAll();
	}
}