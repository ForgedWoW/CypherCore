// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.LootManagement;

public struct LootGroupInvalidSelector
{
    public LootGroupInvalidSelector(ushort lootMode, Player personalLooter, GameObjectManager objectManager, ConditionManager conditionManager)
    {
        _lootMode = lootMode;
        _personalLooter = personalLooter;
        _objectManager = objectManager;
        _conditionManager = conditionManager;
    }

    public bool Check(LootStoreItem item)
    {
        if ((item.Lootmode & _lootMode) == 0)
            return true;

        if (_personalLooter &&
            !LootItem.AllowedForPlayer(_personalLooter,
                                       null,
                                       item.Itemid,
                                       item.NeedsQuest,
                                       !item.NeedsQuest || _objectManager.GetItemTemplate(item.Itemid).HasFlag(ItemFlagsCustom.FollowLootRules),
                                       true,
                                       item.Conditions,
                                       _objectManager,
                                       _conditionManager))
            return true;

        return false;
    }

    private readonly ushort _lootMode;
    private readonly Player _personalLooter;
    private readonly GameObjectManager _objectManager;
    private readonly ConditionManager _conditionManager;
}