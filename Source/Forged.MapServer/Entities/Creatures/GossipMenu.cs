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
	readonly SortedDictionary<uint, GossipMenuItem> _menuItems = new();
	uint _menuId;
	Locale _locale;

	public uint AddMenuItem(int gossipOptionId, int orderIndex, GossipOptionNpc optionNpc, string optionText, uint language,
							GossipOptionFlags flags, int? gossipNpcOptionId, uint actionMenuId, uint actionPoiId, bool boxCoded, uint boxMoney,
							string boxText, int? spellId, int? overrideIconId, uint sender, uint action)
	{
		// Find a free new id - script case
		if (orderIndex == -1)
		{
			orderIndex = 0;

			if (_menuId != 0)
			{
				// set baseline orderIndex as higher than whatever exists in db
				var bounds = Global.ObjectMgr.GetGossipMenuItemsMapBounds(_menuId);
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
			gossipOptionId = -((int)_menuId * 100 + orderIndex);

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
	///  Adds a localized gossip menu item from db by menu id and menu item id.
	/// </summary>
	/// <param name="menuId"> menuId Gossip menu id. </param>
	/// <param name="menuItemId"> menuItemId Gossip menu item id. </param>
	/// <param name="sender"> sender Identifier of the current menu. </param>
	/// <param name="action"> action Custom action given to OnGossipHello. </param>
	public void AddMenuItem(uint menuId, uint menuItemId, uint sender, uint action)
	{
		// Find items for given menu id.
		var bounds = Global.ObjectMgr.GetGossipMenuItemsMapBounds(menuId);

		// Return if there are none.
		if (bounds.Empty())
			return;

		/// Find the one with the given menu item id.
		var gossipMenuItems = bounds.Find(menuItem => menuItem.OrderIndex == menuItemId);

		if (gossipMenuItems == null)
			return;

		AddMenuItem(gossipMenuItems, sender, action);
	}

	public void AddMenuItem(GossipMenuItems menuItem, uint sender, uint action)
	{
		// Store texts for localization.
		string strOptionText, strBoxText;
		var optionBroadcastText = CliDB.BroadcastTextStorage.LookupByKey(menuItem.OptionBroadcastTextId);
		var boxBroadcastText = CliDB.BroadcastTextStorage.LookupByKey(menuItem.BoxBroadcastTextId);

		// OptionText
		if (optionBroadcastText != null)
		{
			strOptionText = Global.DB2Mgr.GetBroadcastTextValue(optionBroadcastText, GetLocale());
		}
		else
		{
			strOptionText = menuItem.OptionText;

			/// Find localizations from database.
			if (GetLocale() != Locale.enUS)
			{
				var gossipMenuLocale = Global.ObjectMgr.GetGossipMenuItemsLocale(menuItem.MenuId, menuItem.OrderIndex);

				if (gossipMenuLocale != null)
					ObjectManager.GetLocaleString(gossipMenuLocale.OptionText, GetLocale(), ref strOptionText);
			}
		}

		// BoxText
		if (boxBroadcastText != null)
		{
			strBoxText = Global.DB2Mgr.GetBroadcastTextValue(boxBroadcastText, GetLocale());
		}
		else
		{
			strBoxText = menuItem.BoxText;

			// Find localizations from database.
			if (GetLocale() != Locale.enUS)
			{
				var gossipMenuLocale = Global.ObjectMgr.GetGossipMenuItemsLocale(menuItem.MenuId, menuItem.OrderIndex);

				if (gossipMenuLocale != null)
					ObjectManager.GetLocaleString(gossipMenuLocale.BoxText, GetLocale(), ref strBoxText);
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

	public GossipMenuItem GetItem(int gossipOptionId)
	{
		return _menuItems.Values.FirstOrDefault(item => item.GossipOptionId == gossipOptionId);
	}

	public uint GetMenuItemSender(uint orderIndex)
	{
		var item = GetItemByIndex(orderIndex);

		if (item != null)
			return item.Sender;

		return 0;
	}

	public uint GetMenuItemAction(uint orderIndex)
	{
		var item = GetItemByIndex(orderIndex);

		if (item != null)
			return item.Action;

		return 0;
	}

	public bool IsMenuItemCoded(uint orderIndex)
	{
		var item = GetItemByIndex(orderIndex);

		if (item != null)
			return item.BoxCoded;

		return false;
	}

	public void ClearMenu()
	{
		_menuItems.Clear();
	}

	public void SetMenuId(uint menu_id)
	{
		_menuId = menu_id;
	}

	public uint GetMenuId()
	{
		return _menuId;
	}

	public void SetLocale(Locale locale)
	{
		_locale = locale;
	}

	public int GetMenuItemCount()
	{
		return _menuItems.Count;
	}

	public bool IsEmpty()
	{
		return _menuItems.Empty();
	}

	public SortedDictionary<uint, GossipMenuItem> GetMenuItems()
	{
		return _menuItems;
	}

	GossipMenuItem GetItemByIndex(uint orderIndex)
	{
		return _menuItems.LookupByKey(orderIndex);
	}

	Locale GetLocale()
	{
		return _locale;
	}
}