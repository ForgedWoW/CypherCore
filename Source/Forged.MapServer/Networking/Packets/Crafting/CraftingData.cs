﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Item;

namespace Forged.MapServer.Networking.Packets.Crafting;

internal class CraftingData
{
    public bool BonusCraft;
    public int CraftingDataID;
    public int CraftingQualityID;
    public int CritBonusSkill;
    public int EnchantID;
    public float field_1C;
    public ulong field_20;
    public bool field_29;
    public bool field_2A;
    public bool IsCrit;
    public ObjectGuid ItemGUID;
    public int Multicraft;
    public ItemInstance NewItem = new();
    public ItemInstance OldItem = new();
    public uint OperationID;
    public float QualityProgress;
    public int Quantity;
    public List<SpellReducedReagent> ResourcesReturned = new();
    public int Skill;
    public int SkillFromReagents;
    public int SkillLineAbilityID;

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