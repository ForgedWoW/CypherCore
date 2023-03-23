// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Items;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Objects.Update;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class ContainerData : BaseUpdateData<Bag>
{
	public UpdateField<uint> NumSlots = new(0, 1);
	public UpdateFieldArray<ObjectGuid> Slots = new(36, 2, 3);

	public ContainerData() : base(0, TypeId.Container, 39) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Bag owner, Player receiver)
	{
		for (var i = 0; i < 36; ++i)
			data.WritePackedGuid(Slots[i]);

		data.WriteUInt32(NumSlots);
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Bag owner, Player receiver)
	{
		WriteUpdate(data, ChangesMask, false, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Bag owner, Player receiver)
	{
		data.WriteBits(ChangesMask.GetBlocksMask(0), 2);

		for (uint i = 0; i < 2; ++i)
			if (ChangesMask.GetBlock(i) != 0)
				data.WriteBits(ChangesMask.GetBlock(i), 32);

		data.FlushBits();

		if (ChangesMask[0])
			if (ChangesMask[1])
				data.WriteUInt32(NumSlots);

		if (ChangesMask[2])
			for (var i = 0; i < 36; ++i)
				if (ChangesMask[3 + i])
					data.WritePackedGuid(Slots[i]);
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(NumSlots);
		ClearChangesMask(Slots);
		ChangesMask.ResetAll();
	}
}
