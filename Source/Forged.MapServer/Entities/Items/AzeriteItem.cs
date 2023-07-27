// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Azerite;
using Framework.Constants;
using Framework.Database;
using Game.Common;

namespace Forged.MapServer.Entities.Items;

public class AzeriteItem : Item
{
    public AzeriteItemData AzeriteItemData;
    private readonly AzeriteItemFactory _azeriteItemFactory;

    public AzeriteItem(ClassFactory classFactory, ItemFactory itemFactory, DB2Manager db2Manager, PlayerComputators playerComputators, CharacterDatabase characterDatabase,
                       LootItemStorage lootItemStorage, ItemEnchantmentManager itemEnchantmentManager, AzeriteItemFactory azeriteItemFactory, DB6Storage<ItemEffectRecord> itemEffectRecords,
                       ItemTemplateCache itemTemplateCache)
        : base(classFactory, itemFactory, db2Manager, playerComputators, characterDatabase, lootItemStorage, itemEnchantmentManager, itemEffectRecords, itemTemplateCache)
    {
        _azeriteItemFactory = azeriteItemFactory;
        AzeriteItemData = new AzeriteItemData();

        ObjectTypeMask |= TypeMask.AzeriteItem;
        ObjectTypeId = TypeId.AzeriteItem;

        SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.DebugKnowledgeWeek), -1);
    }

    public void AddUnlockedEssenceMilestone(uint azeriteItemMilestonePowerId)
    {
        AddDynamicUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.UnlockedEssenceMilestones), azeriteItemMilestonePowerId);
    }

    public override void BuildValuesCreate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt8((byte)flags);
        ObjectData.WriteCreate(buffer, flags, this, target);
        ItemData.WriteCreate(buffer, flags, this, target);
        AzeriteItemData.WriteCreate(buffer, flags, this, target);

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

        if (Values.HasChanged(TypeId.AzeriteItem))
            AzeriteItemData.WriteUpdate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteUInt32(Values.GetChangedObjectTypeMask());
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
    {
        UpdateMask valuesMask = new(14);
        valuesMask.Set((int)TypeId.Item);
        valuesMask.Set((int)TypeId.AzeriteItem);

        WorldPacket buffer = new();
        buffer.WriteUInt32(valuesMask.GetBlock(0));

        UpdateMask mask = new(40);
        ItemData.AppendAllowedFieldsMaskForFlag(mask, flags);
        ItemData.WriteUpdate(buffer, mask, true, this, target);

        UpdateMask mask2 = new(9);
        AzeriteItemData.AppendAllowedFieldsMaskForFlag(mask2, flags);
        AzeriteItemData.WriteUpdate(buffer, mask2, true, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public bool CanUseEssences()
    {
        return CliDB.PlayerConditionStorage.TryGetValue(PlayerConst.PlayerConditionIdUnlockedAzeriteEssences, out var condition) && ConditionManager.IsPlayerMeetingCondition(OwnerUnit, condition);
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(AzeriteItemData);
        base.ClearUpdateMask(remove);
    }

    public override bool Create(ulong guidlow, uint itemId, ItemContext context, Player owner)
    {
        if (!base.Create(guidlow, itemId, context, owner))
            return false;

        SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.Level), 1u);
        SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.KnowledgeLevel), GetCurrentKnowledgeLevel());
        UnlockDefaultMilestones();

        return true;
    }

    public void CreateSelectedAzeriteEssences(uint specializationId)
    {
        SelectedAzeriteEssences selectedEssences = new();
        selectedEssences.ModifyValue(selectedEssences.SpecializationID).Value = specializationId;
        selectedEssences.ModifyValue(selectedEssences.Enabled).Value = true;
        AddDynamicUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.SelectedEssences), selectedEssences);
    }

    public override void DeleteFromDB(SQLTransaction trans)
    {
        _azeriteItemFactory.DeleteFromDB(trans, GUID.Counter);
        base.DeleteFromDB(trans);
    }

    public uint GetEffectiveLevel()
    {
        uint level = AzeriteItemData.AuraLevel;

        if (level == 0)
            level = AzeriteItemData.Level;

        return level;
    }

    public uint GetEssenceRank(uint azeriteEssenceId)
    {
        var index = AzeriteItemData.UnlockedEssences.FindIndexIf(essence => essence.AzeriteEssenceID == azeriteEssenceId);

        return index < 0 ? 0 : AzeriteItemData.UnlockedEssences[index].Rank;
    }

    public uint GetLevel()
    {
        return AzeriteItemData.Level;
    }

    public SelectedAzeriteEssences GetSelectedAzeriteEssences()
    {
        foreach (var essences in AzeriteItemData.SelectedEssences)
            if (essences.Enabled)
                return essences;

        return null;
    }

    public void GiveXP(ulong xp)
    {
        var owner = OwnerUnit;
        uint level = AzeriteItemData.Level;

        if (level < PlayerConst.MaxAzeriteItemLevel)
        {
            ulong currentXP = AzeriteItemData.Xp;
            var remainingXP = xp;

            do
            {
                var totalXp = CalcTotalXPToNextLevel(level, AzeriteItemData.KnowledgeLevel);

                if (currentXP + remainingXP >= totalXp)
                {
                    // advance to next level
                    ++level;
                    remainingXP -= totalXp - currentXP;
                    currentXP = 0;
                }
                else
                {
                    currentXP += remainingXP;
                    remainingXP = 0;
                }
            } while (remainingXP > 0 && level < PlayerConst.MaxAzeriteItemLevel);

            SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.Xp), currentXP);

            owner.UpdateCriteria(CriteriaType.EarnArtifactXPForAzeriteItem, xp);

            // changing azerite level changes item level, need to update stats
            if (AzeriteItemData.Level != level)
            {
                if (IsEquipped)
                    owner._ApplyItemBonuses(this, Slot, false);

                SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.Level), level);
                UnlockDefaultMilestones();
                owner.UpdateCriteria(CriteriaType.AzeriteLevelReached, level);

                if (IsEquipped)
                    owner._ApplyItemBonuses(this, Slot, true);
            }

            SetState(ItemUpdateState.Changed, owner);
        }

        PlayerAzeriteItemGains xpGain = new()
        {
            ItemGUID = GUID,
            XP = xp
        };

        owner.SendPacket(xpGain);
    }

    public bool HasUnlockedEssenceMilestone(uint azeriteItemMilestonePowerId)
    {
        return AzeriteItemData.UnlockedEssenceMilestones.FindIndex(azeriteItemMilestonePowerId) != -1;
    }

    public bool HasUnlockedEssenceSlot(byte slot)
    {
        var milestone = DB2Manager.GetAzeriteItemMilestonePower(slot);

        return AzeriteItemData.UnlockedEssenceMilestones.FindIndex(milestone.Id) != -1;
    }

    public void LoadAzeriteItemData(Player owner, AzeriteData azeriteData)
    {
        var needSave = false;

        if (!CliDB.AzeriteLevelInfoStorage.ContainsKey(azeriteData.Level))
        {
            azeriteData.Xp = 0;
            azeriteData.Level = 1;
            azeriteData.KnowledgeLevel = GetCurrentKnowledgeLevel();
            needSave = true;
        }
        else if (azeriteData.Level > PlayerConst.MaxAzeriteItemLevel)
        {
            azeriteData.Xp = 0;
            azeriteData.Level = PlayerConst.MaxAzeriteItemLevel;
            needSave = true;
        }

        if (azeriteData.KnowledgeLevel != GetCurrentKnowledgeLevel())
        {
            // rescale XP to maintain same progress %
            var oldMax = CalcTotalXPToNextLevel(azeriteData.Level, azeriteData.KnowledgeLevel);
            azeriteData.KnowledgeLevel = GetCurrentKnowledgeLevel();
            var newMax = CalcTotalXPToNextLevel(azeriteData.Level, azeriteData.KnowledgeLevel);
            azeriteData.Xp = (ulong)(azeriteData.Xp / (double)oldMax * newMax);
            needSave = true;
        }
        else if (azeriteData.KnowledgeLevel > PlayerConst.MaxAzeriteItemKnowledgeLevel)
        {
            azeriteData.KnowledgeLevel = PlayerConst.MaxAzeriteItemKnowledgeLevel;
            needSave = true;
        }

        SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.Xp), azeriteData.Xp);
        SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.Level), azeriteData.Level);
        SetUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.KnowledgeLevel), azeriteData.KnowledgeLevel);

        foreach (var azeriteItemMilestonePowerId in azeriteData.AzeriteItemMilestonePowers)
            AddUnlockedEssenceMilestone(azeriteItemMilestonePowerId);

        UnlockDefaultMilestones();

        foreach (var unlockedAzeriteEssence in azeriteData.UnlockedAzeriteEssences)
            SetEssenceRank((uint)unlockedAzeriteEssence.AzeriteEssenceID, unlockedAzeriteEssence.Tier);

        foreach (var selectedEssenceData in azeriteData.SelectedAzeriteEssences)
        {
            if (selectedEssenceData.SpecializationId == 0)
                continue;

            var selectedEssences = new SelectedAzeriteEssences();
            selectedEssences.ModifyValue(selectedEssences.SpecializationID).Value = selectedEssenceData.SpecializationId;

            for (var i = 0; i < SharedConst.MaxAzeriteEssenceSlot; ++i)
            {
                // Check if essence was unlocked
                if (GetEssenceRank(selectedEssenceData.AzeriteEssenceId[i]) == 0)
                    continue;

                selectedEssences.ModifyValue(selectedEssences.AzeriteEssenceID, i) = selectedEssenceData.AzeriteEssenceId[i];
            }

            if (owner != null && owner.GetPrimarySpecialization() == selectedEssenceData.SpecializationId)
                selectedEssences.ModifyValue(selectedEssences.Enabled).Value = true;

            AddDynamicUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.SelectedEssences), selectedEssences);
        }

        // add selected essences for current spec
        if (owner != null && GetSelectedAzeriteEssences() == null)
            CreateSelectedAzeriteEssences(owner.GetPrimarySpecialization());

        if (needSave)
        {
            var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_AZERITE_ON_LOAD);
            stmt.AddValue(0, azeriteData.Xp);
            stmt.AddValue(1, azeriteData.KnowledgeLevel);
            stmt.AddValue(2, GUID.Counter);
            CharacterDatabase.Execute(stmt);
        }
    }

    public override void SaveToDB(SQLTransaction trans)
    {
        var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE);
        stmt.AddValue(0, GUID.Counter);
        trans.Append(stmt);

        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
        stmt.AddValue(0, GUID.Counter);
        trans.Append(stmt);

        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
        stmt.AddValue(0, GUID.Counter);
        trans.Append(stmt);

        switch (State)
        {
            case ItemUpdateState.New:
            case ItemUpdateState.Changed:
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE);
                stmt.AddValue(0, GUID.Counter);
                stmt.AddValue(1, AzeriteItemData.Xp);
                stmt.AddValue(2, AzeriteItemData.Level);
                stmt.AddValue(3, AzeriteItemData.KnowledgeLevel);

                var specIndex = 0;

                for (; specIndex < AzeriteItemData.SelectedEssences.Size(); ++specIndex)
                {
                    stmt.AddValue(4 + specIndex * 5, AzeriteItemData.SelectedEssences[specIndex].SpecializationID);

                    for (var j = 0; j < SharedConst.MaxAzeriteEssenceSlot; ++j)
                        stmt.AddValue(5 + specIndex * 5 + j, AzeriteItemData.SelectedEssences[specIndex].AzeriteEssenceID[j]);
                }

                for (; specIndex < 4; ++specIndex)
                {
                    stmt.AddValue(4 + specIndex * 5, 0);

                    for (var j = 0; j < SharedConst.MaxAzeriteEssenceSlot; ++j)
                        stmt.AddValue(5 + specIndex * 5 + j, 0);
                }

                trans.Append(stmt);

                foreach (var azeriteItemMilestonePowerId in AzeriteItemData.UnlockedEssenceMilestones)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
                    stmt.AddValue(0, GUID.Counter);
                    stmt.AddValue(1, azeriteItemMilestonePowerId);
                    trans.Append(stmt);
                }

                foreach (var azeriteEssence in AzeriteItemData.UnlockedEssences)
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
                    stmt.AddValue(0, GUID.Counter);
                    stmt.AddValue(1, azeriteEssence.AzeriteEssenceID);
                    stmt.AddValue(2, azeriteEssence.Rank);
                    trans.Append(stmt);
                }

                break;
        }

        base.SaveToDB(trans);
    }

    public void SetEssenceRank(uint azeriteEssenceId, uint rank)
    {
        var index = AzeriteItemData.UnlockedEssences.FindIndexIf(essence => { return essence.AzeriteEssenceID == azeriteEssenceId; });

        if (rank == 0 && index >= 0)
        {
            RemoveDynamicUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.UnlockedEssences), index);

            return;
        }

        if (DB2Manager.GetAzeriteEssencePower(azeriteEssenceId, rank) == null)
            return;

        if (index < 0)
        {
            UnlockedAzeriteEssence unlockedEssence = new()
            {
                AzeriteEssenceID = azeriteEssenceId,
                Rank = rank
            };

            AddDynamicUpdateFieldValue(Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.UnlockedEssences), unlockedEssence);
        }
        else
        {
            UnlockedAzeriteEssence actorField = Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.UnlockedEssences, index);
            SetUpdateFieldValue(ref actorField.Rank, rank);
        }
    }

    public void SetSelectedAzeriteEssence(int slot, uint azeriteEssenceId)
    {
        //ASSERT(slot < MAX_AZERITE_ESSENCE_SLOT);
        var index = AzeriteItemData.SelectedEssences.FindIndexIf(essences => essences.Enabled);
        //ASSERT(index >= 0);
        SelectedAzeriteEssences selectedEssences = Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.SelectedEssences, index);
        SetUpdateFieldValue(ref selectedEssences.ModifyValue(selectedEssences.AzeriteEssenceID, slot), azeriteEssenceId);
    }

    public void SetSelectedAzeriteEssences(uint specializationId)
    {
        var index = AzeriteItemData.SelectedEssences.FindIndexIf(essences => essences.Enabled);

        if (index >= 0)
        {
            SelectedAzeriteEssences selectedEssences = Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.SelectedEssences, index);
            SetUpdateFieldValue(selectedEssences.ModifyValue(selectedEssences.Enabled), false);
        }

        index = AzeriteItemData.SelectedEssences.FindIndexIf(essences => essences.SpecializationID == specializationId);

        if (index >= 0)
        {
            SelectedAzeriteEssences selectedEssences = Values.ModifyValue(AzeriteItemData).ModifyValue(AzeriteItemData.SelectedEssences, index);
            SetUpdateFieldValue(selectedEssences.ModifyValue(selectedEssences.Enabled), true);
        }
        else
            CreateSelectedAzeriteEssences(specializationId);
    }

    private ulong CalcTotalXPToNextLevel(uint level, uint knowledgeLevel)
    {
        var levelInfo = CliDB.AzeriteLevelInfoStorage.LookupByKey(level);
        var totalXp = levelInfo.BaseExperienceToNextLevel * (ulong)CliDB.AzeriteKnowledgeMultiplierStorage.LookupByKey(knowledgeLevel).Multiplier;

        return Math.Max(totalXp, levelInfo.MinimumExperienceToNextLevel);
    }

    private uint GetCurrentKnowledgeLevel()
    {
        // count weeks from 14.01.2020
        var now = GameTime.DateAndTime;
        DateTime beginDate = new(2020, 1, 14);
        uint knowledge = 0;

        while (beginDate < now && knowledge < PlayerConst.MaxAzeriteItemKnowledgeLevel)
        {
            ++knowledge;
            beginDate = beginDate.AddDays(7);
        }

        return knowledge;
    }

    private void UnlockDefaultMilestones()
    {
        var hasPreviousMilestone = true;

        foreach (var milestone in DB2Manager.GetAzeriteItemMilestonePowers())
        {
            if (!hasPreviousMilestone)
                break;

            if (milestone.RequiredLevel > GetLevel())
                break;

            if (HasUnlockedEssenceMilestone(milestone.Id))
                continue;

            if (milestone.AutoUnlock != 0)
            {
                AddUnlockedEssenceMilestone(milestone.Id);
                hasPreviousMilestone = true;
            }
            else
                hasPreviousMilestone = false;
        }
    }
}