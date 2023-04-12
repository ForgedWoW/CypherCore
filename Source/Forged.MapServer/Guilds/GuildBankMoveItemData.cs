// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Linq;
using Forged.MapServer.Chat.Commands;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Guilds;

public class GuildBankMoveItemData : GuildMoveItemData
{
    public GuildBankMoveItemData(Guild guild, Player player, byte container, byte slotId, ScriptManager scriptManager)
        : base(guild, player, container, slotId, scriptManager) { }

    public override InventoryResult CanStore(Item pItem, bool swap)
    {
        Log.Logger.Debug("GUILD STORAGE: CanStore() tab = {0}, slot = {1}, item = {2}, count = {3}",
                         Container,
                         SlotId,
                         pItem.Entry,
                         pItem.Count);

        var count = pItem.Count;

        // Soulbound items cannot be moved
        if (pItem.IsSoulBound)
            return InventoryResult.DropBoundItem;

        // Make sure destination bank tab exists
        if (Container >= Guild.GetPurchasedTabsSize())
            return InventoryResult.WrongBagType;

        // Slot explicitely specified. Check it.
        if (SlotId != ItemConst.NullSlot)
        {
            var pItemDest = Guild.GetItem(Container, SlotId);

            // Ignore swapped item (this slot will be empty after move)
            if ((pItemDest == pItem) || swap)
                pItemDest = null;

            if (!ReserveSpace(SlotId, pItem, pItemDest, ref count))
                return InventoryResult.CantStack;

            if (count == 0)
                return InventoryResult.Ok;
        }

        // Slot was not specified or it has not enough space for all the items in stack
        // Search for stacks to merge with
        if (pItem.MaxStackCount > 1)
        {
            CanStoreItemInTab(pItem, SlotId, true, ref count);

            if (count == 0)
                return InventoryResult.Ok;
        }

        // Search free slot for item
        CanStoreItemInTab(pItem, SlotId, false, ref count);

        return count == 0 ? InventoryResult.Ok : InventoryResult.BankFull;
    }

    public override bool HasStoreRights(GuildMoveItemData pOther)
    {
        // Do not check rights if item is being swapped within the same bank tab
        if (pOther.IsBank() && pOther.Container == Container)
            return true;

        return Guild.MemberHasTabRights(Player.GUID, Container, GuildBankRights.DepositItem);
    }

    public override bool HasWithdrawRights(GuildMoveItemData pOther)
    {
        // Do not check rights if item is being swapped within the same bank tab
        if (pOther.IsBank() && pOther.Container == Container)
            return true;

        var slots = 0;
        var member = Guild.GetMember(Player.GUID);

        if (member != null)
            slots = Guild.GetMemberRemainingSlots(member, Container);

        return slots != 0;
    }

    public override bool InitItem()
    {
        Item = Guild.GetItem(Container, SlotId);

        return (Item != null);
    }

    public override bool IsBank()
    {
        return true;
    }

    public override void LogAction(GuildMoveItemData pFrom)
    {
        base.LogAction(pFrom);

        if (!pFrom.IsBank() && Player.Session.HasPermission(RBACPermissions.LogGmTrade)) // @todo Move this to scripts
            Log.Logger.ForContext<GMCommands>()
               .Information("GM {0} ({1}) (Account: {2}) deposit item: {3} (Entry: {4} Count: {5}) to guild bank named: {6} (Guild ID: {7})",
                            Player.GetName(),
                            Player.GUID.ToString(),
                            Player.Session.AccountId,
                            pFrom.GetItem().Template.GetName(),
                            pFrom.GetItem().Entry,
                            pFrom.GetItem().Count,
                            Guild.GetName(),
                            Guild.GetId());
    }

