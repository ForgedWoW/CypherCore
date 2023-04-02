// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class GossipMenu
{
    private readonly CliDB _cliDB;
    private readonly DB2Manager _db2Manager;
    private readonly SortedDictionary<uint, GossipMenuItem> _menuItems = new();
    private readonly GameObjectManager _objectManager;
    public GossipMenu(GameObjectManager objectManager, CliDB cliDB, DB2Manager db2Manager)
    {
        _objectManager = objectManager;
        _cliDB = cliDB;
        _db2Manager = db2Manager;
    }

    public Locale Locale { get; set; }
    public uint MenuId { get; set; }
    public uint AddMenuItem(int gossipOptionId, int orderIndex, GossipOptionNpc optionNpc, string optionText, uint language,
                            GossipOptionFlags flags, int? gossipNpcOptionId, uint actionMenuId, uint actionPoiId, bool boxCoded, uint boxMoney,
                            string boxText, int? spellId, int? overrideIconId, uint sender, uint action)
    {
        // Find a free new id - script case
        if (orderIndex == -1)
        {
            orderIndex = 0;

            if (MenuId != 0)
            {
                // set baseline orderIndex as higher than whatever exists in db
                var bounds = _objectManager.GetGossipMenuItemsMapBounds(MenuId);
                var itr = bounds.MaxBy(a => a.OrderIndex);

                if (itr != null)
                    orderIndex = (int)(itr.OrderIndex + 1);
            }

            if (!_menuItems.Empty())
                foreach (var pair in _menuItems)
                {
                    if (pair.Value.OrderIndex > orderIndex)
                        break;

                    orderIndex = (int)pair.Value.OrderIndex + 1;
                }
        }

        if (gossipOptionId == 0)
            gossipOptionId = -((int)MenuId * 100 + orderIndex);

        GossipMenuItem menuItem = new()
        {
            GossipOptionId = gossipOptionId,
            OrderIndex = (uint)orderIndex,
            OptionNpc = optionNpc,
            OptionText = optionText,
            Language = language,
            Flags = flags,
            GossipNpcOptionId = gossipNpcOptionId,
            BoxCoded = boxCoded,
            BoxMoney = boxMoney,
            BoxText = boxText,
            SpellId = spellId,
            OverrideIconId = overrideIconId,
            ActionMenuId = actionMenuId,
            ActionPoiId = actionPoiId,
            Sender = sender,
            Action = action
        };

        _menuItems.Add((uint)orderIndex, menuItem);

        return (uint)orderIndex;
    }

    /// <summary>
    ///     Adds a localized gossip menu item from db by menu id and menu item id.
    /// </summary>
    /// <param name="menuId"> menuId Gossip menu id. </param>
    /// <param name="menuItemId"> menuItemId Gossip menu item id. </param>
    /// <param name="sender"> sender Identifier of the current menu. </param>
    /// <param name="action"> action Custom action given to OnGossipHello. </param>
    public void AddMenuItem(uint menuId, uint menuItemId, uint sender, uint action)
    {
        // Find items for given menu id.
        var bounds = _objectManager.GetGossipMenuItemsMapBounds(menuId);

        // Return if there are none.
        if (bounds.Empty())
            return;

        // Find the one with the given menu item id.
        var gossipMenuItems = bounds.Find(menuItem => menuItem.OrderIndex == menuItemId);

        if (gossipMenuItems == null)
            return;

        AddMenuItem(gossipMenuItems, sender, action);
    }

    public void AddMenuItem(GossipMenuItems menuItem, uint sender, uint action)
    {
        // Store texts for localization.
        string strOptionText, strBoxText;
        var optionBroadcastText = _cliDB.BroadcastTextStorage.LookupByKey(menuItem.OptionBroadcastTextId);
        var boxBroadcastText = _cliDB.BroadcastTextStorage.LookupByKey(menuItem.BoxBroadcastTextId);

        // OptionText
        if (optionBroadcastText != null)
        {
            strOptionText = _db2Manager.GetBroadcastTextValue(optionBroadcastText, Locale);
        }
        else
        {
            strOptionText = menuItem.OptionText;

            // Find localizations from database.
            if (Locale != Locale.enUS)
            {
                var gossipMenuLocale = _objectManager.GetGossipMenuItemsLocale(menuItem.MenuId, menuItem.OrderIndex);

                if (gossipMenuLocale != null)
                    GameObjectManager.GetLocaleString(gossipMenuLocale.OptionText, Locale, ref strOptionText);
            }
        }

        // BoxText
        if (boxBroadcastText != null)
        {
            strBoxText = _db2Manager.GetBroadcastTextValue(boxBroadcastText, Locale);
        }
        else
        {
            strBoxText = menuItem.BoxText;

            // Find localizations from database.
            if (Locale != Locale.enUS)
            {
                var gossipMenuLocale = _objectManager.GetGossipMenuItemsLocale(menuItem.MenuId, menuItem.OrderIndex);

                if (gossipMenuLocale != null)
                    GameObjectManager.GetLocaleString(gossipMenuLocale.BoxText, Locale, ref strBoxText);
            }
        }

        AddMenuItem(menuItem.GossipOptionId,
                    (int)menuItem.OrderIndex,
                    menuItem.OptionNpc,
                    strOptionText,
                    menuItem.Language,
                    menuItem.Flags,
                    menuItem.GossipNpcOptionId,
                    menuItem.ActionMenuId,
                    menuItem.ActionPoiId,
                    menuItem.BoxCoded,
                    menuItem.BoxMoney,
                    strBoxText,
                    menuItem.SpellId,
                    menuItem.OverrideIconId,
                    sender,
                    action);
    }

    public void ClearMenu()
    {
        _menuItems.Clear();
    }

    public GossipMenuItem GetItem(int gossipOptionId)
    {
        return _menuItems.Values.FirstOrDefault(item => item.GossipOptionId == gossipOptionId);
    }

    public uint GetMenuItemAction(uint orderIndex)
    {
        return GetItemByIndex(orderIndex)?.Action ?? 0;
    }

    public int GetMenuItemCount()
    {
        return _menuItems.Count;
    }

    public SortedDictionary<uint, GossipMenuItem> GetMenuItems()
    {
        return _menuItems;
    }

    public uint GetMenuItemSender(uint orderIndex)
    {
        return GetItemByIndex(orderIndex)?.Sender ?? 0;
    }
    public bool IsEmpty()
    {
        return _menuItems.Empty();
    }

    public bool IsMenuItemCoded(uint orderIndex)
    {
        return GetItemByIndex(orderIndex) is { BoxCoded: true };
    }
    private GossipMenuItem GetItemByIndex(uint orderIndex)
    {
        return _menuItems.LookupByKey(orderIndex);
    }
}