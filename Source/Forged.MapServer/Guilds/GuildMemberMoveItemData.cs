// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Guilds;

public class GuildMemberMoveItemData : GuildMoveItemData
{
    public GuildMemberMoveItemData(Guild guild, Player player, byte container, byte slotId, ScriptManager scriptManager)
        : base(guild, player, container, slotId, scriptManager) { }

    public override InventoryResult CanStore(Item pItem, bool swap)
    {
        return Player.CanStoreItem(Container, SlotId, ItemPositions, pItem, swap);
    }

    public override bool InitItem()
    {
        Item = Player.GetItemByPos(Container, SlotId);

        if (Item == null)
            return Item != null;

        // Anti-WPE protection. Do not move non-empty bags to bank.
        if (Item.IsNotEmptyBag)
        {
            SendEquipError(InventoryResult.DestroyNonemptyBag, Item);
            Item = null;
        }
        // Bound items cannot be put into bank.
        else if (!Item.CanBeTraded())
        {
            SendEquipError(InventoryResult.CantSwap, Item);
            Item = null;
        }

        return Item != null;
    }

    public override bool IsBank()
    {
        return false;
    }

    public override void LogBankEvent(SQLTransaction trans, GuildMoveItemData pFrom, uint count)
    {
        // Bank . Char
        Guild.LogBankEvent(trans,
                           GuildBankEventLogTypes.WithdrawItem,
                           pFrom.Container,
                           Player.GUID.Counter,
                           pFrom.GetItem().Entry,
                           (ushort)count);
    }

    public override void RemoveItem(SQLTransaction trans, GuildMoveItemData pOther, uint splitedAmount = 0)
    {
        if (splitedAmount != 0)
        {
            Item.SetCount(Item.Count - splitedAmount);
            Item.SetState(ItemUpdateState.Changed, Player);
            Player.SaveInventoryAndGoldToDB(trans);
        }
        else
        {
            Player.MoveItemFromInventory(Container, SlotId, true);
            Item.DeleteFromInventoryDB(trans);
            Item = null;
        }
    }

    public override Item StoreItem(SQLTransaction trans, Item pItem)
    {
        Player.MoveItemToInventory(ItemPositions, pItem, true);
        Player.SaveInventoryAndGoldToDB(trans);

        return pItem;
    }
}