    public override void LogBankEvent(SQLTransaction trans, GuildMoveItemData pFrom, uint count)
    {
        if (pFrom.IsBank())
            // Bank . Bank
            Guild.LogBankEvent(trans,
                               GuildBankEventLogTypes.MoveItem,
                               pFrom.Container,
                               Player.GUID.Counter,
                               pFrom.GetItem().Entry,
                               (ushort)count,
                               Container);
        else
            // Char . Bank
            Guild.LogBankEvent(trans,
                               GuildBankEventLogTypes.DepositItem,
                               Container,
                               Player.GUID.Counter,
                               pFrom.GetItem().Entry,
                               (ushort)count);
    }

    public override void RemoveItem(SQLTransaction trans, GuildMoveItemData pOther, uint splitedAmount = 0)
    {
        if (splitedAmount != 0)
        {
            Item.SetCount(Item.Count - splitedAmount);
            Item.FSetState(ItemUpdateState.Changed);
            Item.SaveToDB(trans);
        }
        else
        {
            Guild.RemoveItem(trans, Container, SlotId);
            Item = null;
        }

        // Decrease amount of player's remaining items (if item is moved to different tab or to player)
        if (!pOther.IsBank() || pOther.Container != Container)
            Guild.UpdateMemberWithdrawSlots(trans, Player.GUID, Container);
    }

    public override Item StoreItem(SQLTransaction trans, Item pItem)
    {
        if (pItem == null)
            return null;

        var pTab = Guild.GetBankTab(Container);

        if (pTab == null)
            return null;

        var pLastItem = pItem;

        foreach (var pos in ItemPositions)
        {
            Log.Logger.Debug("GUILD STORAGE: StoreItem tab = {0}, slot = {1}, item = {2}, count = {3}",
                             Container,
                             SlotId,
                             pItem.Entry,
                             pItem.Count);

            pLastItem = StoreItem(trans, pTab, pItem, pos, pos.Equals(ItemPositions.Last()));
        }

        return pLastItem;
    }

    private void CanStoreItemInTab(Item pItem, byte skipSlotId, bool merge, ref uint count)
    {
        for (byte slotId = 0; (slotId < GuildConst.MaxBankSlots) && (count > 0); ++slotId)
        {
            // Skip slot already processed in CanStore (when destination slot was specified)
            if (slotId == skipSlotId)
                continue;

            var pItemDest = Guild.GetItem(Container, slotId);

            if (pItemDest == pItem)
                pItemDest = null;

            // If merge skip empty, if not merge skip non-empty
            if ((pItemDest != null) != merge)
                continue;

            ReserveSpace(slotId, pItem, pItemDest, ref count);
        }
    }

    private bool ReserveSpace(byte slotId, Item pItem, Item pItemDest, ref uint count)
    {
        var requiredSpace = pItem.MaxStackCount;

        if (pItemDest != null)
        {
            // Make sure source and destination items match and destination item has space for more stacks.
            if (pItemDest.Entry != pItem.Entry || pItemDest.Count >= pItem.MaxStackCount)
                return false;

            requiredSpace -= pItemDest.Count;
        }

        // Let's not be greedy, reserve only required space
        requiredSpace = Math.Min(requiredSpace, count);

        // Reserve space
        ItemPosCount pos = new(slotId, requiredSpace);

        if (pos.IsContainedIn(ItemPositions))
            return true;

        ItemPositions.Add(pos);
        count -= requiredSpace;

        return true;
    }

    private Item StoreItem(SQLTransaction trans, GuildBankTab pTab, Item pItem, ItemPosCount pos, bool clone)
    {
        var slotId = (byte)pos.Pos;
        var count = pos.Count;
        var pItemDest = pTab.GetItem(slotId);

        if (pItemDest != null)
        {
            pItemDest.SetCount(pItemDest.Count + count);
            pItemDest.FSetState(ItemUpdateState.Changed);
            pItemDest.SaveToDB(trans);

            if (!clone)
            {
                pItem.RemoveFromWorld();
                pItem.DeleteFromDB(trans);
            }

            return pItemDest;
        }

        if (clone)
            pItem = pItem.CloneItem(count);
        else
            pItem.SetCount(count);

        if (pItem != null && pTab.SetItem(trans, slotId, pItem))
            return pItem;

        return null;
    }
}