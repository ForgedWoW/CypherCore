// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Entities.Items;
// TODO: Clean up properties that call other properties
public class ItemTemplate
{
    private readonly IConfiguration _configuration;
    private readonly DB6Storage<ItemArmorQualityRecord> _itemArmorQualityRecords;
    private readonly DB6Storage<ItemArmorTotalRecord> _itemArmorTotalRecords;
    private readonly DB6Storage<ArmorLocationRecord> _armorLocationRecords;
    private readonly DB6Storage<ItemArmorShieldRecord> _itemArmorShieldRecords;
    private readonly DB6Storage<ItemDamageAmmoRecord> _itemDamageAmmoRecords;
    private readonly DB6Storage<ItemDamageTwoHandCasterRecord> _itemDamageTwoHandCasterRecords;
    private readonly DB6Storage<ItemDamageTwoHandRecord> _itemDamageTwoHandRecords;
    private readonly DB6Storage<ItemDamageOneHandCasterRecord> _itemDamageOneHandCasterRecords;
    private readonly DB6Storage<ItemDamageOneHandRecord> _itemDamageOneHandRecords;
    private readonly DB6Storage<ChrSpecializationRecord> _chrSpecializationRecords;

    private static readonly SkillType[] ItemArmorSkills =
    {
        0, SkillType.Cloth, SkillType.Leather, SkillType.Mail, SkillType.PlateMail, 0, SkillType.Shield, 0, 0, 0, 0, 0
    };

    private static readonly SkillType[] ItemProfessionSkills =
    {
        SkillType.Blacksmithing, SkillType.Leatherworking, SkillType.Alchemy, SkillType.Herbalism, SkillType.Cooking, SkillType.Mining, SkillType.Tailoring, SkillType.Engineering, SkillType.Enchanting, SkillType.Fishing, SkillType.Skinning, SkillType.Jewelcrafting, SkillType.Inscription, SkillType.Archaeology
    };

    private static readonly SkillType[] ItemWeaponSkills =
    {
        SkillType.Axes, SkillType.TwoHandedAxes, SkillType.Bows, SkillType.Guns, SkillType.Maces, SkillType.TwoHandedMaces, SkillType.Polearms, SkillType.Swords, SkillType.TwoHandedSwords, SkillType.Warglaives, SkillType.Staves, 0, 0, SkillType.FistWeapons, 0, SkillType.Daggers, 0, 0, SkillType.Crossbows, SkillType.Wands, SkillType.ClassicFishing
    };

    private readonly SkillType[] _itemProfessionSkills =
    {
        SkillType.Blacksmithing, SkillType.Leatherworking, SkillType.Alchemy, SkillType.Herbalism, SkillType.Cooking, SkillType.ClassicBlacksmithing, SkillType.ClassicLeatherworking, SkillType.ClassicAlchemy, SkillType.ClassicHerbalism, SkillType.ClassicCooking, SkillType.Mining, SkillType.Tailoring, SkillType.Engineering, SkillType.Enchanting, SkillType.Fishing, SkillType.ClassicMining, SkillType.ClassicTailoring, SkillType.ClassicEngineering, SkillType.ClassicEnchanting, SkillType.ClassicFishing, SkillType.Skinning, SkillType.Jewelcrafting, SkillType.Inscription, SkillType.Archaeology, SkillType.ClassicSkinning, SkillType.ClassicJewelcrafting, SkillType.ClassicInscription
    };

    public ItemTemplate(ItemRecord item, ItemSparseRecord sparse, IConfiguration configuration, DB6Storage<ItemArmorQualityRecord> itemArmorQualityRecords,
                        DB6Storage<ItemArmorTotalRecord> itemArmorTotalRecords, DB6Storage<ArmorLocationRecord> armorLocationRecords, DB6Storage<ItemArmorShieldRecord> itemArmorShieldRecords,
                        DB6Storage<ItemDamageAmmoRecord> itemDamageAmmoRecords, DB6Storage<ItemDamageTwoHandCasterRecord> itemDamageTwoHandCasterRecords, DB6Storage<ItemDamageTwoHandRecord> itemDamageTwoHandRecords,
                        DB6Storage<ItemDamageOneHandCasterRecord> itemDamageOneHandCasterRecords, DB6Storage<ItemDamageOneHandRecord> itemDamageOneHandRecords, DB6Storage<ChrSpecializationRecord> chrSpecializationRecords)
    {
        _configuration = configuration;
        _itemArmorQualityRecords = itemArmorQualityRecords;
        _itemArmorTotalRecords = itemArmorTotalRecords;
        _armorLocationRecords = armorLocationRecords;
        _itemArmorShieldRecords = itemArmorShieldRecords;
        _itemDamageAmmoRecords = itemDamageAmmoRecords;
        _itemDamageTwoHandCasterRecords = itemDamageTwoHandCasterRecords;
        _itemDamageTwoHandRecords = itemDamageTwoHandRecords;
        _itemDamageOneHandCasterRecords = itemDamageOneHandCasterRecords;
        _itemDamageOneHandRecords = itemDamageOneHandRecords;
        _chrSpecializationRecords = chrSpecializationRecords;
        BasicData = item;
        ExtendedData = sparse;

        Specializations[0] = new BitSet((int)PlayerClass.Max * PlayerConst.MaxSpecializations);
        Specializations[1] = new BitSet((int)PlayerClass.Max * PlayerConst.MaxSpecializations);
        Specializations[2] = new BitSet((int)PlayerClass.Max * PlayerConst.MaxSpecializations);
    }

