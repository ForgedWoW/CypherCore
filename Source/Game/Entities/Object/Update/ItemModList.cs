using Game.Networking;

namespace Game.Entities;

public class ItemModList : BaseUpdateData<Item>
{
	public DynamicUpdateField<ItemMod> Values = new(0, 0);

	public ItemModList() : base(1) { }

	public void WriteCreate(WorldPacket data, Item owner, Player receiver)
	{
		data.WriteBits(Values.Size(), 6);
		for (int i = 0; i < Values.Size(); ++i)
		{
			Values[i].WriteCreate(data, owner, receiver);
		}
		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
	{
		UpdateMask changesMask = ChangesMask;
		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlock(0), 1);

		if (changesMask[0])
		{
			if (changesMask[0])
			{
				if (!ignoreChangesMask)
					Values.WriteUpdateMask(data, 6);
				else
					WriteCompleteDynamicFieldUpdateMask(Values.Size(), data, 6);
			}
		}
		data.FlushBits();
		if (changesMask[0])
		{
			if (changesMask[0])
			{
				for (int i = 0; i < Values.Size(); ++i)
				{
					if (Values.HasChanged(i) || ignoreChangesMask)
					{
						Values[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);
					}
				}
			}
		}
		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Values);
		ChangesMask.ResetAll();
	}
}