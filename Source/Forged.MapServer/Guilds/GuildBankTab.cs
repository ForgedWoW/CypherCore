// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.Guild;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Guilds;

public class GuildBankTab
{
    private readonly CharacterDatabase _characterDatabase;
    private readonly ItemFactory _itemFactory;
    private readonly ulong _guildId;
    private readonly Item[] _items = new Item[GuildConst.MaxBankSlots];
    private readonly GameObjectManager _objectManager;
    private readonly byte _tabId;

    public GuildBankTab(ulong guildId, byte tabId, GameObjectManager objectManager, CharacterDatabase characterDatabase, ItemFactory itemFactory)
    {
        _guildId = guildId;
        _tabId = tabId;
        _objectManager = objectManager;
        _characterDatabase = characterDatabase;
        _itemFactory = itemFactory;
    }

    public string Icon { get; private set; }

    public string Name { get; private set; }

    public string Text { get; private set; }

    public void Delete(SQLTransaction trans, bool removeItemsFromDB = false)
    {
        for (byte slotId = 0; slotId < GuildConst.MaxBankSlots; ++slotId)
        {
            var pItem = _items[slotId];

            if (pItem == null)
                continue;

            pItem.RemoveFromWorld();

            if (removeItemsFromDB)
                pItem.DeleteFromDB(trans);
        }
    }

    public Item GetItem(byte slotId)
    {
        return slotId < GuildConst.MaxBankSlots ? _items[slotId] : null;
    }

    public void LoadFromDB(SQLFields field)
    {
        Name = field.Read<string>(2);
        Icon = field.Read<string>(3);
        Text = field.Read<string>(4);
    }

    public bool LoadItemFromDB(SQLFields field)
    {
        var slotId = field.Read<byte>(53);
        var itemGuid = field.Read<uint>(0);
        var itemEntry = field.Read<uint>(1);

        if (slotId >= GuildConst.MaxBankSlots)
        {
            Log.Logger.Error("Invalid slot for item (GUID: {0}, id: {1}) in guild bank, skipped.", itemGuid, itemEntry);

            return false;
        }

        var proto = _objectManager.ItemTemplateCache.GetItemTemplate(itemEntry);

        if (proto == null)
        {
            Log.Logger.Error("Unknown item (GUID: {0}, id: {1}) in guild bank, skipped.", itemGuid, itemEntry);

            return false;
        }

        var pItem = _itemFactory.NewItemOrBag(proto);

        if (!pItem.LoadFromDB(itemGuid, ObjectGuid.Empty, field, itemEntry))
        {
            Log.Logger.Error("Item (GUID {0}, id: {1}) not found in ite_instance, deleting from guild bank!", itemGuid, itemEntry);

            var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_NONEXISTENT_GUILD_BANK_ITEM);
            stmt.AddValue(0, _guildId);
            stmt.AddValue(1, _tabId);
            stmt.AddValue(2, slotId);
            _characterDatabase.Execute(stmt);

            return false;
        }

        pItem.AddToWorld();
        _items[slotId] = pItem;

        return true;
    }

    public void SendText(Guild guild, WorldSession session = null)
    {
        GuildBankTextQueryResult textQuery = new()
        {
            Tab = _tabId,
            Text = Text
        };

        if (session != null)
        {
            Log.Logger.Debug("SMSG_GUILD_BANK_QUERY_TEXT_RESULT [{0}]: Tabid: {1}, Text: {2}", session.GetPlayerInfo(), _tabId, Text);
            session.SendPacket(textQuery);
        }
        else
        {
            Log.Logger.Debug("SMSG_GUILD_BANK_QUERY_TEXT_RESULT [Broadcast]: Tabid: {0}, Text: {1}", _tabId, Text);
            guild.BroadcastPacket(textQuery);
        }
    }

    public void SetInfo(string name, string icon)
    {
        if (Name == name && Icon == icon)
            return;

        Name = name;
        Icon = icon;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_TAB_INFO);
        stmt.AddValue(0, Name);
        stmt.AddValue(1, Icon);
        stmt.AddValue(2, _guildId);
        stmt.AddValue(3, _tabId);
        _characterDatabase.Execute(stmt);
    }

    public bool SetItem(SQLTransaction trans, byte slotId, Item item)
    {
        if (slotId >= GuildConst.MaxBankSlots)
            return false;

        _items[slotId] = item;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_ITEM);
        stmt.AddValue(0, _guildId);
        stmt.AddValue(1, _tabId);
        stmt.AddValue(2, slotId);
        trans.Append(stmt);

        if (item == null)
            return true;

        stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_GUILD_BANK_ITEM);
        stmt.AddValue(0, _guildId);
        stmt.AddValue(1, _tabId);
        stmt.AddValue(2, slotId);
        stmt.AddValue(3, item.GUID.Counter);
        trans.Append(stmt);

        item.SetContainedIn(ObjectGuid.Empty);
        item.SetOwnerGUID(ObjectGuid.Empty);
        item.FSetState(ItemUpdateState.New);
        item.SaveToDB(trans); // Not in inventory and can be saved standalone

        return true;
    }

    public void SetText(string text)
    {
        if (Text == text)
            return;

        Text = text;

        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_TAB_TEXT);
        stmt.AddValue(0, Text);
        stmt.AddValue(1, _guildId);
        stmt.AddValue(2, _tabId);
        _characterDatabase.Execute(stmt);
    }
}