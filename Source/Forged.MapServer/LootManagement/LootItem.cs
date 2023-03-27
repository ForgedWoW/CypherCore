using System;
using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;
using Forged.MapServer.LootManagement;

namespace Forged.MapServer.LootManagement;

public class LootItem
{
    private readonly GameObjectManager _objectManager;
    private readonly ConditionManager _conditionManager;
    public uint Itemid;
    public uint LootListId;
    public uint RandomBonusListId;
    public List<uint> BonusListIDs = new();
    public ItemContext Context;
    public List<Condition> Conditions = new(); // additional loot condition
    public List<ObjectGuid> AllowedGuiDs = new();
    public ObjectGuid RollWinnerGuid; // Stores the guid of person who won loot, if his bags are full only he can see the item in loot list!
    public byte Count;
    public bool IsLooted;
    public bool IsBlocked;
    public bool Freeforall; // free for all
    public bool IsUnderthreshold;
    public bool IsCounted;
    public bool NeedsQuest; // quest drop
    public bool FollowLootRules;

    public LootItem(GameObjectManager objectManager, ConditionManager conditionManager)
    {
        _objectManager = objectManager;
        _conditionManager = conditionManager;
    }

    public LootItem(GameObjectManager objectManager, ConditionManager conditionManager, LootStoreItem li) : this(objectManager, conditionManager)
    {
        Itemid = li.Itemid;
        Conditions = li.Conditions;

        var proto = objectManager.GetItemTemplate(Itemid);
        Freeforall = proto != null && proto.HasFlag(ItemFlags.MultiDrop);
        FollowLootRules = !li.NeedsQuest || (proto != null && proto.FlagsCu.HasAnyFlag(ItemFlagsCustom.FollowLootRules));

        NeedsQuest = li.NeedsQuest;

        RandomBonusListId = ItemEnchantmentManager.GenerateItemRandomBonusListId(Itemid);
    }

    /// <summary>
    ///  Basic checks for player/item compatibility - if false no chance to see the item in the loot - used only for loot generation
    /// </summary>
    /// <param name="player"> </param>
    /// <param name="loot"> </param>
    /// <returns> </returns>
    public bool AllowedForPlayer(Player player, Loot loot)
    {
        return AllowedForPlayer(player, loot, Itemid, NeedsQuest, FollowLootRules, false, Conditions, _objectManager, _conditionManager);
    }

    public static bool AllowedForPlayer(Player player, Loot loot, uint itemid, bool needsQuest, bool followLootRules, bool strictUsabilityCheck, List<Condition> conditions, GameObjectManager objectManager, ConditionManager conditionManager)
    {
        // DB conditions check
        if (!conditionManager.IsObjectMeetToConditions(player, conditions))
            return false;

        var pProto = objectManager.GetItemTemplate(itemid);

        if (pProto == null)
            return false;

        // not show loot for not own team
        if (pProto.HasFlag(ItemFlags2.FactionHorde) && player.Team != TeamFaction.Horde)
            return false;

        if (pProto.HasFlag(ItemFlags2.FactionAlliance) && player.Team != TeamFaction.Alliance)
            return false;

        // Master looter can see all items even if the character can't loot them
        if (loot != null && loot.GetLootMethod() == LootMethod.MasterLoot && followLootRules && loot.GetLootMasterGuid() == player.GUID)
            return true;

        // Don't allow loot for players without profession or those who already know the recipe
        if (pProto.HasFlag(ItemFlags.HideUnusableRecipe))
        {
            if (!player.HasSkill((SkillType)pProto.RequiredSkill))
                return false;

            foreach (var itemEffect in pProto.Effects)
            {
                if (itemEffect.TriggerType != ItemSpelltriggerType.OnLearn)
                    continue;

                if (player.HasSpell((uint)itemEffect.SpellID))
                    return false;
            }
        }

        // check quest requirements
        if (!pProto.FlagsCu.HasAnyFlag(ItemFlagsCustom.IgnoreQuestStatus) && ((needsQuest || (pProto.StartQuest != 0 && player.GetQuestStatus(pProto.StartQuest) != QuestStatus.None)) && !player.HasQuestForItem(itemid)))
            return false;

        if (strictUsabilityCheck)
        {
            if ((pProto.IsWeapon || pProto.IsArmor) && !pProto.IsUsableByLootSpecialization(player, true))
                return false;

            if (player.CanRollNeedForItem(pProto, null, false) != InventoryResult.Ok)
                return false;
        }

        return true;
    }

    public void AddAllowedLooter(Player player)
    {
        AllowedGuiDs.Add(player.GUID);
    }

    public bool HasAllowedLooter(ObjectGuid looter)
    {
        return AllowedGuiDs.Contains(looter);
    }

    public LootSlotType? GetUiTypeForPlayer(Player player, Loot loot)
    {
        if (IsLooted)
            return null;

        if (!AllowedGuiDs.Contains(player.GUID))
            return null;

        if (Freeforall)
        {
            var ffaItems = loot.GetPlayerFFAItems().LookupByKey(player.GUID);

            if (ffaItems != null)
            {
                var ffaItemItr = ffaItems.Find(ffaItem => ffaItem.LootListId == LootListId);

                if (ffaItemItr is { IsLooted: false })
                    return loot.GetLootMethod() == LootMethod.FreeForAll ? LootSlotType.Owner : LootSlotType.AllowLoot;
            }

            return null;
        }

        if (NeedsQuest && !FollowLootRules)
            return loot.GetLootMethod() == LootMethod.FreeForAll ? LootSlotType.Owner : LootSlotType.AllowLoot;

        switch (loot.GetLootMethod())
        {
            case LootMethod.FreeForAll:
                return LootSlotType.Owner;
            case LootMethod.RoundRobin:
                if (!loot.RoundRobinPlayer.IsEmpty && loot.RoundRobinPlayer != player.GUID)
                    return null;

                return LootSlotType.AllowLoot;
            case LootMethod.MasterLoot:
                if (IsUnderthreshold)
                {
                    if (!loot.RoundRobinPlayer.IsEmpty && loot.RoundRobinPlayer != player.GUID)
                        return null;

                    return LootSlotType.AllowLoot;
                }

                return loot.GetLootMasterGuid() == player.GUID ? LootSlotType.Master : LootSlotType.Locked;
            case LootMethod.GroupLoot:
            case LootMethod.NeedBeforeGreed:
                if (IsUnderthreshold)
                    if (!loot.RoundRobinPlayer.IsEmpty && loot.RoundRobinPlayer != player.GUID)
                        return null;

                if (IsBlocked)
                    return LootSlotType.RollOngoing;

                if (RollWinnerGuid.IsEmpty) // all passed
                    return LootSlotType.AllowLoot;

                if (RollWinnerGuid == player.GUID)
                    return LootSlotType.Owner;

                return null;
            case LootMethod.PersonalLoot:
                return LootSlotType.Owner;
            default:
                break;
        }

        return null;
    }

    public List<ObjectGuid> GetAllowedLooters()
    {
        return AllowedGuiDs;
    }
}
