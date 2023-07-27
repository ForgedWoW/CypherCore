// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Framework.Constants;

namespace Forged.MapServer.LootManagement;

public struct LootGroupInvalidSelector
{
    private readonly ConditionManager _conditionManager;

    private readonly ushort _lootMode;

    private readonly ItemTemplateCache _itemTemplateCache;

    private readonly Player _personalLooter;

    public LootGroupInvalidSelector(ushort lootMode, Player personalLooter, ItemTemplateCache itemTemplateCache, ConditionManager conditionManager)
    {
        _lootMode = lootMode;
        _personalLooter = personalLooter;
        _itemTemplateCache = itemTemplateCache;
        _conditionManager = conditionManager;
    }

    public bool Check(LootStoreItem item)
    {
        if ((item.Lootmode & _lootMode) == 0)
            return true;

        return _personalLooter != null &&
               !LootItem.AllowedForPlayer(_personalLooter,
                                          null,
                                          item.Itemid,
                                          item.NeedsQuest,
                                          !item.NeedsQuest || _itemTemplateCache.GetItemTemplate(item.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                          true,
        item.Conditions,
                                          _itemTemplateCache,
                                          _conditionManager);
    }
}