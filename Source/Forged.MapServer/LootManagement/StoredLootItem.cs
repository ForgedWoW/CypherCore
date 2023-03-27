// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.LootManagement;

internal class StoredLootItem
{
    public uint ItemId;
    public uint Count;
    public uint ItemIndex;
    public bool FollowRules;
    public bool FFA;
    public bool Blocked;
    public bool Counted;
    public bool UnderThreshold;
    public bool NeedsQuest;
    public uint RandomBonusListId;
    public ItemContext Context;
    public List<uint> BonusListIDs;

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
}