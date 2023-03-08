using Framework.Constants;
using Game.Networking;

namespace Game.Entities;

public class AzeriteEmpoweredItemData : BaseUpdateData<Item>
{
	public UpdateFieldArray<int> Selections = new(5, 0, 1);

	public AzeriteEmpoweredItemData() : base(0, TypeId.AzeriteEmpoweredItem, 6) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
	{
		for (int i = 0; i < 5; ++i)
		{
			data.WriteInt32(Selections[i]);
		}
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AzeriteEmpoweredItem owner, Player receiver)
	{
		WriteUpdate(data, ChangesMask, false, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, AzeriteEmpoweredItem owner, Player receiver)
	{
		data.WriteBits(ChangesMask.GetBlocksMask(0), 1);
		if (ChangesMask.GetBlock(0) != 0)
			data.WriteBits(ChangesMask.GetBlock(0), 32);

		data.FlushBits();
		if (ChangesMask[0])
		{
			for (int i = 0; i < 5; ++i)
			{
				if (ChangesMask[1 + i])
				{
					data.WriteInt32(Selections[i]);
				}
			}
		}
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Selections);
		ChangesMask.ResetAll();
	}
}