// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Items;
using Framework.Collections;
using Framework.Constants;
using Framework.IO;
using Serilog;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("additem")]
internal class MiscAddItemCommands
{
    [Command("", RBACPermissions.CommandAdditem)]
    private static bool HandleAddItemCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        uint itemId = 0;

        if (args[0] == '[') // [name] manual form
        {
            var itemName = args.NextString("]")[1..];

            if (!string.IsNullOrEmpty(itemName))
            {
                var record = handler.CliDB.ItemSparseStorage.Values.FirstOrDefault(itemSparse =>
                {
                    for (Locale i = 0; i < Locale.Total; ++i)
                        if (itemName == itemSparse.Display[i])
                            return true;

                    return false;
                });

                if (record == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandCouldnotfind, itemName);

                    return false;
                }

                itemId = record.Id;
            }
            else
                return false;
        }
        else // item_id or [name] Shift-click form |color|Hitem:item_id:0:0:0|h[name]|h|r
        {
            var idStr = handler.ExtractKeyFromLink(args, "Hitem");

            if (string.IsNullOrEmpty(idStr))
                return false;

            if (!uint.TryParse(idStr, out itemId))
                return false;
        }

        var count = args.NextInt32();

        count = count switch
        {
            0 => 1,
            _ => count
        };

        List<uint> bonusListIDs = new();
        var bonuses = args.NextString();
        var context = args.NextString();

        // semicolon separated bonuslist ids (parse them after all arguments are extracted by strtok!)
        if (!bonuses.IsEmpty())
        {
            var tokens = new StringArray(bonuses, ';');

            for (var i = 0; i < tokens.Length; ++i)
                if (uint.TryParse(tokens[i], out var id))
                    bonusListIDs.Add(id);
        }

        var itemContext = ItemContext.None;

        if (!context.IsEmpty())
        {
            itemContext = context.ToEnum<ItemContext>();

            if (itemContext != ItemContext.None && itemContext < ItemContext.Max)
            {
                var contextBonuses = handler.ClassFactory.Resolve<DB2Manager>().GetDefaultItemBonusTree(itemId, itemContext);
                bonusListIDs.AddRange(contextBonuses);
            }
        }

        var player = handler.Session.Player;
        var playerTarget = handler.SelectedPlayer;

        if (!playerTarget)
            playerTarget = player;

        var itemTemplate = handler.ObjectManager.GetItemTemplate(itemId);

        if (itemTemplate == null)
        {
            handler.SendSysMessage(CypherStrings.CommandItemidinvalid, itemId);

            return false;
        }

        // Subtract
        if (count < 0)
        {
            var destroyedItemCount = playerTarget.DestroyItemCount(itemId, (uint)-count, true, false);

            if (destroyedItemCount > 0)
            {
                // output the amount of items successfully destroyed
                handler.SendSysMessage(CypherStrings.Removeitem, itemId, destroyedItemCount, handler.GetNameLink(playerTarget));

                // check to see if we were unable to destroy all of the amount requested.
                var unableToDestroyItemCount = (uint)(-count - destroyedItemCount);

                if (unableToDestroyItemCount > 0)
                    // output message for the amount of items we couldn't destroy
                    handler.SendSysMessage(CypherStrings.RemoveitemFailure, itemId, unableToDestroyItemCount, handler.GetNameLink(playerTarget));
            }
            else
                // failed to destroy items of the amount requested
                handler.SendSysMessage(CypherStrings.RemoveitemFailure, itemId, -count, handler.GetNameLink(playerTarget));

            return true;
        }

        // Adding items

        // check space and find places
        List<ItemPosCount> dest = new();
        var msg = playerTarget.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemId, (uint)count, out var noSpaceForCount);

        if (msg != InventoryResult.Ok) // convert to possible store amount
            count -= (int)noSpaceForCount;

        if (count == 0 || dest.Empty()) // can't add any
        {
            handler.SendSysMessage(CypherStrings.ItemCannotCreate, itemId, noSpaceForCount);

            return false;
        }

        var item = playerTarget.StoreNewItem(dest, itemId, true, handler.ClassFactory.Resolve<ItemEnchantmentManager>().GenerateItemRandomBonusListId(itemId), null, itemContext, bonusListIDs);

        // remove binding (let GM give it to another player later)
        if (player == playerTarget)
            foreach (var posCount in dest)
            {
                var item1 = player.GetItemByPos(posCount.Pos);

                if (item1)
                    item1.SetBinding(false);
            }

        if (count > 0 && item)
        {
            player.SendNewItem(item, (uint)count, false, true);
            handler.SendSysMessage(CypherStrings.Additem, itemId, count, handler.GetNameLink(playerTarget));

            if (player != playerTarget)
                playerTarget.SendNewItem(item, (uint)count, true, false);
        }

        if (noSpaceForCount > 0)
            handler.SendSysMessage(CypherStrings.ItemCannotCreate, itemId, noSpaceForCount);

        return true;
    }

    [Command("set", RBACPermissions.CommandAdditemset)]
    private static bool HandleAddItemSetCommand(CommandHandler handler, uint itemSetId, [OptionalArg] string bonuses, byte? context)
    {
        // prevent generation all items with itemset field value '0'
        if (itemSetId == 0)
        {
            handler.SendSysMessage(CypherStrings.NoItemsFromItemsetFound, itemSetId);

            return false;
        }

        List<uint> bonusListIDs = new();

        // semicolon separated bonuslist ids (parse them after all arguments are extracted by strtok!)
        if (!bonuses.IsEmpty())
        {
            var tokens = new StringArray(bonuses, ';');

            for (var i = 0; i < tokens.Length; ++i)
                if (uint.TryParse(tokens[i], out var id))
                    bonusListIDs.Add(id);
        }

        var itemContext = ItemContext.None;

        if (context.HasValue)
            itemContext = (ItemContext)context;

        var player = handler.Session.Player;
        var playerTarget = handler.SelectedPlayer;

        if (!playerTarget)
            playerTarget = player;

        Log.Logger.Debug(handler.ObjectManager.GetCypherString(CypherStrings.Additemset), itemSetId);

        var found = false;
        var its = handler.ObjectManager.GetItemTemplates();

        foreach (var template in its)
        {
            if (template.Value.ItemSet != itemSetId)
                continue;

            found = true;
            List<ItemPosCount> dest = new();
            var msg = playerTarget.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, template.Value.Id, 1);

            if (msg == InventoryResult.Ok)
            {
                List<uint> bonusListIDsForItem = new(bonusListIDs); // copy, bonuses for each depending on context might be different for each item

                if (itemContext != ItemContext.None && itemContext < ItemContext.Max)
                {
                    var contextBonuses = handler.ClassFactory.Resolve<DB2Manager>().GetDefaultItemBonusTree(template.Value.Id, itemContext);
                    bonusListIDsForItem.AddRange(contextBonuses);
                }

                var item = playerTarget.StoreNewItem(dest, template.Value.Id, true, 0, null, itemContext, bonusListIDsForItem);

                // remove binding (let GM give it to another player later)
                if (player == playerTarget)
                    item.SetBinding(false);

                player.SendNewItem(item, 1, false, true);

                if (player != playerTarget)
                    playerTarget.SendNewItem(item, 1, true, false);
            }
            else
            {
                player.SendEquipError(msg, null, null, template.Value.Id);
                handler.SendSysMessage(CypherStrings.ItemCannotCreate, template.Value.Id, 1);
            }
        }

        if (!found)
        {
            handler.SendSysMessage(CypherStrings.CommandNoitemsetfound, itemSetId);

            return false;
        }

        return true;
    }

    [Command("to", RBACPermissions.CommandAdditemset)]
    private static bool HandleAddItemToCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var player = handler.Session.Player;

        if (!handler.ExtractPlayerTarget(args, out var playerTarget))
            return false;

        var tailArgs = new StringArguments(args.NextString(""));

        if (tailArgs.Empty())
            return false;

        uint itemId = 0;

        if (tailArgs[0] == '[') // [name] manual form
        {
            var itemNameStr = tailArgs.NextString("]");

            if (!itemNameStr.IsEmpty())
            {
                var itemName = itemNameStr[1..];

                var itr = handler.CliDB.ItemSparseStorage.Values.FirstOrDefault(sparse =>
                {
                    for (var i = Locale.enUS; i < Locale.Total; ++i)
                        if (itemName == sparse.Display[i])
                            return true;

                    return false;
                });

                if (itr == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandCouldnotfind, itemName);

                    return false;
                }

                itemId = itr.Id;
            }
            else
                return false;
        }
        else // item_id or [name] Shift-click form |color|Hitem:item_id:0:0:0|h[name]|h|r
        {
            var id = handler.ExtractKeyFromLink(tailArgs, "Hitem");

            if (id.IsEmpty())
                return false;

            itemId = uint.Parse(id);
        }

        var ccount = tailArgs.NextString();

        var count = 1;

        if (!ccount.IsEmpty())
            count = int.Parse(ccount);

        count = count switch
        {
            0 => 1,
            _ => count
        };

        List<uint> bonusListIDs = new();
        var bonuses = tailArgs.NextString();

        var context = tailArgs.NextString();

        var itemContext = ItemContext.None;

        if (!context.IsEmpty())
        {
            itemContext = context.ToEnum<ItemContext>();

            if (itemContext != ItemContext.None && itemContext < ItemContext.Max)
            {
                var contextBonuses = handler.ClassFactory.Resolve<DB2Manager>().GetDefaultItemBonusTree(itemId, itemContext);
                bonusListIDs.AddRange(contextBonuses);
            }
        }

        // semicolon separated bonuslist ids
        if (!bonuses.IsEmpty())
            foreach (var token in bonuses.Split(';', StringSplitOptions.RemoveEmptyEntries))
                if (uint.TryParse(token, out var bonusListId))
                    bonusListIDs.Add(bonusListId);

        var itemTemplate = handler.ObjectManager.GetItemTemplate(itemId);

        if (itemTemplate == null)
        {
            handler.SendSysMessage(CypherStrings.CommandItemidinvalid, itemId);

            return false;
        }

        // Subtract
        if (count < 0)
        {
            var destroyedItemCount = playerTarget.DestroyItemCount(itemId, (uint)-count, true, false);

            if (destroyedItemCount > 0)
            {
                // output the amount of items successfully destroyed
                handler.SendSysMessage(CypherStrings.Removeitem, itemId, destroyedItemCount, handler.GetNameLink(playerTarget));

                // check to see if we were unable to destroy all of the amount requested.
                var unableToDestroyItemCount = (uint)(-count - destroyedItemCount);

                if (unableToDestroyItemCount > 0)
                    // output message for the amount of items we couldn't destroy
                    handler.SendSysMessage(CypherStrings.RemoveitemFailure, itemId, unableToDestroyItemCount, handler.GetNameLink(playerTarget));
            }
            else
                // failed to destroy items of the amount requested
                handler.SendSysMessage(CypherStrings.RemoveitemFailure, itemId, -count, handler.GetNameLink(playerTarget));

            return true;
        }

        // Adding items

        // check space and find places
        List<ItemPosCount> dest = new();
        var msg = playerTarget.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemId, (uint)count, out var noSpaceForCount);

        if (msg != InventoryResult.Ok) // convert to possible store amount
            count -= (int)noSpaceForCount;

        if (count == 0 || dest.Empty()) // can't add any
        {
            handler.SendSysMessage(CypherStrings.ItemCannotCreate, itemId, noSpaceForCount);

            return false;
        }

        var item = playerTarget.StoreNewItem(dest, itemId, true, handler.ClassFactory.Resolve<ItemEnchantmentManager>().GenerateItemRandomBonusListId(itemId), null, itemContext, bonusListIDs);

        // remove binding (let GM give it to another player later)
        if (player == playerTarget)
            foreach (var itemPostCount in dest)
            {
                var item1 = player.GetItemByPos(itemPostCount.Pos);

                item1?.SetBinding(false);
            }

        if (count > 0 && item)
        {
            player.SendNewItem(item, (uint)count, false, true);

            if (player != playerTarget)
                playerTarget.SendNewItem(item, (uint)count, true, false);
        }

        if (noSpaceForCount > 0)
            handler.SendSysMessage(CypherStrings.ItemCannotCreate, itemId, noSpaceForCount);

        return true;
    }
}