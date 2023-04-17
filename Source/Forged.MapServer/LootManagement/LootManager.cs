// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.LootManagement;

public class LootManager : LootStoreBox
{
    private readonly CliDB _cliDB;
    private readonly ConditionManager _conditionManager;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;
    private readonly ItemEnchantmentManager _itemEnchantmentManager;
    private readonly LootFactory _lootFactory;
    private readonly LootStoreBox _lootStorage;
    private readonly ObjectAccessor _objectAccessor;
    private readonly GameObjectManager _objectManager;
    private readonly SpellManager _spellManager;
    private readonly WorldDatabase _worldDatabase;

    public LootManager(GameObjectManager objectManager, SpellManager spellManager, CliDB cliDB, ConditionManager conditionManager, IConfiguration configuration, WorldDatabase worldDatabase,
                       DB2Manager db2Manager, ObjectAccessor objectAccessor, LootStoreBox lootStorage, ItemEnchantmentManager itemEnchantmentManager, LootFactory lootFactory)
    {
        _objectManager = objectManager;
        _spellManager = spellManager;
        _cliDB = cliDB;
        _conditionManager = conditionManager;
        _configuration = configuration;
        _worldDatabase = worldDatabase;
        _db2Manager = db2Manager;
        _objectAccessor = objectAccessor;
        _lootStorage = lootStorage;
        _itemEnchantmentManager = itemEnchantmentManager;
        _lootFactory = lootFactory;
    }

    public Dictionary<ObjectGuid, Loot> GenerateDungeonEncounterPersonalLoot(uint dungeonEncounterId, uint lootId, LootStore store,
                                                                             LootType type, WorldObject lootOwner, uint minMoney, uint maxMoney, ushort lootMode, ItemContext context, List<Player> tappers)
    {
        Dictionary<Player, Loot> tempLoot = new();

        foreach (var tapper in tappers)
        {
            if (tapper.IsLockedToDungeonEncounter(dungeonEncounterId))
                continue;

            Loot loot = new(lootOwner.Location.Map, lootOwner.GUID, type, null, _conditionManager, _objectManager, _db2Manager, _objectAccessor, _lootStorage, _configuration, _lootFactory, _itemEnchantmentManager)
            {
                ItemContext = context,
                DungeonEncounterId = dungeonEncounterId
            };

            loot.GenerateMoneyLoot(minMoney, maxMoney);

            tempLoot[tapper] = loot;
        }

        var tab = store.GetLootFor(lootId);

        tab?.ProcessPersonalLoot(tempLoot, store.IsRatesAllowed, lootMode);

        Dictionary<ObjectGuid, Loot> personalLoot = new();

        foreach (var (looter, loot) in tempLoot)
        {
            loot.FillNotNormalLootFor(looter);

            if (loot.IsLooted())
                continue;

            personalLoot[looter.GUID] = loot;
        }

        return personalLoot;
    }

    public void LoadLootTables()
    {
        Initialize();
        LoadLootTemplates_Creature();
        LoadLootTemplates_Fishing();
        LoadLootTemplates_Gameobject();
        LoadLootTemplates_Item();
        LoadLootTemplates_Mail();
        LoadLootTemplates_Milling();
        LoadLootTemplates_Pickpocketing();
        LoadLootTemplates_Skinning();
        LoadLootTemplates_Disenchant();
        LoadLootTemplates_Prospecting();
        LoadLootTemplates_Spell();

        LoadLootTemplates_Reference();
    }

