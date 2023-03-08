// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Networking;

namespace Game.Entities;

public class AzeriteEmpoweredItem : Item
{
	readonly AzeriteEmpoweredItemData _azeriteEmpoweredItemData;
	List<AzeritePowerSetMemberRecord> _azeritePowers;
	int _maxTier;

	public AzeriteEmpoweredItem()
	{
		ObjectTypeMask |= TypeMask.AzeriteEmpoweredItem;
		ObjectTypeId = TypeId.AzeriteEmpoweredItem;

		_azeriteEmpoweredItemData = new AzeriteEmpoweredItemData();
	}

	public override bool Create(ulong guidlow, uint itemId, ItemContext context, Player owner)
	{
		if (!base.Create(guidlow, itemId, context, owner))
			return false;

		InitAzeritePowerData();

		return true;
	}

	public override void SaveToDB(SQLTransaction trans)
	{
		var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
		stmt.AddValue(0, GetGUID().GetCounter());
		trans.Append(stmt);

		switch (GetState())
		{
			case ItemUpdateState.New:
			case ItemUpdateState.Changed:
				stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_EMPOWERED);
				stmt.AddValue(0, GetGUID().GetCounter());

				for (var i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
					stmt.AddValue(1 + i, _azeriteEmpoweredItemData.Selections[i]);

				trans.Append(stmt);

				break;
		}

		base.SaveToDB(trans);
	}

	public void LoadAzeriteEmpoweredItemData(Player owner, AzeriteEmpoweredData azeriteEmpoweredItem)
	{
		InitAzeritePowerData();
		var needSave = false;

		if (_azeritePowers != null)
			for (var i = SharedConst.MaxAzeriteEmpoweredTier; --i >= 0;)
			{
				var selection = azeriteEmpoweredItem.SelectedAzeritePowers[i];

				if (GetTierForAzeritePower(owner.GetClass(), selection) != i)
				{
					needSave = true;

					break;
				}

				SetSelectedAzeritePower(i, selection);
			}
		else
			needSave = true;

		if (needSave)
		{
			var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_AZERITE_EMPOWERED);

			for (var i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
				stmt.AddValue(i, _azeriteEmpoweredItemData.Selections[i]);

			stmt.AddValue(5, GetGUID().GetCounter());
			DB.Characters.Execute(stmt);
		}
	}

	public static new void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
	{
		var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
		stmt.AddValue(0, itemGuid);
		DB.Characters.ExecuteOrAppend(trans, stmt);
	}

	public override void DeleteFromDB(SQLTransaction trans)
	{
		DeleteFromDB(trans, GetGUID().GetCounter());
		base.DeleteFromDB(trans);
	}

	public uint GetRequiredAzeriteLevelForTier(uint tier)
	{
		return Global.DB2Mgr.GetRequiredAzeriteLevelForAzeritePowerTier(BonusData.AzeriteTierUnlockSetId, GetContext(), tier);
	}

	public int GetTierForAzeritePower(Class playerClass, int azeritePowerId)
	{
		var azeritePowerItr = _azeritePowers.Find(power => { return power.AzeritePowerID == azeritePowerId && power.Class == (int)playerClass; });

		if (azeritePowerItr != null)
			return azeritePowerItr.Tier;

		return SharedConst.MaxAzeriteEmpoweredTier;
	}

	public void SetSelectedAzeritePower(int tier, int azeritePowerId)
	{
		SetUpdateFieldValue(ref Values.ModifyValue(_azeriteEmpoweredItemData).ModifyValue(_azeriteEmpoweredItemData.Selections, tier), azeritePowerId);

		// Not added to UF::ItemData::BonusListIDs, client fakes it on its own too
		BonusData.AddBonusList(CliDB.AzeritePowerStorage.LookupByKey(azeritePowerId).ItemBonusListID);
	}

	public long GetRespecCost()
	{
		var owner = GetOwner();

		if (owner != null)
			return (long)(MoneyConstants.Gold * Global.DB2Mgr.GetCurveValueAt((uint)Curves.AzeriteEmpoweredItemRespecCost, (float)owner.GetNumRespecs()));

		return (long)PlayerConst.MaxMoneyAmount + 1;
	}

