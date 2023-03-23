// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Items;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects.Update;
using Game.Entities;
using Game.Common.Entities.Players;
using Game.Common.Networking;

namespace Game.Common.Entities.Objects.Update;

public class ItemModList : BaseUpdateData<Item>
{
	public DynamicUpdateField<ItemMod> Values = new(0, 0);

	public ItemModList() : base(1) { }

	public void WriteCreate(WorldPacket data, Item owner, Player receiver)
	{
		data.WriteBits(Values.Size(), 6);

		for (var i = 0; i < Values.Size(); ++i)
			Values[i].WriteCreate(data, owner, receiver);

		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
	{
		var changesMask = ChangesMask;

		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlock(0), 1);

		if (changesMask[0])
			if (changesMask[0])
			{
				if (!ignoreChangesMask)
					Values.WriteUpdateMask(data, 6);
				else
					WriteCompleteDynamicFieldUpdateMask(Values.Size(), data, 6);
			}

		data.FlushBits();

		if (changesMask[0])
			if (changesMask[0])
				for (var i = 0; i < Values.Size(); ++i)
					if (Values.HasChanged(i) || ignoreChangesMask)
						Values[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);

		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Values);
		ChangesMask.ResetAll();
	}
}
