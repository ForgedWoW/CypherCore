// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IGuild;
using Framework.Constants;
using Framework.Database;

namespace Forged.MapServer.Guilds;

public abstract class GuildMoveItemData
{
    private readonly ScriptManager _scriptManager;

    protected GuildMoveItemData(Guild guild, Player player, byte container, byte slotId, ScriptManager scriptManager)
    {
        _scriptManager = scriptManager;
        Guild = guild;
        Player = player;
        Container = container;
        SlotId = slotId;
        Item = null;
        ClonedItem = null;
    }

    public Item ClonedItem { get; set; }
    public byte Container { get; set; }
    public Guild Guild { get; set; }
    public Item Item { get; set; }
    public List<ItemPosCount> ItemPositions { get; set; } = new();
    public Player Player { get; set; }
    public byte SlotId { get; set; }
    public InventoryResult CanStore(Item pItem, bool swap, bool sendError)
    {
        ItemPositions.Clear();
        var msg = CanStore(pItem, swap);

        if (sendError && msg != InventoryResult.Ok)
            SendEquipError(msg, pItem);

        return msg;
    }

    public abstract InventoryResult CanStore(Item pItem, bool swap);

    public virtual bool CheckItem(ref uint splitedAmount)
    {
        if (splitedAmount > Item.Count)
            return false;

        if (splitedAmount == Item.Count)
            splitedAmount = 0;

        return true;
    }

    public bool CloneItem(uint count)
    {
        ClonedItem = Item.CloneItem(count);

        if (ClonedItem != null)
            return true;

        SendEquipError(InventoryResult.ItemNotFound, Item);

        return false;
    }

    public void CopySlots(List<byte> ids)
    {
        ids.AddRange(ItemPositions.Select(item => (byte)item.Pos));
    }


    public Item GetItem(bool isCloned = false)
    {
        return isCloned ? ClonedItem : Item;
    }

    // Checks splited amount against item. Splited amount cannot be more that number of items in stack.
    // Defines if player has rights to save item in container
    public virtual bool HasStoreRights(GuildMoveItemData pOther)
    {
        return true;
    }

    // Defines if player has rights to withdraw item from container
    public virtual bool HasWithdrawRights(GuildMoveItemData pOther)
    {
        return true;
    }

    // Initializes item. Returns true, if item exists, false otherwise.
    public abstract bool InitItem();

    public abstract bool IsBank();

    public virtual void LogAction(GuildMoveItemData pFrom)
    {
        _scriptManager.ForEach<IGuildOnItemMove>(p => p.OnItemMove(Guild,
                                                                   Player,
                                                                   pFrom.GetItem(),
                                                                   pFrom.IsBank(),
                                                                   pFrom.Container,
                                                                   pFrom.SlotId,
                                                                   IsBank(),
                                                                   Container,
                                                                   SlotId));
    }

    // Log bank event
    public abstract void LogBankEvent(SQLTransaction trans, GuildMoveItemData pFrom, uint count);

    // Remove item from container (if splited update items fields)
    public abstract void RemoveItem(SQLTransaction trans, GuildMoveItemData pOther, uint splitedAmount = 0);

    public void SendEquipError(InventoryResult result, Item item)
    {
        Player.SendEquipError(result, item);
    }

    // Saves item to container
    public abstract Item StoreItem(SQLTransaction trans, Item pItem);
}