    public void LoadLootTemplates_Creature()
    {
        Log.Logger.Information("Loading creature loot templates...");

        var oldMSTime = Time.MSTime;

        List<uint> lootIdSetUsed = new();
        var count = Creature.LoadAndCollectLootIds(out var lootIdSet);

        // Remove real entries and check loot existence
        var ctc = _objectManager.GetCreatureTemplates();

        foreach (var pair in ctc)
        {
            var lootid = pair.Value.LootId;

            if (lootid != 0)
            {
                if (!lootIdSet.Contains(lootid))
                    Creature.ReportNonExistingId(lootid, pair.Value.Entry);
                else
                    lootIdSetUsed.Add(lootid);
            }
        }

        foreach (var id in lootIdSetUsed)
            lootIdSet.Remove(id);

        // 1 means loot for player corpse
        lootIdSet.Remove(SharedConst.PlayerCorpseLootEntry);

        // output error for any still listed (not referenced from appropriate table) ids
        Creature.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} creature loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 creature loot templates. DB table `creature_loot_template` is empty");
    }

    public void LoadLootTemplates_Disenchant()
    {
        Log.Logger.Information("Loading disenchanting loot templates...");

        var oldMSTime = Time.MSTime;

        List<uint> lootIdSetUsed = new();
        var count = Disenchant.LoadAndCollectLootIds(out var lootIdSet);

        foreach (var disenchant in _cliDB.ItemDisenchantLootStorage.Values)
        {
            var lootid = disenchant.Id;

            if (!lootIdSet.Contains(lootid))
                Disenchant.ReportNonExistingId(lootid, disenchant.Id);
            else
                lootIdSetUsed.Add(lootid);
        }

        foreach (var id in lootIdSetUsed)
            lootIdSet.Remove(id);

        // output error for any still listed (not referenced from appropriate table) ids
        Disenchant.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} disenchanting loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 disenchanting loot templates. DB table `disenchant_loot_template` is empty");
    }

    public void LoadLootTemplates_Fishing()
    {
        Log.Logger.Information("Loading fishing loot templates...");

        var oldMSTime = Time.MSTime;

        var count = Fishing.LoadAndCollectLootIds(out var lootIdSet);

        // remove real entries and check existence loot
        foreach (var areaEntry in _cliDB.AreaTableStorage.Values)
            if (lootIdSet.Contains(areaEntry.Id))
                lootIdSet.Remove(areaEntry.Id);

        // output error for any still listed (not referenced from appropriate table) ids
        Fishing.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} fishing loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 fishing loot templates. DB table `fishing_loot_template` is empty");
    }

    public void LoadLootTemplates_Gameobject()
    {
        Log.Logger.Information("Loading gameobject loot templates...");

        var oldMSTime = Time.MSTime;

        List<uint> lootIdSetUsed = new();
        var count = Gameobject.LoadAndCollectLootIds(out var lootIdSet);

        void CheckLootId(uint lootId, uint gameObjectId)
        {
            if (!lootIdSet.Contains(lootId))
                Gameobject.ReportNonExistingId(lootId, gameObjectId);
            else
                lootIdSetUsed.Add(lootId);
        }

        // remove real entries and check existence loot
        var gotc = _objectManager.GetGameObjectTemplates();

        foreach (var (gameObjectId, gameObjectTemplate) in gotc)
        {
            var lootid = gameObjectTemplate.GetLootId();

            if (lootid != 0)
                CheckLootId(lootid, gameObjectId);

            if (gameObjectTemplate.type != GameObjectTypes.Chest)
                continue;

            if (gameObjectTemplate.Chest.chestPersonalLoot != 0)
                CheckLootId(gameObjectTemplate.Chest.chestPersonalLoot, gameObjectId);

            if (gameObjectTemplate.Chest.chestPushLoot != 0)
                CheckLootId(gameObjectTemplate.Chest.chestPushLoot, gameObjectId);
        }

        foreach (var id in lootIdSetUsed)
            lootIdSet.Remove(id);

        // output error for any still listed (not referenced from appropriate table) ids
        Gameobject.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} gameobject loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 gameobject loot templates. DB table `gameobject_loot_template` is empty");
    }

    public void LoadLootTemplates_Item()
    {
        Log.Logger.Information("Loading item loot templates...");

        var oldMSTime = Time.MSTime;

        var count = Items.LoadAndCollectLootIds(out var lootIdSet);

        // remove real entries and check existence loot
        var its = _objectManager.GetItemTemplates();

        foreach (var pair in its)
            if (lootIdSet.Contains(pair.Value.Id) && pair.Value.HasFlag(ItemFlags.HasLoot))
                lootIdSet.Remove(pair.Value.Id);

        // output error for any still listed (not referenced from appropriate table) ids
        Items.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} item loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 item loot templates. DB table `item_loot_template` is empty");
    }

    public void LoadLootTemplates_Mail()
    {
        Log.Logger.Information("Loading mail loot templates...");

        var oldMSTime = Time.MSTime;

        var count = Mail.LoadAndCollectLootIds(out var lootIdSet);

        // remove real entries and check existence loot
        foreach (var mail in _cliDB.MailTemplateStorage.Values)
            if (lootIdSet.Contains(mail.Id))
                lootIdSet.Remove(mail.Id);

        // output error for any still listed (not referenced from appropriate table) ids
        Mail.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} mail loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 mail loot templates. DB table `mail_loot_template` is empty");
    }

    public void LoadLootTemplates_Milling()
    {
        Log.Logger.Information("Loading milling loot templates...");

        var oldMSTime = Time.MSTime;

        var count = Milling.LoadAndCollectLootIds(out var lootIdSet);

        // remove real entries and check existence loot
        var its = _objectManager.GetItemTemplates();

        foreach (var pair in its)
        {
            if (!pair.Value.HasFlag(ItemFlags.IsMillable))
                continue;

            if (lootIdSet.Contains(pair.Value.Id))
                lootIdSet.Remove(pair.Value.Id);
        }

        // output error for any still listed (not referenced from appropriate table) ids
        Milling.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} milling loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 milling loot templates. DB table `milling_loot_template` is empty");
    }

    public void LoadLootTemplates_Pickpocketing()
    {
        Log.Logger.Information("Loading pickpocketing loot templates...");

        var oldMSTime = Time.MSTime;

        List<uint> lootIdSetUsed = new();
        var count = Pickpocketing.LoadAndCollectLootIds(out var lootIdSet);

        // Remove real entries and check loot existence
        var ctc = _objectManager.GetCreatureTemplates();

        foreach (var pair in ctc)
        {
            var lootid = pair.Value.PickPocketId;

            if (lootid != 0)
            {
                if (!lootIdSet.Contains(lootid))
                    Pickpocketing.ReportNonExistingId(lootid, pair.Value.Entry);
                else
                    lootIdSetUsed.Add(lootid);
            }
        }

        foreach (var id in lootIdSetUsed)
            lootIdSet.Remove(id);

        // output error for any still listed (not referenced from appropriate table) ids
        Pickpocketing.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} pickpocketing loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 pickpocketing loot templates. DB table `pickpocketing_loot_template` is empty");
    }

    public void LoadLootTemplates_Prospecting()
    {
        Log.Logger.Information("Loading prospecting loot templates...");

        var oldMSTime = Time.MSTime;

        var count = Prospecting.LoadAndCollectLootIds(out var lootIdSet);

        // remove real entries and check existence loot
        var its = _objectManager.GetItemTemplates();

        foreach (var pair in its)
        {
            if (!pair.Value.HasFlag(ItemFlags.IsProspectable))
                continue;

            if (lootIdSet.Contains(pair.Value.Id))
                lootIdSet.Remove(pair.Value.Id);
        }

        // output error for any still listed (not referenced from appropriate table) ids
        Prospecting.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} prospecting loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 prospecting loot templates. DB table `prospecting_loot_template` is empty");
    }

    public void LoadLootTemplates_Reference()
    {
        Log.Logger.Information("Loading reference loot templates...");

        var oldMSTime = Time.MSTime;

        Reference.LoadAndCollectLootIds(out var lootIdSet);

        // check references and remove used
        Creature.CheckLootRefs(lootIdSet);
        Fishing.CheckLootRefs(lootIdSet);
        Gameobject.CheckLootRefs(lootIdSet);
        Items.CheckLootRefs(lootIdSet);
        Milling.CheckLootRefs(lootIdSet);
        Pickpocketing.CheckLootRefs(lootIdSet);
        Skinning.CheckLootRefs(lootIdSet);
        Disenchant.CheckLootRefs(lootIdSet);
        Prospecting.CheckLootRefs(lootIdSet);
        Mail.CheckLootRefs(lootIdSet);
        Reference.CheckLootRefs(lootIdSet);

        // output error for any still listed ids (not referenced from any loot table)
        Reference.ReportUnusedIds(lootIdSet);

        Log.Logger.Information("Loaded reference loot templates in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadLootTemplates_Skinning()
    {
        Log.Logger.Information("Loading skinning loot templates...");

        var oldMSTime = Time.MSTime;

        List<uint> lootIdSetUsed = new();
        var count = Skinning.LoadAndCollectLootIds(out var lootIdSet);

        // remove real entries and check existence loot
        var ctc = _objectManager.GetCreatureTemplates();

        foreach (var pair in ctc)
        {
            var lootid = pair.Value.SkinLootId;

            if (lootid != 0)
            {
                if (!lootIdSet.Contains(lootid))
                    Skinning.ReportNonExistingId(lootid, pair.Value.Entry);
                else
                    lootIdSetUsed.Add(lootid);
            }
        }

        foreach (var id in lootIdSetUsed)
            lootIdSet.Remove(id);

        // output error for any still listed (not referenced from appropriate table) ids
        Skinning.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} skinning loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 skinning loot templates. DB table `skinning_loot_template` is empty");
    }

    public void LoadLootTemplates_Spell()
    {
        // TODO: change this to use MiscValue from spell effect as id instead of spell id
        Log.Logger.Information("Loading spell loot templates...");

        var oldMSTime = Time.MSTime;

        var count = Spell.LoadAndCollectLootIds(out var lootIdSet);

        // remove real entries and check existence loot
        foreach (var spellNameEntry in _cliDB.SpellNameStorage.Values)
        {
            var spellInfo = _spellManager.GetSpellInfo(spellNameEntry.Id);

            if (spellInfo == null)
                continue;

            // possible cases
            if (!spellInfo.IsLootCrafting)
                continue;

            if (!lootIdSet.Contains(spellInfo.Id))
            {
                // not report about not trainable spells (optionally supported by DB)
                // ignore 61756 (Northrend Inscription Research (FAST QA VERSION) for example
                if (!spellInfo.HasAttribute(SpellAttr0.NotShapeshifted) || spellInfo.HasAttribute(SpellAttr0.IsTradeskill))
                    Spell.ReportNonExistingId(spellInfo.Id, spellInfo.Id);
            }
            else
                lootIdSet.Remove(spellInfo.Id);
        }

        // output error for any still listed (not referenced from appropriate table) ids
        Spell.ReportUnusedIds(lootIdSet);

        if (count != 0)
            Log.Logger.Information("Loaded {0} spell loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        else
            Log.Logger.Information("Loaded 0 spell loot templates. DB table `spell_loot_template` is empty");
    }

    private void Initialize()
    {
        Creature = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "creature_loot_template", "creature entry");
        Disenchant = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "disenchant_loot_template", "item disenchant id");
        Fishing = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "fishing_loot_template", "area id");
        Gameobject = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "gameobject_loot_template", "gameobject entry");
        Items = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "item_loot_template", "item entry");
        Mail = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "mail_loot_template", "mail template id");
        Milling = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "milling_loot_template", "item entry (herb)");
        Pickpocketing = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "pickpocketing_loot_template", "creature pickpocket lootid");
        Prospecting = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "prospecting_loot_template", "item entry (ore)");
        Reference = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "reference_loot_template", "reference id");
        Skinning = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "skinning_loot_template", "creature skinning id");
        Spell = new LootStore(_configuration, _worldDatabase, _conditionManager, _objectManager, _lootStorage, "spell_loot_template", "spell id (random item creating)");
    }
}