	public override void BuildValuesCreate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		buffer.WriteUInt8((byte)flags);
		ObjectData.WriteCreate(buffer, flags, this, target);
		ItemData.WriteCreate(buffer, flags, this, target);
		_azeriteEmpoweredItemData.WriteCreate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteBytes(buffer);
	}

	public override void BuildValuesUpdate(WorldPacket data, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		WorldPacket buffer = new();

		if (Values.HasChanged(TypeId.Object))
			ObjectData.WriteUpdate(buffer, flags, this, target);

		if (Values.HasChanged(TypeId.Item))
			ItemData.WriteUpdate(buffer, flags, this, target);

		if (Values.HasChanged(TypeId.AzeriteEmpoweredItem))
			_azeriteEmpoweredItemData.WriteUpdate(buffer, flags, this, target);

		data.WriteUInt32(buffer.GetSize());
		data.WriteUInt32(Values.GetChangedObjectTypeMask());
		data.WriteBytes(buffer);
	}

	public override void ClearUpdateMask(bool remove)
	{
		Values.ClearChangesMask(_azeriteEmpoweredItemData);
		base.ClearUpdateMask(remove);
	}

	public int GetMaxAzeritePowerTier()
	{
		return _maxTier;
	}

	public uint GetSelectedAzeritePower(int tier)
	{
		return (uint)_azeriteEmpoweredItemData.Selections[tier];
	}

	void ClearSelectedAzeritePowers()
	{
		for (var i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
			SetUpdateFieldValue(ref Values.ModifyValue(_azeriteEmpoweredItemData).ModifyValue(_azeriteEmpoweredItemData.Selections, i), 0);

		BonusData = new BonusData(GetTemplate());

		foreach (var bonusListID in GetBonusListIDs())
			BonusData.AddBonusList(bonusListID);
	}

	void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, UpdateMask requestedAzeriteEmpoweredItemMask, Player target)
	{
		var flags = GetUpdateFieldFlagsFor(target);
		UpdateMask valuesMask = new((int)TypeId.Max);

		if (requestedObjectMask.IsAnySet())
			valuesMask.Set((int)TypeId.Object);

		ItemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);

		if (requestedItemMask.IsAnySet())
			valuesMask.Set((int)TypeId.Item);

		if (requestedAzeriteEmpoweredItemMask.IsAnySet())
			valuesMask.Set((int)TypeId.AzeriteEmpoweredItem);

		WorldPacket buffer = new();
		buffer.WriteUInt32(valuesMask.GetBlock(0));

		if (valuesMask[(int)TypeId.Object])
			ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

		if (valuesMask[(int)TypeId.Item])
			ItemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

		if (valuesMask[(int)TypeId.AzeriteEmpoweredItem])
			_azeriteEmpoweredItemData.WriteUpdate(buffer, requestedAzeriteEmpoweredItemMask, true, this, target);

		WorldPacket buffer1 = new();
		buffer1.WriteUInt8((byte)UpdateType.Values);
		buffer1.WritePackedGuid(GetGUID());
		buffer1.WriteUInt32(buffer.GetSize());
		buffer1.WriteBytes(buffer.GetData());

		data.AddUpdateBlock(buffer1);
	}

	void InitAzeritePowerData()
	{
		_azeritePowers = Global.DB2Mgr.GetAzeritePowers(GetEntry());

		if (_azeritePowers != null)
			_maxTier = _azeritePowers.Aggregate((a1, a2) => a1.Tier < a2.Tier ? a2 : a1).Tier;
	}

	class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
	{
		readonly AzeriteEmpoweredItem Owner;
		readonly ObjectFieldData ObjectMask = new();
		readonly ItemData ItemMask = new();
		readonly AzeriteEmpoweredItemData AzeriteEmpoweredItemMask = new();

		public ValuesUpdateForPlayerWithMaskSender(AzeriteEmpoweredItem owner)
		{
			Owner = owner;
		}

		public void Invoke(Player player)
		{
			UpdateData udata = new(Owner.Location.MapId);

			Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), ItemMask.GetUpdateMask(), AzeriteEmpoweredItemMask.GetUpdateMask(), player);

			udata.BuildPacket(out var packet);
			player.SendPacket(packet);
		}
	}
}