    public int AllowableClass => ExtendedData.AllowableClass;
    public long AllowableRace => ExtendedData.AllowableRace;
    public byte ArtifactID => ExtendedData.ArtifactID;
    public BagFamilyMask BagFamily => (BagFamilyMask)ExtendedData.BagFamily;
    public uint BaseItemLevel => ExtendedData.ItemLevel;
    public int BaseRequiredLevel => ExtendedData.RequiredLevel;
    public ItemBondingType Bonding => (ItemBondingType)ExtendedData.Bonding;
    public uint BuyCount => Math.Max(ExtendedData.VendorStackCount, 1u);
    public uint BuyPrice => ExtendedData.BuyPrice;
    public ItemClass Class => BasicData.ClassID;
    public uint ContainerSlots => ExtendedData.ContainerSlots;
    public uint DamageType => ExtendedData.DamageType;
    public uint Delay => ExtendedData.ItemDelay;
    public float DmgVariance => ExtendedData.DmgVariance;
    public uint Duration => ExtendedData.DurationInInventory;
    public List<ItemEffectRecord> Effects { get; set; } = new();
    public ItemFlagsCustom FlagsCu { get; set; }
    public uint FoodType { get; set; }
    public uint GemProperties => ExtendedData.GemProperties;

    public bool HasSignature => MaxStackSize == 1 &&
                                Class != ItemClass.Consumable &&
                                Class != ItemClass.Quest &&
                                !HasFlag(ItemFlags.NoCreator) &&
                                Id != 6948;

    public HolidayIds HolidayID => (HolidayIds)ExtendedData.RequiredHoliday;
    public uint Id => BasicData.Id;
    public InventoryType InventoryType => ExtendedData.inventoryType;
    public bool IsArmor => Class == ItemClass.Armor;
    public bool IsConjuredConsumable => Class == ItemClass.Consumable && HasFlag(ItemFlags.Conjured);
    public bool IsCraftingReagent => HasFlag(ItemFlags2.UsedInATradeskill);
    public bool IsCurrencyToken => (BagFamily & BagFamilyMask.CurrencyTokens) != 0;
    public bool IsPotion => Class == ItemClass.Consumable && SubClass == (uint)ItemSubClassConsumable.Potion;

    public bool IsRangedWeapon => IsWeapon &&
                                  SubClass is (uint)ItemSubClassWeapon.Bow or (uint)ItemSubClassWeapon.Gun or (uint)ItemSubClassWeapon.Crossbow;

    public bool IsVellum => HasFlag(ItemFlags3.CanStoreEnchants);
    public bool IsWeapon => Class == ItemClass.Weapon;
    public uint ItemLimitCategory => ExtendedData.LimitCategory;
    public uint ItemSet => ExtendedData.ItemSet;
    public uint ItemSpecClassMask { get; set; }
    public uint LockID => ExtendedData.LockID;
    public uint Map => ExtendedData.InstanceBound;
    public uint MaxCount => ExtendedData.MaxCount;
    public uint MaxDurability { get; set; }
    public uint MaxMoneyLoot { get; set; }

    public uint MaxStackSize => ExtendedData.Stackable is 2147483647 or <= 0 ? 0x7FFFFFFF - 1 : ExtendedData.Stackable;

    public uint MinMoneyLoot { get; set; }

    public uint OtherFactionItemId => ExtendedData.FactionRelated;

    public uint PageText => ExtendedData.PageID;

    public uint PlayerLevelToItemLevelCurveId => ExtendedData.PlayerLevelToItemLevelCurveID;

