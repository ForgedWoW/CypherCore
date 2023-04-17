// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IItem;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.World.ItemScripts;

internal struct SpellIds
{
    //Onlyforflight
    public const uint ARCANE_CHARGES = 45072;

    //Petrovclusterbombs
    public const uint PETROV_BOMB = 42406;
}

internal struct CreatureIds
{
    //Pilefakefur
    public const uint NESINGWARY_TRAPPER = 25835;

    //Theemissary
    public const uint LEVIROTH = 26452;

    //Capturedfrog
    public const uint VANIRAS_SENTRY_TOTEM = 40187;
}

internal struct GameObjectIds
{
    //Pilefakefur
    public const uint HIGH_QUALITY_FUR = 187983;

    public static uint[] CaribouTraps =
    {
        187982, 187995, 187996, 187997, 187998, 187999, 188000, 188001, 188002, 188003, 188004, 188005, 188006, 188007, 188008
    };
}

internal struct QuestIds
{
    //Helpthemselves
    public const uint CANNOT_HELP_THEMSELVES = 11876;

    //Theemissary
    public const uint THE_EMISSARY = 11626;

    //Capturedfrog
    public const uint THE_PERFECT_SPIES = 25444;
}

internal struct Misc
{
    //Petrovclusterbombs
    public const uint AREA_ID_SHATTERED_STRAITS = 4064;
    public const uint ZONE_ID_HOWLING = 495;
}

[Script]
internal class ItemOnlyForFlight : ScriptObjectAutoAddDBBound, IItemOnUse
{
    public ItemOnlyForFlight() : base("item_only_for_flight") { }

    public bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
    {
        var itemId = item.Entry;
        var disabled = false;

        //for special scripts
        switch (itemId)
        {
            case 24538:
                if (player.Area != 3628)
                    disabled = true;

                break;
            case 34489:
                if (player.Zone != 4080)
                    disabled = true;

                break;
            case 34475:
                var spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.ARCANE_CHARGES, player.Map.DifficultyID);

                if (spellInfo != null)
                    Spell.SendCastResult(player, spellInfo, default, castId, SpellCastResult.NotOnGround);

                break;
        }

        // allow use in flight only
        if (player.IsInFlight &&
            !disabled)
            return false;

        // error
        player.SendEquipError(InventoryResult.ClientLockedOut, item, null);

        return true;
    }
}

[Script]
internal class ItemGorDreksOintment : ScriptObjectAutoAddDBBound, IItemOnUse
{
    public ItemGorDreksOintment() : base("item_gor_dreks_ointment") { }

    public bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
    {
        if (targets.UnitTarget &&
            targets.UnitTarget.IsTypeId(TypeId.Unit) &&
            targets.UnitTarget.Entry == 20748 &&
            !targets.UnitTarget.HasAura(32578))
            return false;

        player.SendEquipError(InventoryResult.ClientLockedOut, item, null);

        return true;
    }
}

[Script]
internal class ItemMysteriousEgg : ScriptObjectAutoAddDBBound, IItemOnExpire
{
    public ItemMysteriousEgg() : base("item_mysterious_egg") { }

    public bool OnExpire(Player player, ItemTemplate pItemProto)
    {
        List<ItemPosCount> dest = new();
        var msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, 39883, 1); // Cracked Egg

        if (msg == InventoryResult.Ok)
            player.StoreNewItem(dest, 39883, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(39883));

        return true;
    }
}

[Script]
internal class ItemDisgustingJar : ScriptObjectAutoAddDBBound, IItemOnExpire
{
    public ItemDisgustingJar() : base("item_disgusting_jar") { }

    public bool OnExpire(Player player, ItemTemplate pItemProto)
    {
        List<ItemPosCount> dest = new();
        var msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, 44718, 1); // Ripe Disgusting Jar

        if (msg == InventoryResult.Ok)
            player.StoreNewItem(dest, 44718, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(44718));

        return true;
    }
}

[Script]
internal class ItemPetrovClusterBombs : ScriptObjectAutoAddDBBound, IItemOnUse
{
    public ItemPetrovClusterBombs() : base("item_petrov_cluster_bombs") { }

    public bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
    {
        if (player.Zone != Misc.ZONE_ID_HOWLING)
            return false;

        if (player.Transport == null ||
            player.Area != Misc.AREA_ID_SHATTERED_STRAITS)
        {
            var spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.PETROV_BOMB, Difficulty.None);

            if (spellInfo != null)
                Spell.SendCastResult(player, spellInfo, default, castId, SpellCastResult.NotHere);

            return true;
        }

        return false;
    }
}

[Script]
internal class ItemCapturedFrog : ScriptObjectAutoAddDBBound, IItemOnUse
{
    public ItemCapturedFrog() : base("item_captured_frog") { }

    public bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
    {
        if (player.GetQuestStatus(QuestIds.THE_PERFECT_SPIES) == QuestStatus.Incomplete)
        {
            if (player.FindNearestCreature(CreatureIds.VANIRAS_SENTRY_TOTEM, 10.0f))
                return false;
            else
                player.SendEquipError(InventoryResult.OutOfRange, item, null);
        }
        else
            player.SendEquipError(InventoryResult.ClientLockedOut, item, null);

        return true;
    }
}

[Script] // Only used currently for
// 19169: Nightfall
internal class ItemGenericLimitChanceAbove60 : ScriptObjectAutoAddDBBound, IItemOnCastItemCombatSpell
{
    public ItemGenericLimitChanceAbove60() : base("item_generic_limit_chance_above_60") { }

    public bool OnCastItemCombatSpell(Player player, Unit victim, SpellInfo spellInfo, Item item)
    {
        // spell proc chance gets severely reduced on victims > 60 (formula unknown)
        if (victim.Level > 60)
        {
            // gives ~0.1% proc chance at lvl 70
            double lvlPenaltyFactor = 9.93f;
            var failureChance = (victim.GetLevelForTarget(player) - 60) * lvlPenaltyFactor;

            // base ppm chance was already rolled, only roll success chance
            return !RandomHelper.randChance(failureChance);
        }

        return true;
    }
}