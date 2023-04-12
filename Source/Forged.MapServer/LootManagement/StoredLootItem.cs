// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.LootManagement;

internal class StoredLootItem
{
    public StoredLootItem(LootItem lootItem)
    {
        ItemId = lootItem.Itemid;
        Count = lootItem.Count;
        ItemIndex = lootItem.LootListId;
        FollowRules = lootItem.FollowLootRules;
        FFA = lootItem.Freeforall;
        Blocked = lootItem.IsBlocked;
        Counted = lootItem.IsCounted;
        UnderThreshold = lootItem.IsUnderthreshold;
        NeedsQuest = lootItem.NeedsQuest;
        RandomBonusListId = lootItem.RandomBonusListId;
        Context = lootItem.Context;
        BonusListIDs = lootItem.BonusListIDs;
    }

    public bool Blocked { get; set; }
    public List<uint> BonusListIDs { get; set; }
    public ItemContext Context { get; set; }
    public uint Count { get; set; }
    public bool Counted { get; set; }
    public bool FFA { get; set; }
    public bool FollowRules { get; set; }
    public uint ItemId { get; set; }
    public uint ItemIndex { get; set; }
    public bool NeedsQuest { get; set; }
    public uint RandomBonusListId { get; set; }
    public bool UnderThreshold { get; set; }
}