    public float PriceVariance => ExtendedData.PriceVariance;

    public ItemQuality Quality => (ItemQuality)ExtendedData.OverallQualityID;

    public uint RandomBonusListTemplateId { get; set; }

    public float RangedModRange => ExtendedData.ItemRange;

    public byte RequiredExpansion => (byte)ExtendedData.ExpansionID;

    public uint RequiredReputationFaction => ExtendedData.MinFactionID;

    public uint RequiredReputationRank => ExtendedData.MinReputation;

    public uint RequiredSkill => ExtendedData.RequiredSkill;

    public uint RequiredSkillRank => ExtendedData.RequiredSkillRank;

    public uint RequiredSpell => ExtendedData.RequiredAbility;

    public uint ScalingStatContentTuning => ExtendedData.ContentTuningID;

    // extra fields, not part of db2 files
    public uint ScriptId { get; set; }
    public uint SellPrice => ExtendedData.SellPrice;
    public uint SocketBonus => ExtendedData.SocketMatchEnchantmentId;
    public BitSet[] Specializations { get; set; } = new BitSet[3];
    public float SpellPPMRate { get; set; }
    public uint StartQuest => ExtendedData.StartQuestID;
    public uint SubClass => BasicData.SubclassID;
    public uint TotemCategory => ExtendedData.TotemCategoryID;
    public ItemRecord BasicData { get; set; }
    public ItemSparseRecord ExtendedData { get; set; }

    /*Hearthstone*/
    public static int CalculateItemSpecBit(ChrSpecializationRecord spec)
    {
        return (spec.ClassID - 1) * PlayerConst.MaxSpecializations + spec.OrderIndex;
    }

    public bool CanChangeEquipStateInCombat()
    {
        return InventoryType switch
        {
            InventoryType.Relic    => true,
            InventoryType.Shield   => true,
            InventoryType.Holdable => true,
            _ => Class switch
            {
                ItemClass.Weapon     => true,
                ItemClass.Projectile => true,
                _                    => false
            }
        };
    }

    public uint GetArea(int index)
    {
        return ExtendedData.ZoneBound[index];
    }

    public uint GetArmor(uint itemLevel)
    {
        var quality = Quality != ItemQuality.Heirloom ? Quality : ItemQuality.Rare;

        if (quality > ItemQuality.Artifact)
            return 0;

        // all items but shields
        if (Class != ItemClass.Armor || SubClass != (uint)ItemSubClassArmor.Shield)
        {
            var armorQuality = _itemArmorQualityRecords.LookupByKey(itemLevel);

            if (!_itemArmorTotalRecords.TryGetValue(itemLevel, out var armorTotal))
                return 0;

            var inventoryType = InventoryType;

            if (inventoryType == InventoryType.Robe)
                inventoryType = InventoryType.Chest;

            if (!_armorLocationRecords.TryGetValue((uint)inventoryType, out var location))
                return 0;

            if (SubClass is < (uint)ItemSubClassArmor.Cloth or > (uint)ItemSubClassArmor.Plate)
                return 0;

            var total = 1.0f;
            var locationModifier = 1.0f;

            switch ((ItemSubClassArmor)SubClass)
            {
                case ItemSubClassArmor.Cloth:
                    total = armorTotal.Cloth;
                    locationModifier = location.Clothmodifier;

                    break;
                case ItemSubClassArmor.Leather:
                    total = armorTotal.Leather;
                    locationModifier = location.Leathermodifier;

                    break;
                case ItemSubClassArmor.Mail:
                    total = armorTotal.Mail;
                    locationModifier = location.Chainmodifier;

                    break;
                case ItemSubClassArmor.Plate:
                    total = armorTotal.Plate;
                    locationModifier = location.Platemodifier;

                    break;
            }

            return (uint)(armorQuality.QualityMod[(int)quality] * total * locationModifier + 0.5f);
        }

        // shields
        if (!_itemArmorShieldRecords.TryGetValue(itemLevel, out var shield))
            return 0;

        return (uint)(shield.Quality[(int)quality] + 0.5f);
    }

    public void GetDamage(uint itemLevel, out float minDamage, out float maxDamage)
    {
        minDamage = maxDamage = 0.0f;
        var dps = GetDps(itemLevel);

        if (!(dps > 0.0f))
            return;

        var avgDamage = dps * Delay * 0.001f;
        minDamage = (DmgVariance * -0.5f + 1.0f) * avgDamage;
        maxDamage = (float)Math.Floor(avgDamage * (DmgVariance * 0.5f + 1.0f) + 0.5f);
    }

