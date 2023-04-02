// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.Globals;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.LootManagement;

public class LootStoreItem
{
    public static string[] QualityToRate =
    {
        "Rate.Drop.Item.Poor",      // ITEM_QUALITY_POOR
        "Rate.Drop.Item.Normal",    // ITEM_QUALITY_NORMAL
        "Rate.Drop.Item.Uncommon",  // ITEM_QUALITY_UNCOMMON
        "Rate.Drop.Item.Rare",      // ITEM_QUALITY_RARE
        "Rate.Drop.Item.Epic",      // ITEM_QUALITY_EPIC
        "Rate.Drop.Item.Legendary", // ITEM_QUALITY_LEGENDARY
        "Rate.Drop.Item.Artifact",  // ITEM_QUALITY_ARTIFACT
    };

    public float Chance;
    public List<Condition> Conditions;
    public byte Groupid;
    public uint Itemid;    // id of the item
                           // chance to drop for both quest and non-quest items, chance to be used for refs
    public ushort Lootmode;

    public byte Maxcount;
    public byte Mincount;
    public bool NeedsQuest;
    public uint Reference; // referenced TemplateleId
    private readonly IConfiguration _configuration;

    // quest drop (negative ChanceOrQuestChance in DB)
    // mincount for drop items
    // max drop count for the item mincount or Ref multiplicator
    // additional loot condition
    private readonly GameObjectManager _objectManager;
    private readonly WorldDatabase _worldDatabase;

    public LootStoreItem(uint itemid, uint reference, float chance, bool needsQuest, ushort lootmode, byte groupid, byte mincount, byte maxcount, GameObjectManager objectManager, IConfiguration configuration, WorldDatabase worldDatabase)
    {
        Itemid = itemid;
        Reference = reference;
        Chance = chance;
        Lootmode = lootmode;
        NeedsQuest = needsQuest;
        Groupid = groupid;
        Mincount = mincount;
        Maxcount = maxcount;
        _objectManager = objectManager;
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        Conditions = new List<Condition>();
    }

    public bool IsValid(LootStore store, uint entry)
    {
        if (Mincount == 0)
        {
            Log.Logger.Error("Table '{0}' entry {1} item {2}: wrong mincount ({3}) - skipped", store.GetName(), entry, Itemid, Reference);

            return false;
        }

        if (Reference == 0) // item (quest or non-quest) entry, maybe grouped
        {
            var proto = _objectManager.GetItemTemplate(Itemid);

            if (proto == null)
            {
                if (_configuration.GetDefaultValue("load.autoclean", false))
                    _worldDatabase.Execute($"DELETE FROM {store.GetName()} WHERE Entry = {Itemid}");
                else
                    Log.Logger.Error("Table '{0}' entry {1} item {2}: item does not exist - skipped", store.GetName(), entry, Itemid);

                return false;
            }

            if (Chance == 0 && Groupid == 0) // Zero chance is allowed for grouped entries only
            {
                Log.Logger.Error("Table '{0}' entry {1} item {2}: equal-chanced grouped entry, but group not defined - skipped", store.GetName(), entry, Itemid);

                return false;
            }

            if (Chance != 0 && Chance < 0.000001f) // loot with low chance
            {
                Log.Logger.Error("Table '{0}' entry {1} item {2}: low chance ({3}) - skipped",
                                 store.GetName(),
                                 entry,
                                 Itemid,
                                 Chance);

                return false;
            }

            if (Maxcount < Mincount) // wrong max count
            {
                Log.Logger.Error("Table '{0}' entry {1} item {2}: max count ({3}) less that min count ({4}) - skipped", store.GetName(), entry, Itemid, Maxcount, Reference);

                return false;
            }
        }
        else // mincountOrRef < 0
        {
            if (NeedsQuest)
            {
                Log.Logger.Error("Table '{0}' entry {1} item {2}: quest chance will be treated as non-quest chance", store.GetName(), entry, Itemid);
            }
            else if (Chance == 0) // no chance for the reference
            {
                Log.Logger.Error("Table '{0}' entry {1} item {2}: zero chance is specified for a reference, skipped", store.GetName(), entry, Itemid);

                return false;
            }
        }

        return true; // Referenced template existence is checked at whole store level
    }

    public bool Roll(bool rate)
    {
        if (Chance >= 100.0f)
            return true;

        if (Reference > 0) // reference case
            return RandomHelper.randChance(Chance * (rate ? _configuration.GetDefaultValue("Rate.Drop.Item.Referenced", 1.0f) : 1.0f));

        var pProto = _objectManager.GetItemTemplate(Itemid);

        var qualityModifier = pProto != null && rate ? _configuration.GetDefaultValue(QualityToRate[(int)pProto.Quality], 1.0f) : 1.0f;

        return RandomHelper.randChance(Chance * qualityModifier);
    }
}