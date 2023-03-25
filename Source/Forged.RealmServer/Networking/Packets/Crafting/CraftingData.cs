// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class CraftingData
{
	public int CraftingQualityID;
	public float QualityProgress;
	public int SkillLineAbilityID;
	public int CraftingDataID;
	public int Multicraft;
	public int SkillFromReagents;
	public int Skill;
	public int CritBonusSkill;
	public float field_1C;
	public ulong field_20;
	public bool IsCrit;
	public bool field_29;
	public bool field_2A;
	public bool BonusCraft;
	public List<SpellReducedReagent> ResourcesReturned = new();
	public uint OperationID;
	public ObjectGuid ItemGUID;
	public int Quantity;
	public ItemInstance OldItem = new();
	public ItemInstance NewItem = new();
	public int EnchantID;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(CraftingQualityID);
		data.WriteFloat(QualityProgress);
		data.WriteInt32(SkillLineAbilityID);
		data.WriteInt32(CraftingDataID);
		data.WriteInt32(Multicraft);
		data.WriteInt32(SkillFromReagents);
		data.WriteInt32(Skill);
		data.WriteInt32(CritBonusSkill);
		data.WriteFloat(field_1C);
		data.WriteUInt64(field_20);
		data.WriteInt32(ResourcesReturned.Count);
		data.WriteUInt32(OperationID);
		data.WritePackedGuid(ItemGUID);
		data.WriteInt32(Quantity);
		data.WriteInt32(EnchantID);

		foreach (var spellReducedReagent in ResourcesReturned)
			spellReducedReagent.Write(data);

		data.WriteBit(IsCrit);
		data.WriteBit(field_29);
		data.WriteBit(field_2A);
		data.WriteBit(BonusCraft);
		data.FlushBits();

		OldItem.Write(data);
		NewItem.Write(data);
	}
}