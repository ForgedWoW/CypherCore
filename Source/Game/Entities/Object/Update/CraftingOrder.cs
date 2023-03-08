using Game.Networking;
using Game.Networking.Packets;

namespace Game.Entities;

public class CraftingOrder : BaseUpdateData<Player>
{
	public DynamicUpdateField<ItemEnchantData> Enchantments = new(-1, 0);
	public DynamicUpdateField<ItemGemData> Gems = new(-1, 1);
	public UpdateField<CraftingOrderData> Data = new(-1, 2);
	public OptionalUpdateField<ItemInstance> RecraftItemInfo = new(-1, 3);

	public CraftingOrder() : base(4) { }

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		Data.GetValue().WriteCreate(data, owner, receiver);
		data.WriteBits(RecraftItemInfo.HasValue(), 1);
		data.WriteBits(Enchantments.Size(), 4);
		data.WriteBits(Gems.Size(), 2);
		if (RecraftItemInfo.HasValue())
		{
			RecraftItemInfo.GetValue().Write(data);
		}
		for (int i = 0; i < Enchantments.Size(); ++i)
		{
			Enchantments[i].Write(data);
		}
		for (int i = 0; i < Gems.Size(); ++i)
		{
			Gems[i].Write(data);
		}
		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		UpdateMask changesMask = ChangesMask;
		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlock(0), 4);

		if (changesMask[0])
		{
			if (!ignoreChangesMask)
				Enchantments.WriteUpdateMask(data, 4);
			else
				WriteCompleteDynamicFieldUpdateMask(Enchantments.Size(), data, 4);
		}
		if (changesMask[1])
		{
			if (!ignoreChangesMask)
				Gems.WriteUpdateMask(data, 2);
			else
				WriteCompleteDynamicFieldUpdateMask(Gems.Size(), data, 2);
		}
		data.FlushBits();
		if (changesMask[0])
		{
			for (int i = 0; i < Enchantments.Size(); ++i)
			{
				if (Enchantments.HasChanged(i) || ignoreChangesMask)
				{
					Enchantments[i].Write(data);
				}
			}
		}
		if (changesMask[1])
		{
			for (int i = 0; i < Gems.Size(); ++i)
			{
				if (Gems.HasChanged(i) || ignoreChangesMask)
				{
					Gems[i].Write(data);
				}
			}
		}

		if (changesMask[2])
		{
			Data.GetValue().WriteUpdate(data, ignoreChangesMask, owner, receiver);
		}
		data.WriteBits(RecraftItemInfo.HasValue(), 1);
		if (changesMask[3])
		{
			if (RecraftItemInfo.HasValue())
			{
				RecraftItemInfo.GetValue().Write(data);
			}
		}
		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Enchantments);
		ClearChangesMask(Gems);
		ClearChangesMask(Data);
		ClearChangesMask(RecraftItemInfo);
		ChangesMask.ResetAll();
	}
}