    public float GetDps(uint itemLevel)
    {
        var quality = Quality != ItemQuality.Heirloom ? Quality : ItemQuality.Rare;

        if (Class != ItemClass.Weapon || quality > ItemQuality.Artifact)
            return 0.0f;

        var dps = 0.0f;

        switch (InventoryType)
        {
            case InventoryType.Ammo:
                dps = _itemDamageAmmoRecords.LookupByKey(itemLevel).Quality[(int)quality];

                break;
            case InventoryType.Weapon2Hand:
                dps = HasFlag(ItemFlags2.CasterWeapon) ? _itemDamageTwoHandCasterRecords.LookupByKey(itemLevel).Quality[(int)quality] : _itemDamageTwoHandRecords.LookupByKey(itemLevel).Quality[(int)quality];

                break;
            case InventoryType.Ranged:
            case InventoryType.Thrown:
            case InventoryType.RangedRight:
                switch ((ItemSubClassWeapon)SubClass)
                {
                    case ItemSubClassWeapon.Wand:
                        dps = _itemDamageOneHandCasterRecords.LookupByKey(itemLevel).Quality[(int)quality];

                        break;
                    case ItemSubClassWeapon.Bow:
                    case ItemSubClassWeapon.Gun:
                    case ItemSubClassWeapon.Crossbow:
                        dps = HasFlag(ItemFlags2.CasterWeapon) ? _itemDamageTwoHandCasterRecords.LookupByKey(itemLevel).Quality[(int)quality] : _itemDamageTwoHandRecords.LookupByKey(itemLevel).Quality[(int)quality];

                        break;
                }

                break;
            case InventoryType.Weapon:
            case InventoryType.WeaponMainhand:
            case InventoryType.WeaponOffhand:
                dps = HasFlag(ItemFlags2.CasterWeapon) ? _itemDamageOneHandCasterRecords.LookupByKey(itemLevel).Quality[(int)quality] : _itemDamageOneHandRecords.LookupByKey(itemLevel).Quality[(int)quality];

                break;
        }

        return dps;
    }

    public string GetName(Locale locale = SharedConst.DefaultLocale)
    {
        return ExtendedData.Display[locale];
    }

    public SkillType GetSkill()
    {
        switch (Class)
        {
            case ItemClass.Weapon:
                return SubClass >= (int)ItemSubClassWeapon.Max ? 0 : ItemWeaponSkills[SubClass];

            case ItemClass.Armor:
                return SubClass >= (int)ItemSubClassArmor.Max ? 0 : ItemArmorSkills[SubClass];

            case ItemClass.Profession:

                if (_configuration.GetDefaultValue("Professions:AllowClassicProfessionSlots", false))
                    return SubClass >= (int)ItemSubclassProfession.Max ? 0 : _itemProfessionSkills[SubClass];

                return SubClass >= (int)ItemSubclassProfession.Max ? 0 : ItemProfessionSkills[SubClass];

            default:
                return 0;
        }
    }

    public SocketColor GetSocketColor(uint index)
    {
        return (SocketColor)ExtendedData.SocketType[index];
    }

    public bool HasFlag(ItemFlags flag)
    {
        return (ExtendedData.Flags[0] & (int)flag) != 0;
    }

    public bool HasFlag(ItemFlags2 flag)
    {
        return (ExtendedData.Flags[1] & (int)flag) != 0;
    }

    public bool HasFlag(ItemFlags3 flag)
    {
        return (ExtendedData.Flags[2] & (int)flag) != 0;
    }

    public bool HasFlag(ItemFlags4 flag)
    {
        return (ExtendedData.Flags[3] & (int)flag) != 0;
    }

    public bool HasFlag(ItemFlagsCustom customFlag)
    {
        return (FlagsCu & customFlag) != 0;
    }

    public bool IsUsableByLootSpecialization(Player player, bool alwaysAllowBoundToAccount)
    {
        if (HasFlag(ItemFlags.IsBoundToAccount) && alwaysAllowBoundToAccount)
            return true;

        var spec = player.GetLootSpecId();

        if (spec == 0)
            spec = player.GetPrimarySpecialization();

        if (spec == 0)
            spec = player.GetDefaultSpecId();

        if (!_chrSpecializationRecords.TryGetValue(spec, out var chrSpecialization))
            return false;

        var levelIndex = player.Level switch
        {
            >= 110 => 2,
            > 40   => 1,
            _      => 0
        };

        return Specializations[levelIndex].Get(CalculateItemSpecBit(chrSpecialization));
    }
}