// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Dynamic;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Spells;

public class SpellInfo
{
    public uint ChargeCategoryId;
    public List<uint> Labels = new();
    public SpellPowerRecord[] PowerCosts = new SpellPowerRecord[SpellConst.MaxPowersPerSpell];
    public int[] Reagent = new int[SpellConst.MaxReagents];
    public uint[] ReagentCount = new uint[SpellConst.MaxReagents];

    public List<SpellReagentsCurrencyRecord> ReagentsCurrency = new();

    // SpellScalingEntry
    public ScalingInfo Scaling;

    public uint[] Totem = new uint[SpellConst.MaxTotems];
    public uint[] TotemCategory = new uint[SpellConst.MaxTotems];
    private readonly CliDB _cliDB;
    private readonly ConditionManager _conditionManager;
    private readonly DB2Manager _db2Manager;
    private readonly ItemEnchantmentManager _itemEnchantmentManager;
    private readonly LootStoreBox _lootStoreBox;
    private readonly ObjectAccessor _objectAccessor;
    private readonly List<SpellProcsPerMinuteModRecord> _procPpmMods = new();
    private readonly SpellManager _spellManager;
    private SpellDiminishInfo _diminishInfo;

    public SpellInfo(SpellNameRecord spellName, Difficulty difficulty, SpellInfoLoadHelper data, ClassFactory classFactory, CliDB cliDB, ItemEnchantmentManager itemEnchantmentManager,
                     DB2Manager db2Manager, SpellManager spellManager, LootStoreBox lootStoreBox, ObjectAccessor objectAccessor, ConditionManager conditionManager)
    {
        _cliDB = cliDB;
        _itemEnchantmentManager = itemEnchantmentManager;
        _db2Manager = db2Manager;
        _spellManager = spellManager;
        _lootStoreBox = lootStoreBox;
        _objectAccessor = objectAccessor;
        _conditionManager = conditionManager;
        Id = spellName.Id;
        Difficulty = difficulty;

        foreach (var spellEffect in data.Effects)
        {
            Effects.EnsureWritableListIndex(spellEffect.Key, classFactory.ResolveWithPositionalParameters<SpellEffectInfo>(this, null));
            Effects[spellEffect.Key] = classFactory.ResolveWithPositionalParameters<SpellEffectInfo>(this, spellEffect.Value);
        }

        // Correct EffectIndex for blank effects
        for (var i = 0; i < Effects.Count; ++i)
            Effects[i].EffectIndex = i;

        NegativeEffects = new HashSet<int>();

        SpellName = spellName.Name;

        var misc = data.Misc;

        if (misc != null)
        {
            Attributes = (SpellAttr0)misc.Attributes[0];
            AttributesEx = (SpellAttr1)misc.Attributes[1];
            AttributesEx2 = (SpellAttr2)misc.Attributes[2];
            AttributesEx3 = (SpellAttr3)misc.Attributes[3];
            AttributesEx4 = (SpellAttr4)misc.Attributes[4];
            AttributesEx5 = (SpellAttr5)misc.Attributes[5];
            AttributesEx6 = (SpellAttr6)misc.Attributes[6];
            AttributesEx7 = (SpellAttr7)misc.Attributes[7];
            AttributesEx8 = (SpellAttr8)misc.Attributes[8];
            AttributesEx9 = (SpellAttr9)misc.Attributes[9];
            AttributesEx10 = (SpellAttr10)misc.Attributes[10];
            AttributesEx11 = (SpellAttr11)misc.Attributes[11];
            AttributesEx12 = (SpellAttr12)misc.Attributes[12];
            AttributesEx13 = (SpellAttr13)misc.Attributes[13];
            AttributesEx14 = (SpellAttr14)misc.Attributes[14];
            CastTimeEntry = _cliDB.SpellCastTimesStorage.LookupByKey(misc.CastingTimeIndex);
            DurationEntry = _cliDB.SpellDurationStorage.LookupByKey(misc.DurationIndex);
            RangeEntry = _cliDB.SpellRangeStorage.LookupByKey(misc.RangeIndex);
            Speed = misc.Speed;
            LaunchDelay = misc.LaunchDelay;
            SchoolMask = (SpellSchoolMask)misc.SchoolMask;
            IconFileDataId = misc.SpellIconFileDataID;
            ActiveIconFileDataId = misc.ActiveIconFileDataID;
            ContentTuningId = misc.ContentTuningID;
            ShowFutureSpellPlayerConditionId = (uint)misc.ShowFutureSpellPlayerConditionID;
        }

        // SpellScalingEntry
        var scaling = data.Scaling;

        if (scaling != null)
        {
            Scaling.MinScalingLevel = scaling.MinScalingLevel;
            Scaling.MaxScalingLevel = scaling.MaxScalingLevel;
            Scaling.ScalesFromItemLevel = scaling.ScalesFromItemLevel;
        }

        // SpellAuraOptionsEntry
        var options = data.AuraOptions;

        if (options != null)
        {
            ProcFlags = new ProcFlagsInit(options.ProcTypeMask);
            ProcChance = options.ProcChance;
            ProcCharges = (uint)options.ProcCharges;
            ProcCooldown = options.ProcCategoryRecovery;
            StackAmount = options.CumulativeAura;

            if (_cliDB.SpellProcsPerMinuteStorage.TryGetValue(options.SpellProcsPerMinuteID, out var ppm))
            {
                ProcBasePpm = ppm.BaseProcRate;
                _procPpmMods = _db2Manager.GetSpellProcsPerMinuteMods(ppm.Id);
            }
        }

        // SpellAuraRestrictionsEntry
        var aura = data.AuraRestrictions;

        if (aura != null)
        {
            CasterAuraState = (AuraStateType)aura.CasterAuraState;
            TargetAuraState = (AuraStateType)aura.TargetAuraState;
            ExcludeCasterAuraState = (AuraStateType)aura.ExcludeCasterAuraState;
            ExcludeTargetAuraState = (AuraStateType)aura.ExcludeTargetAuraState;
            CasterAuraSpell = aura.CasterAuraSpell;
            TargetAuraSpell = aura.TargetAuraSpell;
            ExcludeCasterAuraSpell = aura.ExcludeCasterAuraSpell;
            ExcludeTargetAuraSpell = aura.ExcludeTargetAuraSpell;
            CasterAuraType = (AuraType)aura.CasterAuraType;
            TargetAuraType = (AuraType)aura.TargetAuraType;
            ExcludeCasterAuraType = (AuraType)aura.ExcludeCasterAuraType;
            ExcludeTargetAuraType = (AuraType)aura.ExcludeTargetAuraType;
        }

        RequiredAreasId = -1;
        // SpellCastingRequirementsEntry
        var castreq = data.CastingRequirements;

        if (castreq != null)
        {
            RequiresSpellFocus = castreq.RequiresSpellFocus;
            FacingCasterFlags = castreq.FacingCasterFlags;
            RequiredAreasId = castreq.RequiredAreasID;
        }

        // SpellCategoriesEntry
        var categories = data.Categories;

        if (categories != null)
        {
            CategoryId = categories.Category;
            Dispel = (DispelType)categories.DispelType;
            Mechanic = (Mechanics)categories.Mechanic;
            StartRecoveryCategory = categories.StartRecoveryCategory;
            DmgClass = (SpellDmgClass)categories.DefenseType;
            PreventionType = (SpellPreventionType)categories.PreventionType;
            ChargeCategoryId = categories.ChargeCategory;
        }

        // SpellClassOptionsEntry
        SpellFamilyFlags = new FlagArray128();
        var classOptions = data.ClassOptions;

        if (classOptions != null)
        {
            SpellFamilyName = (SpellFamilyNames)classOptions.SpellClassSet;
            SpellFamilyFlags = classOptions.SpellClassMask;
        }

        // SpellCooldownsEntry
        var cooldowns = data.Cooldowns;

        if (cooldowns != null)
        {
            RecoveryTime = cooldowns.RecoveryTime;
            CategoryRecoveryTime = cooldowns.CategoryRecoveryTime;
            StartRecoveryTime = cooldowns.StartRecoveryTime;
            CooldownAuraSpellId = cooldowns.AuraSpellID;
        }

        EquippedItemClass = ItemClass.None;
        EquippedItemSubClassMask = 0;
        EquippedItemInventoryTypeMask = 0;
        // SpellEquippedItemsEntry
        var equipped = data.EquippedItems;

        if (equipped != null)
        {
            EquippedItemClass = (ItemClass)equipped.EquippedItemClass;
            EquippedItemSubClassMask = equipped.EquippedItemSubclass;
            EquippedItemInventoryTypeMask = equipped.EquippedItemInvTypes;
        }

        // SpellInterruptsEntry
        var interrupt = data.Interrupts;

        if (interrupt != null)
        {
            InterruptFlags = (SpellInterruptFlags)interrupt.InterruptFlags;
            AuraInterruptFlags = (SpellAuraInterruptFlags)interrupt.AuraInterruptFlags[0];
            AuraInterruptFlags2 = (SpellAuraInterruptFlags2)interrupt.AuraInterruptFlags[1];
            ChannelInterruptFlags = (SpellAuraInterruptFlags)interrupt.ChannelInterruptFlags[0];
            ChannelInterruptFlags2 = (SpellAuraInterruptFlags2)interrupt.ChannelInterruptFlags[1];
        }

        foreach (var label in data.Labels)
            Labels.Add(label.LabelID);

        // SpellLevelsEntry
        var levels = data.Levels;

        if (levels != null)
        {
            MaxLevel = levels.MaxLevel;
            BaseLevel = levels.BaseLevel;
            SpellLevel = levels.SpellLevel;
        }

        // SpellPowerEntry
        PowerCosts = data.Powers;

        // SpellReagentsEntry
        var reagents = data.Reagents;

        if (reagents != null)
            for (var i = 0; i < SpellConst.MaxReagents; ++i)
            {
                Reagent[i] = reagents.Reagent[i];
                ReagentCount[i] = reagents.ReagentCount[i];
            }

        ReagentsCurrency = data.ReagentsCurrency;

        // SpellShapeshiftEntry
        var shapeshift = data.Shapeshift;

        if (shapeshift != null)
        {
            Stances = MathFunctions.MakePair64(shapeshift.ShapeshiftMask[0], shapeshift.ShapeshiftMask[1]);
            StancesNot = MathFunctions.MakePair64(shapeshift.ShapeshiftExclude[0], shapeshift.ShapeshiftExclude[1]);
        }

        // SpellTargetRestrictionsEntry
        var target = data.TargetRestrictions;

        if (target != null)
        {
            Targets = (SpellCastTargetFlags)target.Targets;
            ConeAngle = target.ConeDegrees;
            Width = target.Width;
            TargetCreatureType = target.TargetCreatureType;
            MaxAffectedTargets = target.MaxTargets;
            MaxTargetLevel = target.MaxTargetLevel;
        }

        // SpellTotemsEntry
        var totem = data.Totems;

        if (totem != null)
            for (var i = 0; i < 2; ++i)
            {
                TotemCategory[i] = totem.RequiredTotemCategoryID[i];
                Totem[i] = totem.Totem[i];
            }

        SpellVisuals = data.Visuals;

        SpellSpecific = SpellSpecificType.Normal;
        AuraState = AuraStateType.None;

        EmpowerStages = data.EmpowerStages.ToDictionary(a => a.Stage);
    }

    public SpellInfo(SpellNameRecord spellName, Difficulty difficulty, List<SpellEffectRecord> effects, ClassFactory classFactory, CliDB cliDB, ItemEnchantmentManager itemEnchantmentManager,
                     DB2Manager db2Manager, SpellManager spellManager, LootStoreBox lootStoreBox, ObjectAccessor objectAccessor, ConditionManager conditionManager)
    {
        _cliDB = cliDB;
        _itemEnchantmentManager = itemEnchantmentManager;
        _db2Manager = db2Manager;
        _spellManager = spellManager;
        _lootStoreBox = lootStoreBox;
        _objectAccessor = objectAccessor;
        _conditionManager = conditionManager;
        Id = spellName.Id;
        Difficulty = difficulty;
        SpellName = spellName.Name;

        foreach (var spellEffect in effects)
        {
            Effects.EnsureWritableListIndex(spellEffect.EffectIndex, classFactory.ResolveWithPositionalParameters<SpellEffectInfo>(this, null));
            Effects[spellEffect.EffectIndex] = classFactory.ResolveWithPositionalParameters<SpellEffectInfo>(this, spellEffect);
        }

        // Correct EffectIndex for blank effects
        for (var i = 0; i < Effects.Count; ++i)
            Effects[i].EffectIndex = i;

        NegativeEffects = new HashSet<int>();
    }

    public uint ActiveIconFileDataId { get; set; }
    public ulong AllowedMechanicMask { get; private set; }
    public SpellAttr0 Attributes { get; set; }
    public SpellCustomAttributes AttributesCu { get; set; }
    public SpellAttr1 AttributesEx { get; set; }
    public SpellAttr10 AttributesEx10 { get; set; }
    public SpellAttr11 AttributesEx11 { get; set; }
    public SpellAttr12 AttributesEx12 { get; set; }
    public SpellAttr13 AttributesEx13 { get; set; }
    public SpellAttr14 AttributesEx14 { get; set; }
    public SpellAttr2 AttributesEx2 { get; set; }
    public SpellAttr3 AttributesEx3 { get; set; }
    public SpellAttr4 AttributesEx4 { get; set; }
    public SpellAttr5 AttributesEx5 { get; set; }
    public SpellAttr6 AttributesEx6 { get; set; }
    public SpellAttr7 AttributesEx7 { get; set; }
    public SpellAttr8 AttributesEx8 { get; set; }
    public SpellAttr9 AttributesEx9 { get; set; }
    public SpellAuraInterruptFlags AuraInterruptFlags { get; set; }
    public SpellAuraInterruptFlags2 AuraInterruptFlags2 { get; set; }
    public AuraStateType AuraState { get; private set; }
    public uint BaseLevel { get; set; }
    public bool CanBeUsedInCombat => !HasAttribute(SpellAttr0.NotInCombatOnlyPeaceful);
    public uint CasterAuraSpell { get; set; }
    public AuraStateType CasterAuraState { get; set; }
    public AuraType CasterAuraType { get; set; }
    public SpellCastTimesRecord CastTimeEntry { get; set; }
    public uint Category => CategoryId;

    public uint CategoryId { get; set; }
    public uint CategoryRecoveryTime { get; set; }
    public SpellChainNode ChainEntry { get; set; }
    public SpellAuraInterruptFlags ChannelInterruptFlags { get; set; }
    public SpellAuraInterruptFlags2 ChannelInterruptFlags2 { get; set; }
    public float ConeAngle { get; set; }
    public uint ContentTuningId { get; set; }
    public uint CooldownAuraSpellId { get; set; }
    public Difficulty Difficulty { get; set; }
    public DiminishingGroup DiminishingReturnsGroupForSpell => _diminishInfo.DiminishGroup;
    public DiminishingReturnsType DiminishingReturnsGroupType => _diminishInfo.DiminishReturnType;
    public int DiminishingReturnsLimitDuration => _diminishInfo.DiminishDurationLimit;
    public DiminishingLevels DiminishingReturnsMaxLevel => _diminishInfo.DiminishMaxLevel;
    public DispelType Dispel { get; set; }
    public uint DispelMask => GetDispelMask(Dispel);
    public SpellDmgClass DmgClass { get; set; }

    public int Duration
    {
        get
        {
            if (DurationEntry == null)
                return IsPassive ? -1 : 0;

            return DurationEntry.Duration == -1 ? -1 : Math.Abs(DurationEntry.Duration);
        }
    }

    public SpellDurationRecord DurationEntry { get; set; }
    public List<SpellEffectInfo> Effects { get; } = new();

    public Dictionary<byte, SpellEmpowerStageRecord> EmpowerStages { get; set; } = new();
    public ItemClass EquippedItemClass { get; set; }
    public int EquippedItemInventoryTypeMask { get; set; }
    public int EquippedItemSubClassMask { get; set; }
    public uint ExcludeCasterAuraSpell { get; set; }
    public AuraStateType ExcludeCasterAuraState { get; set; }
    public AuraType ExcludeCasterAuraType { get; set; }
    public uint ExcludeTargetAuraSpell { get; set; }
    public AuraStateType ExcludeTargetAuraState { get; set; }
    public AuraType ExcludeTargetAuraType { get; set; }
    public SpellCastTargetFlags ExplicitTargetMask { get; set; }
    public uint FacingCasterFlags { get; set; }
    public SpellInfo FirstRankSpell => ChainEntry == null ? this : ChainEntry.First;
    public bool HasAnyAuraInterruptFlag => AuraInterruptFlags != SpellAuraInterruptFlags.None || AuraInterruptFlags2 != SpellAuraInterruptFlags2.None;

    public bool HasAreaAuraEffect
    {
        get
        {
            foreach (var effectInfo in Effects)
                if (effectInfo.IsAreaAuraEffect)
                    return true;

            return false;
        }
    }

    public bool HasHitDelay => Speed > 0.0f || LaunchDelay > 0.0f;
    public bool HasInitialAggro => !(HasAttribute(SpellAttr1.NoThreat) || HasAttribute(SpellAttr2.NoInitialThreat) || HasAttribute(SpellAttr4.NoHarmfulThreat));

    public bool HasOnlyDamageEffects
    {
        get
        {
            foreach (var effectInfo in Effects)
                switch (effectInfo.Effect)
                {
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoSchool:
                    case SpellEffectName.NormalizedWeaponDmg:
                    case SpellEffectName.WeaponPercentDamage:
                    case SpellEffectName.SchoolDamage:
                    case SpellEffectName.EnvironmentalDamage:
                    case SpellEffectName.HealthLeech:
                    case SpellEffectName.DamageFromMaxHealthPCT:
                        continue;
                    default:
                        return false;
                }

            return true;
        }
    }

    public uint IconFileDataId { get; set; }
    public uint Id { get; set; }
    public SpellInterruptFlags InterruptFlags { get; set; }

    public bool IsAffectingArea
    {
        get
        {
            foreach (var effectInfo in Effects)
                if (effectInfo.IsEffect && (effectInfo.IsTargetingArea || effectInfo.IsEffectName(SpellEffectName.PersistentAreaAura) || effectInfo.IsAreaAuraEffect))
                    return true;

            return false;
        }
    }

    public bool IsAllowingDeadTarget
    {
        get
        {
            if (HasAttribute(SpellAttr2.AllowDeadTarget) || Targets.HasAnyFlag(SpellCastTargetFlags.CorpseAlly | SpellCastTargetFlags.CorpseEnemy | SpellCastTargetFlags.UnitDead))
                return true;

            foreach (var effectInfo in Effects)
                if (effectInfo.TargetA.ObjectType == SpellTargetObjectTypes.Corpse || effectInfo.TargetB.ObjectType == SpellTargetObjectTypes.Corpse)
                    return true;

            return false;
        }
    }

    public bool IsAutocastable
    {
        get
        {
            if (IsPassive)
                return false;

            if (HasAttribute(SpellAttr1.NoAutocastAi))
                return false;

            return true;
        }
    }

    public bool IsAutoRepeatRangedSpell => HasAttribute(SpellAttr2.AutoRepeat);
    public bool IsChanneled => HasAttribute(SpellAttr1.IsChannelled | SpellAttr1.IsSelfChannelled);

    public bool IsCooldownStartedOnEvent
    {
        get
        {
            if (HasAttribute(SpellAttr0.CooldownOnEvent))
                return true;

            var category = _cliDB.SpellCategoryStorage.LookupByKey(CategoryId);

            return category != null && category.Flags.HasAnyFlag(SpellCategoryFlags.CooldownStartsOnEvent);
        }
    }

    public bool IsDeathPersistent => HasAttribute(SpellAttr3.AllowAuraWhileDead);

    public bool IsExplicitDiscovery
    {
        get
        {
            if (Effects.Count < 2)
                return false;

            return (GetEffect(0).Effect is SpellEffectName.CreateRandomItem or SpellEffectName.CreateLoot && GetEffect(1).Effect == SpellEffectName.ScriptEffect) || Id == 64323;
        }
    }

    public bool IsGroupBuff
    {
        get
        {
            foreach (var effectInfo in Effects)
                switch (effectInfo.TargetA.CheckType)
                {
                    case SpellTargetCheckTypes.Party:
                    case SpellTargetCheckTypes.Raid:
                    case SpellTargetCheckTypes.RaidClass:
                        return true;
                }

            return false;
        }
    }

    public bool IsLootCrafting => HasEffect(SpellEffectName.CreateRandomItem) || HasEffect(SpellEffectName.CreateLoot);
    public bool IsMoveAllowedChannel => IsChanneled && !ChannelInterruptFlags.HasFlag(SpellAuraInterruptFlags.Moving | SpellAuraInterruptFlags.Turning);
    public bool IsMultiSlotAura => IsPassive || Id is 55849 or 40075 or 44413;
    public bool IsNextMeleeSwingSpell => HasAttribute(SpellAttr0.OnNextSwingNoDamage | SpellAttr0.OnNextSwing);
    public bool IsPassive => HasAttribute(SpellAttr0.Passive);
    public bool IsPassiveStackableWithRanks => IsPassive && !HasEffect(SpellEffectName.ApplyAura);
    public bool IsPositive => NegativeEffects.Count == 0;

    public bool IsPrimaryProfession
    {
        get
        {
            return Effects.Any(effectInfo => effectInfo.IsEffectName(SpellEffectName.Skill) && _spellManager.IsPrimaryProfessionSkill((uint)effectInfo.MiscValue));
        }
    }

    public bool IsPrimaryProfessionFirstRank => IsPrimaryProfession && Rank == 1;

    public bool IsProfession
    {
        get
        {
            foreach (var effectInfo in Effects)
                if (effectInfo.IsEffectName(SpellEffectName.Skill))
                {
                    var skill = (uint)effectInfo.MiscValue;

                    if (_spellManager.IsProfessionSkill(skill))
                        return true;
                }

            return false;
        }
    }

    public bool IsRangedWeaponSpell => (SpellFamilyName == SpellFamilyNames.Hunter && !SpellFamilyFlags[1].HasAnyFlag(0x10000000u)) // for 53352, cannot find better way
                                       ||
                                       Convert.ToBoolean(EquippedItemSubClassMask & (int)ItemSubClassWeapon.MaskRanged) ||
                                       Attributes.HasAnyFlag(SpellAttr0.UsesRangedSlot);

    public bool IsRanked => ChainEntry != null;
    public bool IsRequiringDeadTarget => HasAttribute(SpellAttr3.OnlyOnGhosts);

    public bool IsStackableOnOneSlotWithDifferentCasters =>
        // TODO: Re-verify meaning of SPELL_ATTR3_STACK_FOR_DIFF_CASTERS and update conditions here
        StackAmount > 1 && !IsChanneled && !HasAttribute(SpellAttr3.DotStackingRule);

    public bool IsStackableWithRanks
    {
        get
        {
            if (IsPassive)
                return false;

            // All stance spells. if any better way, change it.
            foreach (var effectInfo in Effects)
                switch (SpellFamilyName)
                {
                    case SpellFamilyNames.Paladin:
                        // Paladin aura Spell
                        if (effectInfo.Effect == SpellEffectName.ApplyAreaAuraRaid)
                            return false;

                        break;

                    case SpellFamilyNames.Druid:
                        // Druid form Spell
                        if (effectInfo.Effect == SpellEffectName.ApplyAura &&
                            effectInfo.ApplyAuraName == AuraType.ModShapeshift)
                            return false;

                        break;
                }

            return true;
        }
    }

    // checks if spell targets are selected from area, doesn't include spell effects in check (like area wide auras for example)
    public bool IsTargetingArea
    {
        get { return Effects.Any(effectInfo => effectInfo.IsEffect && effectInfo.IsTargetingArea); }
    }

    public float LaunchDelay { get; set; }
    public uint MaxAffectedTargets { get; set; }

    public int MaxDuration
    {
        get
        {
            if (DurationEntry == null)
                return IsPassive ? -1 : 0;

            return DurationEntry.MaxDuration == -1 ? -1 : Math.Abs(DurationEntry.MaxDuration);
        }
    }

    public uint MaxLevel { get; set; }
    public uint MaxTargetLevel { get; set; }

    public uint MaxTicks
    {
        get
        {
            uint totalTicks = 0;
            var dotDuration = Duration;

            foreach (var effectInfo in Effects)
            {
                if (!effectInfo.IsEffectName(SpellEffectName.ApplyAura))
                    continue;

                switch (effectInfo.ApplyAuraName)
                {
                    case AuraType.PeriodicDamage:
                    case AuraType.PeriodicDamagePercent:
                    case AuraType.PeriodicHeal:
                    case AuraType.ObsModHealth:
                    case AuraType.ObsModPower:
                    case AuraType.PeriodicTriggerSpellFromClient:
                    case AuraType.PowerBurn:
                    case AuraType.PeriodicLeech:
                    case AuraType.PeriodicManaLeech:
                    case AuraType.PeriodicEnergize:
                    case AuraType.PeriodicDummy:
                    case AuraType.PeriodicTriggerSpell:
                    case AuraType.PeriodicTriggerSpellWithValue:
                    case AuraType.PeriodicHealthFunnel:
                        // skip infinite periodics
                        if (effectInfo.ApplyAuraPeriod > 0 && dotDuration > 0)
                        {
                            totalTicks = (uint)dotDuration / effectInfo.ApplyAuraPeriod;

                            if (HasAttribute(SpellAttr5.ExtraInitialPeriod))
                                ++totalTicks;
                        }

                        break;
                }
            }

            return totalTicks;
        }
    }

    public Mechanics Mechanic { get; set; }

    // Power Spark, Fel Flak Fire, Incanter's Absorption
    public bool NeedsComboPoints => HasAttribute(SpellAttr1.FinishingMoveDamage | SpellAttr1.FinishingMoveDuration);

    public bool NeedsExplicitUnitTarget => Convert.ToBoolean(ExplicitTargetMask & SpellCastTargetFlags.UnitMask);
    public HashSet<int> NegativeEffects { get; set; }
    public SpellInfo NextRankSpell => ChainEntry?.Next;
    public SpellPreventionType PreventionType { get; set; }
    public float ProcBasePpm { get; set; }
    public uint ProcChance { get; set; }
    public uint ProcCharges { get; set; }
    public uint ProcCooldown { get; set; }
    public ProcFlagsInit ProcFlags { get; set; }
    public SpellRangeRecord RangeEntry { get; set; }

    public byte Rank => ChainEntry?.Rank ?? 1;

    public uint RecoveryTime { get; set; }
    public uint RecoveryTime1 => RecoveryTime > CategoryRecoveryTime ? RecoveryTime : CategoryRecoveryTime;
    public int RequiredAreasId { get; set; }
    public uint RequiresSpellFocus { get; set; }
    public SpellSchoolMask SchoolMask { get; set; }
    public uint ShowFutureSpellPlayerConditionId { get; set; }
    public float Speed { get; set; }
    public FlagArray128 SpellFamilyFlags { get; set; }
    public SpellFamilyNames SpellFamilyName { get; set; }
    public uint SpellLevel { get; set; }
    public LocalizedString SpellName { get; set; }
    public SpellSpecificType SpellSpecific { get; private set; }
    public List<SpellXSpellVisualRecord> SpellVisuals { get; } = new();
    public uint StackAmount { get; set; }
    public ulong Stances { get; set; }
    public ulong StancesNot { get; set; }
    public uint StartRecoveryCategory { get; set; }
    public uint StartRecoveryTime { get; set; }
    public uint TargetAuraSpell { get; set; }
    public AuraStateType TargetAuraState { get; set; }
    public AuraType TargetAuraType { get; set; }
    public uint TargetCreatureType { get; set; }
    public SpellCastTargetFlags Targets { get; set; }
    public float Width { get; set; }
    private bool IsAffectedBySpellMods => !HasAttribute(SpellAttr3.IgnoreCasterModifiers);

    public static uint GetDispelMask(DispelType type)
    {
        // If dispel all
        if (type == DispelType.ALL)
            return (uint)DispelType.AllMask;

        return (uint)(1 << (int)type);
    }

    public static SpellCastTargetFlags GetTargetFlagMask(SpellTargetObjectTypes objType)
    {
        return objType switch
        {
            SpellTargetObjectTypes.Dest => SpellCastTargetFlags.DestLocation,
            SpellTargetObjectTypes.UnitAndDest => SpellCastTargetFlags.DestLocation | SpellCastTargetFlags.Unit,
            SpellTargetObjectTypes.CorpseAlly => SpellCastTargetFlags.CorpseAlly,
            SpellTargetObjectTypes.CorpseEnemy => SpellCastTargetFlags.CorpseEnemy,
            SpellTargetObjectTypes.Corpse => SpellCastTargetFlags.CorpseAlly | SpellCastTargetFlags.CorpseEnemy,
            SpellTargetObjectTypes.Unit => SpellCastTargetFlags.Unit,
            SpellTargetObjectTypes.Gobj => SpellCastTargetFlags.Gameobject,
            SpellTargetObjectTypes.GobjItem => SpellCastTargetFlags.GameobjectItem,
            SpellTargetObjectTypes.Item => SpellCastTargetFlags.Item,
            SpellTargetObjectTypes.Src => SpellCastTargetFlags.SourceLocation,
            _ => SpellCastTargetFlags.None
        };
    }

    public void _LoadAuraState()
    {
        AuraState = AuraStateType.None;

        // Faerie Fire
        if (Category == 1133)
            AuraState = AuraStateType.FaerieFire;

        AuraState = SpellFamilyName switch
        {
            // Swiftmend state on Regrowth, Rejuvenation, Wild Growth
            SpellFamilyNames.Druid when SpellFamilyFlags[0].HasAnyFlag(0x50u) || SpellFamilyFlags[1].HasAnyFlag(0x4000000u) => AuraStateType.DruidPeriodicHeal,
            // Deadly poison aura state
            SpellFamilyNames.Rogue when SpellFamilyFlags[0].HasAnyFlag(0x10000u) => AuraStateType.RoguePoisoned,
            _ => AuraState
        };

        // Enrage aura state
        if (Dispel == DispelType.Enrage)
            AuraState = AuraStateType.Enraged;

        // Bleeding aura state
        if (Convert.ToBoolean(GetAllEffectsMechanicMask() & (1 << (int)Mechanics.Bleed)))
            AuraState = AuraStateType.Bleed;

        if (Convert.ToBoolean(SchoolMask & SpellSchoolMask.Frost))
            foreach (var effectInfo in Effects)
                if (effectInfo.IsAuraType(AuraType.ModStun) || effectInfo.IsAuraType(AuraType.ModRoot) || effectInfo.IsAuraType(AuraType.ModRoot2))
                    AuraState = AuraStateType.Frozen;

        AuraState = Id switch
        {
            1064 => // Dazed
                AuraStateType.Dazed,
            32216 => // Victorious
                AuraStateType.Victorious,
            71465 => // Divine Surge
                AuraStateType.RaidEncounter,
            50241 => // Evasive Charges
                AuraStateType.RaidEncounter,
            6950 => // Faerie Fire
                AuraStateType.FaerieFire,
            9806 => // Phantom Strike
                AuraStateType.FaerieFire,
            9991 => // Touch of Zanzil
                AuraStateType.FaerieFire,
            13424 => // Faerie Fire
                AuraStateType.FaerieFire,
            13752 => // Faerie Fire
                AuraStateType.FaerieFire,
            16432 => // Plague Mist
                AuraStateType.FaerieFire,
            20656 => // Faerie Fire
                AuraStateType.FaerieFire,
            25602 => // Faerie Fire
                AuraStateType.FaerieFire,
            32129 => // Faerie Fire
                AuraStateType.FaerieFire,
            35325 => // Glowing Blood
                AuraStateType.FaerieFire,
            35328 => // Lambent Blood
                AuraStateType.FaerieFire,
            35329 => // Vibrant Blood
                AuraStateType.FaerieFire,
            35331 => // Black Blood
                AuraStateType.FaerieFire,
            49163 => // Perpetual Instability
                AuraStateType.FaerieFire,
            65863 => // Faerie Fire
                AuraStateType.FaerieFire,
            79559 => // Luxscale Light
                AuraStateType.FaerieFire,
            82855 => // Dazzling
                AuraStateType.FaerieFire,
            102953 => // In the Rumpus
                AuraStateType.FaerieFire,
            127907 => // Phosphorescence
                AuraStateType.FaerieFire,
            127913 => // Phosphorescence
                AuraStateType.FaerieFire,
            129007 => // Zijin Sting
                AuraStateType.FaerieFire,
            130159 => // Fae Touch
                AuraStateType.FaerieFire,
            142537 => // Spotter Smoke
                AuraStateType.FaerieFire,
            168455 => // Spotted!
                AuraStateType.FaerieFire,
            176905 => // Super Sticky Glitter Bomb
                AuraStateType.FaerieFire,
            189502 => // Marked
                AuraStateType.FaerieFire,
            201785 => // Intruder Alert!
                AuraStateType.FaerieFire,
            201786 => // Intruder Alert!
                AuraStateType.FaerieFire,
            201935 => // Spotted!
                AuraStateType.FaerieFire,
            239233 => // Smoke Bomb
                AuraStateType.FaerieFire,
            319400 => // Glitter Burst
                AuraStateType.FaerieFire,
            321470 => // Dimensional Shifter Mishap
                AuraStateType.FaerieFire,
            331134 => // Spotted
                AuraStateType.FaerieFire,
            _ => AuraState
        };
    }

    public void _LoadImmunityInfo()
    {
        // TODO Pandaros - These need to be moved to scripts.
        foreach (var effect in Effects)
        {
            uint schoolImmunityMask = 0;
            uint applyHarmfulAuraImmunityMask = 0;
            ulong mechanicImmunityMask = 0;
            uint dispelImmunity = 0;
            uint damageImmunityMask = 0;

            var miscVal = effect.MiscValue;
            var amount = effect.CalcValue();

            var immuneInfo = effect.ImmunityInfo;

            switch (effect.ApplyAuraName)
            {
                case AuraType.MechanicImmunityMask:
                {
                    switch (miscVal)
                    {
                        case 96: // Free Friend, Uncontrollable Frenzy, Warlord's Presence
                        {
                            mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                            immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);

                            break;
                        }
                        case 1615: // Incite Rage, Wolf Spirit, Overload, Lightning Tendrils
                        {
                            switch (Id)
                            {
                                case 43292: // Incite Rage
                                case 49172: // Wolf Spirit
                                    mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                    immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                                    goto case 61869;
                                // no break intended
                                case 61869: // Overload
                                case 63481:
                                case 61887: // Lightning Tendrils
                                case 63486:
                                    mechanicImmunityMask |= (1 << (int)Mechanics.Interrupt) | (1 << (int)Mechanics.Silence);

                                    immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBack);
                                    immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBackDest);

                                    break;
                            }

                            break;
                        }
                        case 679: // Mind Control, Avenging Fury
                        {
                            if (Id == 57742) // Avenging Fury
                            {
                                mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                            }

                            break;
                        }
                        case 1557: // Startling Roar, Warlord Roar, Break Bonds, Stormshield
                        {
                            if (Id == 64187) // Stormshield
                            {
                                mechanicImmunityMask |= 1 << (int)Mechanics.Stun;
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                            }
                            else
                            {
                                mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                            }

                            break;
                        }
                        case 1614: // Fixate
                        case 1694: // Fixated, Lightning Tendrils
                        {
                            immuneInfo.SpellEffectImmune.Add(SpellEffectName.AttackMe);
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModTaunt);

                            break;
                        }
                        case 1630: // Fervor, Berserk
                        {
                            if (Id == 64112) // Berserk
                            {
                                immuneInfo.SpellEffectImmune.Add(SpellEffectName.AttackMe);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModTaunt);
                            }
                            else
                            {
                                mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                            }

                            break;
                        }
                        case 477:  // Bladestorm
                        case 1733: // Bladestorm, Killing Spree
                        {
                            if (amount == 0)
                            {
                                mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                                immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBack);
                                immuneInfo.SpellEffectImmune.Add(SpellEffectName.KnockBackDest);

                                immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                            }

                            break;
                        }
                        case 878: // Whirlwind, Fog of Corruption, Determination
                        {
                            if (Id == 66092) // Determination
                            {
                                mechanicImmunityMask |= (1 << (int)Mechanics.Snare) | (1 << (int)Mechanics.Stun) | (1 << (int)Mechanics.Disoriented) | (1 << (int)Mechanics.Freeze);

                                immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);
                                immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);
                            }

                            break;
                        }
                    }

                    if (immuneInfo.AuraTypeImmune.Empty())
                    {
                        if (miscVal.HasAnyFlag(1 << 10))
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModStun);

                        if (miscVal.HasAnyFlag(1 << 1))
                            immuneInfo.AuraTypeImmune.Add(AuraType.Transform);

                        // These Id can be recognized wrong:
                        if (miscVal.HasAnyFlag(1 << 6))
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModDecreaseSpeed);

                        if (miscVal.HasAnyFlag(1 << 0))
                        {
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot);
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModRoot2);
                        }

                        if (miscVal.HasAnyFlag(1 << 2))
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModConfuse);

                        if (miscVal.HasAnyFlag(1 << 9))
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModFear);

                        if (miscVal.HasAnyFlag(1 << 7))
                            immuneInfo.AuraTypeImmune.Add(AuraType.ModDisarm);
                    }

                    break;
                }
                case AuraType.MechanicImmunity:
                {
                    switch (Id)
                    {
                        case 42292: // PvP trinket
                        case 59752: // Every Man for Himself
                            mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;
                            immuneInfo.AuraTypeImmune.Add(AuraType.UseNormalMovementSpeed);

                            break;

                        case 34471:  // The Beast Within
                        case 19574:  // Bestial Wrath
                        case 46227:  // Medallion of Immunity
                        case 53490:  // Bullheaded
                        case 65547:  // PvP Trinket
                        case 134946: // Supremacy of the Alliance
                        case 134956: // Supremacy of the Horde
                        case 195710: // Honorable Medallion
                        case 208683: // Gladiator's Medallion
                            mechanicImmunityMask |= (ulong)Mechanics.ImmuneToMovementImpairmentAndLossControlMask;

                            break;

                        case 54508: // Demonic Empowerment
                            mechanicImmunityMask |= (1 << (int)Mechanics.Snare) | (1 << (int)Mechanics.Root) | (1 << (int)Mechanics.Stun);

                            break;

                        default:
                            if (miscVal < 1)
                                break;

                            mechanicImmunityMask |= 1ul << miscVal;

                            break;
                    }

                    break;
                }
                case AuraType.EffectImmunity:
                {
                    immuneInfo.SpellEffectImmune.Add((SpellEffectName)miscVal);

                    break;
                }
                case AuraType.StateImmunity:
                {
                    immuneInfo.AuraTypeImmune.Add((AuraType)miscVal);

                    break;
                }
                case AuraType.SchoolImmunity:
                {
                    schoolImmunityMask |= (uint)miscVal;

                    break;
                }
                case AuraType.ModImmuneAuraApplySchool:
                {
                    applyHarmfulAuraImmunityMask |= (uint)miscVal;

                    break;
                }
                case AuraType.DamageImmunity:
                {
                    damageImmunityMask |= (uint)miscVal;

                    break;
                }
                case AuraType.DispelImmunity:
                {
                    dispelImmunity = (uint)miscVal;

                    break;
                }
            }

            immuneInfo.SchoolImmuneMask = schoolImmunityMask;
            immuneInfo.ApplyHarmfulAuraImmuneMask = applyHarmfulAuraImmunityMask;
            immuneInfo.MechanicImmuneMask = mechanicImmunityMask;
            immuneInfo.DispelImmune = dispelImmunity;
            immuneInfo.DamageSchoolMask = damageImmunityMask;

            AllowedMechanicMask |= immuneInfo.MechanicImmuneMask;
        }

        if (HasAttribute(SpellAttr5.AllowWhileStunned))
            switch (Id)
            {
                case 22812: // Barkskin
                case 47585: // Dispersion
                    AllowedMechanicMask |=
                        (1 << (int)Mechanics.Stun) |
                        (1 << (int)Mechanics.Freeze) |
                        (1 << (int)Mechanics.Knockout) |
                        (1 << (int)Mechanics.Sleep);

                    break;

                case 49039: // Lichborne, don't allow normal stuns
                    break;

                default:
                    AllowedMechanicMask |= 1 << (int)Mechanics.Stun;

                    break;
            }

        if (HasAttribute(SpellAttr5.AllowWhileConfused))
            AllowedMechanicMask |= 1 << (int)Mechanics.Disoriented;

        if (!HasAttribute(SpellAttr5.AllowWhileFleeing))
            return;

        switch (Id)
        {
            case 22812: // Barkskin
            case 47585: // Dispersion
                AllowedMechanicMask |= (1 << (int)Mechanics.Fear) | (1 << (int)Mechanics.Horror);

                break;

            default:
                AllowedMechanicMask |= (1 << (int)Mechanics.Fear);

                break;
        }
    }

    public void _LoadSpellDiminishInfo()
    {
        SpellDiminishInfo diminishInfo = new()
        {
            DiminishGroup = DiminishingGroupCompute()
        };

        diminishInfo.DiminishReturnType = DiminishingTypeCompute(diminishInfo.DiminishGroup);
        diminishInfo.DiminishMaxLevel = DiminishingMaxLevelCompute(diminishInfo.DiminishGroup);
        diminishInfo.DiminishDurationLimit = DiminishingLimitDurationCompute();

        _diminishInfo = diminishInfo;
    }

    public void _LoadSpellSpecific()
    {
        SpellSpecific = SpellSpecificType.Normal;

        switch (SpellFamilyName)
        {
            case SpellFamilyNames.Generic:
            {
                // Food / Drinks (mostly)
                if (HasAuraInterruptFlag(SpellAuraInterruptFlags.Standing))
                {
                    var food = false;
                    var drink = false;

                    foreach (var effectInfo in Effects)
                    {
                        if (!effectInfo.IsAura)
                            continue;

                        switch (effectInfo.ApplyAuraName)
                        {
                            // Food
                            case AuraType.ModRegen:
                            case AuraType.ObsModHealth:
                                food = true;

                                break;
                            // Drink
                            case AuraType.ModPowerRegen:
                            case AuraType.ObsModPower:
                                drink = true;

                                break;
                        }
                    }

                    if (food && drink)
                        SpellSpecific = SpellSpecificType.FoodAndDrink;
                    else if (food)
                        SpellSpecific = SpellSpecificType.Food;
                    else if (drink)
                        SpellSpecific = SpellSpecificType.Drink;
                }
                // scrolls effects
                else
                {
                    var firstRankSpellInfo = FirstRankSpell;

                    SpellSpecific = firstRankSpellInfo.Id switch
                    {
                        8118 => // Strength
                            SpellSpecificType.Scroll,
                        8099 => // Stamina
                            SpellSpecificType.Scroll,
                        8112 => // Spirit
                            SpellSpecificType.Scroll,
                        8096 => // Intellect
                            SpellSpecificType.Scroll,
                        8115 => // Agility
                            SpellSpecificType.Scroll,
                        8091 => // Armor
                            SpellSpecificType.Scroll,
                        _ => SpellSpecific
                    };
                }

                break;
            }
            case SpellFamilyNames.Mage:
            {
                // family flags 18(Molten), 25(Frost/Ice), 28(Mage)
                if (SpellFamilyFlags[0].HasAnyFlag(0x12040000u))
                    SpellSpecific = SpellSpecificType.MageArmor;

                // Arcane brillance and Arcane intelect (normal check fails because of flags difference)
                if (SpellFamilyFlags[0].HasAnyFlag(0x400u))
                    SpellSpecific = SpellSpecificType.MageArcaneBrillance;

                if (SpellFamilyFlags[0].HasAnyFlag(0x1000000u) && GetEffect(0).IsAuraType(AuraType.ModConfuse))
                    SpellSpecific = SpellSpecificType.MagePolymorph;

                break;
            }
            case SpellFamilyNames.Warrior:
            {
                if (Id == 12292) // Death Wish
                    SpellSpecific = SpellSpecificType.WarriorEnrage;

                break;
            }
            case SpellFamilyNames.Warlock:
            {
                // Warlock (Bane of Doom | Bane of Agony | Bane of Havoc)
                if (Id is 603 or 980 or 80240)
                    SpellSpecific = SpellSpecificType.Bane;

                // only warlock curses have this
                if (Dispel == DispelType.Curse)
                    SpellSpecific = SpellSpecificType.Curse;

                // Warlock (Demon Armor | Demon Skin | Fel Armor)
                if (SpellFamilyFlags[1].HasAnyFlag(0x20000020u) || SpellFamilyFlags[2].HasAnyFlag(0x00000010u))
                    SpellSpecific = SpellSpecificType.WarlockArmor;

                //seed of corruption and corruption
                if (SpellFamilyFlags[1].HasAnyFlag(0x10u) || SpellFamilyFlags[0].HasAnyFlag(0x2u))
                    SpellSpecific = SpellSpecificType.WarlockCorruption;

                break;
            }
            case SpellFamilyNames.Priest:
            {
                // Divine Spirit and Prayer of Spirit
                if (SpellFamilyFlags[0].HasAnyFlag(0x20u))
                    SpellSpecific = SpellSpecificType.PriestDivineSpirit;

                break;
            }
            case SpellFamilyNames.Hunter:
            {
                // only hunter stings have this
                if (Dispel == DispelType.Poison)
                    SpellSpecific = SpellSpecificType.Sting;

                // only hunter aspects have this (but not all aspects in hunter family)
                if (SpellFamilyFlags & new FlagArray128(0x00200000, 0x00000000, 0x00001010))
                    SpellSpecific = SpellSpecificType.Aspect;

                break;
            }
            case SpellFamilyNames.Paladin:
            {
                // Collection of all the seal family flags. No other paladin spell has any of those.
                if (SpellFamilyFlags[1].HasAnyFlag(0xA2000800))
                    SpellSpecific = SpellSpecificType.Seal;

                if (SpellFamilyFlags[0].HasAnyFlag(0x00002190u))
                    SpellSpecific = SpellSpecificType.Hand;

                // only paladin auras have this (for palaldin class family)
                SpellSpecific = Id switch
                {
                    465 => // Devotion Aura
                        SpellSpecificType.Aura,
                    32223 => // Crusader Aura
                        SpellSpecificType.Aura,
                    183435 => // Retribution Aura
                        SpellSpecificType.Aura,
                    317920 => // Concentration Aura
                        SpellSpecificType.Aura,
                    _ => SpellSpecific
                };

                break;
            }
            case SpellFamilyNames.Shaman:
            {
                // family flags 10 (Lightning), 42 (Earth), 37 (Water), proc shield from T2 8 pieces bonus
                if (SpellFamilyFlags[1].HasAnyFlag(0x420u) || SpellFamilyFlags[0].HasAnyFlag(0x00000400u) || Id == 23552)
                    SpellSpecific = SpellSpecificType.ElementalShield;

                break;
            }
            case SpellFamilyNames.Deathknight:
                if (Id is 48266 or 48263 or 48265)
                    SpellSpecific = SpellSpecificType.Presence;

                break;
        }

        foreach (var effectInfo in Effects)
            if (effectInfo.IsEffectName(SpellEffectName.ApplyAura))
                switch (effectInfo.ApplyAuraName)
                {
                    case AuraType.ModCharm:
                    case AuraType.ModPossessPet:
                    case AuraType.ModPossess:
                    case AuraType.AoeCharm:
                        SpellSpecific = SpellSpecificType.Charm;

                        break;

                    case AuraType.TrackCreatures:
                        // @workaround For non-stacking tracking spells (We need generic solution)
                        if (Id == 30645) // Gas Cloud Tracking
                            SpellSpecific = SpellSpecificType.Normal;

                        break;

                    case AuraType.TrackResources:
                    case AuraType.TrackStealthed:
                        SpellSpecific = SpellSpecificType.Tracker;

                        break;
                }
    }

    public void ApplyAllSpellImmunitiesTo(Unit target, SpellEffectInfo spellEffectInfo, bool apply)
    {
        var immuneInfo = spellEffectInfo.ImmunityInfo;

        var schoolImmunity = immuneInfo.SchoolImmuneMask;

        if (schoolImmunity != 0)
        {
            target.ApplySpellImmune(Id, SpellImmunity.School, schoolImmunity, apply);

            if (apply && HasAttribute(SpellAttr1.ImmunityPurgesEffect))
                target.RemoveAppliedAuras(aurApp =>
                {
                    var auraSpellInfo = aurApp.Base.SpellInfo;

                    return ((uint)auraSpellInfo.SchoolMask & schoolImmunity) != 0 && // Check for school mask
                           CanDispelAura(auraSpellInfo) &&
                           IsPositive != aurApp.IsPositive && // Check spell vs aura possitivity
                           !auraSpellInfo.IsPassive &&        // Don't remove passive auras
                           auraSpellInfo.Id != Id;            // Don't remove self
                });

            if (apply && (schoolImmunity & (uint)SpellSchoolMask.Normal) != 0)
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.InvulnerabilityBuff);
        }

        var mechanicImmunity = immuneInfo.MechanicImmuneMask;

        if (mechanicImmunity != 0)
        {
            for (uint i = 0; i < (int)Mechanics.Max; ++i)
                if (Convert.ToBoolean(mechanicImmunity & (1ul << (int)i)))
                    target.ApplySpellImmune(Id, SpellImmunity.Mechanic, i, apply);

            if (HasAttribute(SpellAttr1.ImmunityPurgesEffect))
            {
                // exception for purely snare mechanic (eg. hands of freedom)!
                if (apply)
                    target.RemoveAurasWithMechanic(mechanicImmunity, AuraRemoveMode.Default, Id);
                else
                {
                    List<Aura> aurasToUpdateTargets = new();

                    target.RemoveAppliedAuras(aurApp =>
                    {
                        var aura = aurApp.Base;

                        if ((aura.SpellInfo.GetAllEffectsMechanicMask() & mechanicImmunity) != 0)
                            aurasToUpdateTargets.Add(aura);

                        // only update targets, don't remove anything
                        return false;
                    });

                    foreach (var aura in aurasToUpdateTargets)
                        aura.UpdateTargetMap(aura.Caster);
                }
            }
        }

        var dispelImmunity = immuneInfo.DispelImmune;

        if (dispelImmunity != 0)
        {
            target.ApplySpellImmune(Id, SpellImmunity.Dispel, dispelImmunity, apply);

            if (apply && HasAttribute(SpellAttr1.ImmunityPurgesEffect))
                target.RemoveAppliedAuras(aurApp =>
                {
                    var spellInfo = aurApp.Base.SpellInfo;

                    if ((uint)spellInfo.Dispel == dispelImmunity)
                        return true;

                    return false;
                });
        }

        var damageImmunity = immuneInfo.DamageSchoolMask;

        if (damageImmunity != 0)
        {
            target.ApplySpellImmune(Id, SpellImmunity.Damage, damageImmunity, apply);

            if (apply && (damageImmunity & (uint)SpellSchoolMask.Normal) != 0)
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.InvulnerabilityBuff);
        }

        foreach (var auraType in immuneInfo.AuraTypeImmune)
        {
            target.ApplySpellImmune(Id, SpellImmunity.State, auraType, apply);

            if (apply && HasAttribute(SpellAttr1.ImmunityPurgesEffect))
                target.RemoveAurasByType(auraType,
                                         aurApp =>
                                         {
                                             // if the aura has SPELL_ATTR0_NO_IMMUNITIES, then it cannot be removed by immunity
                                             return !aurApp.Base.SpellInfo.HasAttribute(SpellAttr0.NoImmunities);
                                         });
        }

        foreach (var effectType in immuneInfo.SpellEffectImmune)
            target.ApplySpellImmune(Id, SpellImmunity.Effect, effectType, apply);
    }

    public int CalcCastTime(Spell spell = null)
    {
        var castTime = 0;

        if (CastTimeEntry != null)
            castTime = Math.Max(CastTimeEntry.Base, CastTimeEntry.Minimum);

        if (castTime <= 0)
            return 0;

        spell?.Caster.WorldObjectCombat.ModSpellCastTime(this, ref castTime, spell);

        if (HasAttribute(SpellAttr0.UsesRangedSlot) && !IsAutoRepeatRangedSpell && !HasAttribute(SpellAttr9.AimedShot))
            castTime += 500;

        return castTime > 0 ? castTime : 0;
    }

    public int CalcDuration(WorldObject caster = null)
    {
        var duration = Duration;
        caster?.SpellModOwner?.ApplySpellMod(this, SpellModOp.Duration, ref duration);

        return duration;
    }

    public SpellPowerCost CalcPowerCost(PowerType powerType, bool optionalCost, WorldObject caster, SpellSchoolMask schoolMask, Spell spell = null)
    {
        // gameobject casts don't use power
        var unitCaster = caster.AsUnit;

        if (unitCaster == null)
            return null;

        var spellPowerRecord = PowerCosts.FirstOrDefault(spellPowerEntry => spellPowerEntry?.PowerType == powerType);

        return spellPowerRecord == null ? null : CalcPowerCost(spellPowerRecord, optionalCost, caster, schoolMask, spell);
    }

    public SpellPowerCost CalcPowerCost(SpellPowerRecord power, bool optionalCost, WorldObject caster, SpellSchoolMask schoolMask, Spell spell = null)
    {
        // gameobject casts don't use power
        var unitCaster = caster.AsUnit;

        if (unitCaster == null)
            return null;

        if (power.RequiredAuraSpellID != 0 && !unitCaster.HasAura(power.RequiredAuraSpellID))
            return null;

        SpellPowerCost cost = new();

        // Spell drain all exist power on cast (Only paladin lay of Hands)
        if (HasAttribute(SpellAttr1.UseAllMana))
        {
            // If power type - health drain all
            if (power.PowerType == PowerType.Health)
            {
                cost.Power = PowerType.Health;
                cost.Amount = (int)unitCaster.Health;

                return cost;
            }

            // Else drain all power
            if (power.PowerType < PowerType.Max)
            {
                cost.Power = power.PowerType;
                cost.Amount = unitCaster.GetPower(cost.Power);

                return cost;
            }

            Log.Logger.Error($"SpellInfo.CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id}");

            return default;
        }

        // Base powerCost
        double powerCost = 0;

        if (!optionalCost)
        {
            powerCost = power.ManaCost;

            // PCT cost from total amount
            if (power.PowerCostPct != 0)
                switch (power.PowerType)
                {
                    // health as power used
                    case PowerType.Health:
                        if (MathFunctions.fuzzyEq(power.PowerCostPct, 0.0f))
                            powerCost += (int)MathFunctions.CalculatePct(unitCaster.MaxHealth, power.PowerCostMaxPct);
                        else
                            powerCost += (int)MathFunctions.CalculatePct(unitCaster.MaxHealth, power.PowerCostPct);

                        break;

                    case PowerType.Mana:
                        powerCost += (int)MathFunctions.CalculatePct(unitCaster.GetCreateMana(), power.PowerCostPct);

                        break;

                    case PowerType.AlternatePower:
                        Log.Logger.Error($"SpellInfo.CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id}");

                        return null;

                    default:
                    {
                        var powerTypeEntry = _db2Manager.GetPowerTypeEntry(power.PowerType);

                        if (powerTypeEntry != null)
                        {
                            powerCost += MathFunctions.CalculatePct(powerTypeEntry.MaxBasePower, power.PowerCostPct);

                            break;
                        }

                        Log.Logger.Error($"SpellInfo.CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id}");

                        return null;
                    }
                }
        }
        else
        {
            powerCost = power.OptionalCost;

            if (power.OptionalCostPct != 0)
                switch (power.PowerType)
                {
                    // health as power used
                    case PowerType.Health:
                        powerCost += (int)MathFunctions.CalculatePct(unitCaster.MaxHealth, power.OptionalCostPct);

                        break;

                    case PowerType.Mana:
                        powerCost += (int)MathFunctions.CalculatePct(unitCaster.GetCreateMana(), power.OptionalCostPct);

                        break;

                    case PowerType.AlternatePower:
                        Log.Logger.Error($"SpellInfo::CalcPowerCost: Unsupported power type POWER_ALTERNATE_POWER in spell {Id} for optional cost percent");

                        return null;

                    default:
                    {
                        var powerTypeEntry = _db2Manager.GetPowerTypeEntry(power.PowerType);

                        if (powerTypeEntry != null)
                        {
                            powerCost += MathFunctions.CalculatePct(powerTypeEntry.MaxBasePower, power.OptionalCostPct);

                            break;
                        }

                        Log.Logger.Error($"SpellInfo::CalcPowerCost: Unknown power type '{power.PowerType}' in spell {Id} for optional cost percent");

                        return null;
                    }
                }

            powerCost += unitCaster.GetTotalAuraModifier(AuraType.ModAdditionalPowerCost, aurEff => aurEff.MiscValue == (int)power.PowerType && aurEff.IsAffectingSpell(this));
        }

        var initiallyNegative = powerCost < 0;

        // Shiv - costs 20 + weaponSpeed*10 energy (apply only to non-triggered spell with energy cost)
        if (HasAttribute(SpellAttr4.WeaponSpeedCostScaling))
        {
            uint speed = 0;

            if (_cliDB.SpellShapeshiftFormStorage.TryGetValue((uint)unitCaster.ShapeshiftForm, out var ss))
                speed = ss.CombatRoundTime;
            else
            {
                var slot = WeaponAttackType.BaseAttack;

                if (!HasAttribute(SpellAttr3.RequiresMainHandWeapon) && HasAttribute(SpellAttr3.RequiresOffHandWeapon))
                    slot = WeaponAttackType.OffAttack;

                speed = unitCaster.GetBaseAttackTime(slot);
            }

            powerCost += speed / 100f;
        }

        if (power.PowerType != PowerType.Health)
        {
            if (!optionalCost)
                // Flat mod from caster auras by spell school and power type
                foreach (var aura in unitCaster.GetAuraEffectsByType(AuraType.ModPowerCostSchool))
                {
                    if ((aura.MiscValue & (int)schoolMask) == 0)
                        continue;

                    if ((aura.MiscValueB & (1 << (int)power.PowerType)) == 0)
                        continue;

                    powerCost += aura.Amount;
                }

            // PCT mod from user auras by spell school and power type
            foreach (var schoolCostPct in unitCaster.GetAuraEffectsByType(AuraType.ModPowerCostSchoolPct))
            {
                if ((schoolCostPct.MiscValue & (int)schoolMask) == 0)
                    continue;

                if ((schoolCostPct.MiscValueB & (1 << (int)power.PowerType)) == 0)
                    continue;

                powerCost += MathFunctions.CalculatePct(powerCost, schoolCostPct.Amount);
            }
        }

        // Apply cost mod by spell
        var modOwner = unitCaster.SpellModOwner;

        if (modOwner != null)
        {
            var mod = power.OrderIndex switch
            {
                0 => SpellModOp.PowerCost0,
                1 => SpellModOp.PowerCost1,
                2 => SpellModOp.PowerCost2,
                _ => SpellModOp.Max
            };

            if (mod != SpellModOp.Max)
            {
                if (!optionalCost)
                    modOwner.ApplySpellMod(this, mod, ref powerCost, spell);
                else
                {
                    // optional cost ignores flat modifiers
                    double flatMod = 0;
                    double pctMod = 1.0f;
                    modOwner.GetSpellModValues(this, mod, spell, powerCost, ref flatMod, ref pctMod);
                    powerCost *= pctMod;
                }
            }
        }

        if (!unitCaster.ControlledByPlayer && MathFunctions.fuzzyEq(power.PowerCostPct, 0.0f) && SpellLevel != 0 && power.PowerType == PowerType.Mana)
            if (HasAttribute(SpellAttr0.ScalesWithCreatureLevel))
            {
                var spellScaler = _cliDB.NpcManaCostScalerGameTable.GetRow(SpellLevel);
                var casterScaler = _cliDB.NpcManaCostScalerGameTable.GetRow(unitCaster.Level);

                if (spellScaler != null && casterScaler != null)
                    powerCost *= (int)(casterScaler.Scaler / spellScaler.Scaler);
            }

        if (power.PowerType == PowerType.Mana)
            powerCost = (int)(powerCost * (1.0f + unitCaster.UnitData.ManaCostMultiplier));

        // power cost cannot become negative if initially positive
        if (initiallyNegative != powerCost < 0)
            powerCost = 0;

        cost.Power = power.PowerType;
        cost.Amount = (int)powerCost;

        return cost;
    }

    public List<SpellPowerCost> CalcPowerCost(WorldObject caster, SpellSchoolMask schoolMask, Spell spell = null)
    {
        List<SpellPowerCost> costs = new();

        if (!caster.IsUnit)
            return costs;

        SpellPowerCost GetOrCreatePowerCost(PowerType powerType)
        {
            var itr = costs.Find(cost => cost.Power == powerType);

            if (itr != null)
                return itr;

            SpellPowerCost cost = new()
            {
                Power = powerType,
                Amount = 0
            };

            costs.Add(cost);

            return costs.Last();
        }

        foreach (var power in PowerCosts)
        {
            if (power == null)
                continue;

            var cost = CalcPowerCost(power, false, caster, schoolMask, spell);

            if (cost != null)
                GetOrCreatePowerCost(cost.Power).Amount += cost.Amount;

            var optionalCost = CalcPowerCost(power, true, caster, schoolMask, spell);

            if (optionalCost == null)
                continue;

            var cost1 = GetOrCreatePowerCost(optionalCost.Power);
            var remainingPower = caster.AsUnit.GetPower(optionalCost.Power) - cost1.Amount;

            if (remainingPower > 0)
                cost1.Amount += Math.Min(optionalCost.Amount, remainingPower);
        }

        return costs;
    }

    public double CalcProcPPM(Unit caster, int itemLevel)
    {
        double ppm = ProcBasePpm;

        if (caster == null)
            return ppm;

        foreach (var mod in _procPpmMods)
            switch (mod.Type)
            {
                case SpellProcsPerMinuteModType.Haste:
                {
                    ppm *= 1.0f + CalcPPMHasteMod(mod, caster);

                    break;
                }
                case SpellProcsPerMinuteModType.Crit:
                {
                    ppm *= 1.0f + CalcPPMCritMod(mod, caster);

                    break;
                }
                case SpellProcsPerMinuteModType.Class:
                {
                    if (caster.ClassMask.HasAnyFlag(mod.Param))
                        ppm *= 1.0f + mod.Coeff;

                    break;
                }
                case SpellProcsPerMinuteModType.Spec:
                {
                    var plrCaster = caster.AsPlayer;

                    if (plrCaster != null)
                        if (plrCaster.GetPrimarySpecialization() == mod.Param)
                            ppm *= 1.0f + mod.Coeff;

                    break;
                }
                case SpellProcsPerMinuteModType.Race:
                {
                    if (SharedConst.GetMaskForRace(caster.Race).HasAnyFlag((int)mod.Param))
                        ppm *= 1.0f + mod.Coeff;

                    break;
                }
                case SpellProcsPerMinuteModType.ItemLevel:
                {
                    ppm *= 1.0f + CalcPPMItemLevelMod(mod, itemLevel);

                    break;
                }
                case SpellProcsPerMinuteModType.Battleground:
                {
                    if (caster.Location.Map.IsBattlegroundOrArena)
                        ppm *= 1.0f + mod.Coeff;

                    break;
                }
            }

        return ppm;
    }

    public bool CanBeInterrupted(WorldObject interruptCaster, Unit interruptTarget, bool ignoreImmunity = false)
    {
        return HasAttribute(SpellAttr7.CanAlwaysBeInterrupted) || HasChannelInterruptFlag(SpellAuraInterruptFlags.Damage | SpellAuraInterruptFlags.EnteringCombat) || (interruptTarget.IsPlayer && InterruptFlags.HasFlag(SpellInterruptFlags.DamageCancelsPlayerOnly)) || InterruptFlags.HasFlag(SpellInterruptFlags.DamageCancels) || (interruptCaster is { IsUnit: true } && interruptCaster.AsUnit.HasAuraTypeWithMiscvalue(AuraType.AllowInterruptSpell, (int)Id)) || (((interruptTarget.MechanicImmunityMask & (1 << (int)Mechanics.Interrupt)) == 0 || ignoreImmunity) && !interruptTarget.HasAuraTypeWithAffectMask(AuraType.PreventInterrupt, this) && PreventionType.HasAnyFlag(SpellPreventionType.Silence));
    }

    public bool CanDispelAura(SpellInfo auraSpellInfo)
    {
        // These auras (like Divine Shield) can't be dispelled
        if (auraSpellInfo.HasAttribute(SpellAttr0.NoImmunities))
            return false;

        // These spells (like Mass Dispel) can dispel all auras
        if (HasAttribute(SpellAttr0.NoImmunities))
            return true;

        // These auras (Cyclone for example) are not dispelable
        return (!auraSpellInfo.HasAttribute(SpellAttr1.ImmunityToHostileAndFriendlyEffects) || auraSpellInfo.Mechanic == Mechanics.None) && !auraSpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities);
    }

    public bool CanPierceImmuneAura(SpellInfo auraSpellInfo)
    {
        // aura can't be pierced
        if (auraSpellInfo == null || auraSpellInfo.HasAttribute(SpellAttr0.NoImmunities))
            return false;

        // these spells pierce all avalible spells (Resurrection Sickness for example)
        if (HasAttribute(SpellAttr0.NoImmunities))
            return true;

        // these spells (Cyclone for example) can pierce all...
        if (!HasAttribute(SpellAttr1.ImmunityToHostileAndFriendlyEffects) && !HasAttribute(SpellAttr2.NoSchoolImmunities))
            return HasAttribute(SpellAttr1.ImmunityPurgesEffect) && CanSpellProvideImmunityAgainstAura(auraSpellInfo);

        // ...but not these (Divine shield, Ice block, Cyclone and Banish for example)
        if (auraSpellInfo.Mechanic != Mechanics.ImmuneShield &&
            auraSpellInfo.Mechanic != Mechanics.Invulnerability &&
            (auraSpellInfo.Mechanic != Mechanics.Banish || (IsRankOf(auraSpellInfo) && auraSpellInfo.Dispel != DispelType.None))) // Banish shouldn't be immune to itself, but Cyclone should
            return true;

        // Dispels other auras on immunity, check if this spell makes the unit immune to aura
        return HasAttribute(SpellAttr1.ImmunityPurgesEffect) && CanSpellProvideImmunityAgainstAura(auraSpellInfo);
    }

    public SpellCastResult CheckExplicitTarget(WorldObject caster, WorldObject target, Item itemTarget = null)
    {
        var neededTargets = ExplicitTargetMask;

        if (target == null)
        {
            if (!Convert.ToBoolean(neededTargets & (SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.GameobjectMask | SpellCastTargetFlags.CorpseMask)))
                return SpellCastResult.SpellCastOk;

            if (!Convert.ToBoolean(neededTargets & SpellCastTargetFlags.GameobjectItem) || itemTarget == null)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        var unitTarget = target.AsUnit;

        if (unitTarget == null)
            return SpellCastResult.SpellCastOk;

        if (!neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitEnemy | SpellCastTargetFlags.UnitAlly | SpellCastTargetFlags.UnitRaid | SpellCastTargetFlags.UnitParty | SpellCastTargetFlags.UnitMinipet | SpellCastTargetFlags.UnitPassenger))
            return SpellCastResult.SpellCastOk;

        var unitCaster = caster.AsUnit;

        if (neededTargets.HasFlag(SpellCastTargetFlags.UnitEnemy))
            if (caster.WorldObjectCombat.IsValidAttackTarget(unitTarget, this))
                return SpellCastResult.SpellCastOk;

        if (neededTargets.HasFlag(SpellCastTargetFlags.UnitAlly) || (neededTargets.HasFlag(SpellCastTargetFlags.UnitParty) && unitCaster != null && unitCaster.IsInPartyWith(unitTarget)) || (neededTargets.HasFlag(SpellCastTargetFlags.UnitRaid) && unitCaster != null && unitCaster.IsInRaidWith(unitTarget)))
            if (caster.WorldObjectCombat.IsValidAssistTarget(unitTarget, this))
                return SpellCastResult.SpellCastOk;

        if (neededTargets.HasFlag(SpellCastTargetFlags.UnitMinipet) && unitCaster != null)
            if (unitTarget.GUID == unitCaster.CritterGUID)
                return SpellCastResult.SpellCastOk;

        if (!neededTargets.HasFlag(SpellCastTargetFlags.UnitPassenger) || unitCaster == null)
            return SpellCastResult.BadTargets;

        return unitTarget.IsOnVehicle(unitCaster) ? SpellCastResult.SpellCastOk : SpellCastResult.BadTargets;
    }

    public SpellCastResult CheckLocation(uint mapID, uint zoneID, uint areaID, Player player)
    {
        // normal case
        if (RequiredAreasId > 0)
        {
            var areaGroupMembers = _db2Manager.GetAreasForGroup((uint)RequiredAreasId);

            var found = areaGroupMembers.Any(areaId => areaId == zoneID || areaId == areaID);

            if (!found)
                return SpellCastResult.IncorrectArea;
        }

        // continent limitation (virtual continent)
        if (HasAttribute(SpellAttr4.OnlyFlyingAreas))
        {
            uint mountFlags = 0;

            if (player != null && player.HasAuraType(AuraType.MountRestrictions))
                foreach (var auraEffect in player.GetAuraEffectsByType(AuraType.MountRestrictions))
                    mountFlags |= (uint)auraEffect.MiscValue;
            else
            {
                if (_cliDB.AreaTableStorage.TryGetValue(areaID, out var areaTable))
                    mountFlags = areaTable.MountFlags;
            }

            if (!Convert.ToBoolean(mountFlags & (uint)AreaMountFlags.FlyingAllowed))
                return SpellCastResult.IncorrectArea;

            if (player != null)
            {
                var mapToCheck = mapID;

                if (_cliDB.MapStorage.TryGetValue(mapID, out var mapEntry1))
                    mapToCheck = (uint)mapEntry1.CosmeticParentMapID;

                switch (mapToCheck) // TODO Pandaros - Move to Scripts
                {
                    // Draenor Pathfinder
                    case 1116 or 1464 when !player.HasSpell(191645):
                    // Broken Isles Pathfinder
                    case 1220 when !player.HasSpell(233368):
                    // Battle for Azeroth Pathfinder
                    case 1642 or 1643 when !player.HasSpell(278833):
                        return SpellCastResult.IncorrectArea;
                }
            }
        }

        var mapEntry = _cliDB.MapStorage.LookupByKey(mapID);

        // raid instance limitation
        if (HasAttribute(SpellAttr6.NotInRaidInstances))
            if (mapEntry == null || mapEntry.IsRaid())
                return SpellCastResult.NotInRaidInstance;

        // DB base check (if non empty then must fit at least single for allow)
        var saBounds = _spellManager.GetSpellAreaMapBounds(Id);

        if (!saBounds.Empty())
            return saBounds.Any(bound => bound.IsFitToRequirements(player, zoneID, areaID)) ? SpellCastResult.SpellCastOk : SpellCastResult.IncorrectArea;

        // bg spell checks
        switch (Id)
        {
            case 23333: // Warsong Flag
            case 23335: // Silverwing Flag
                return mapID == 489 && player is { InBattleground: true } ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;

            case 34976: // Netherstorm Flag
                return mapID == 566 && player is { InBattleground: true } ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;

            case 2584:  // Waiting to Resurrect
            case 22011: // Spirit Heal Channel
            case 22012: // Spirit Heal
            case 42792: // Recently Dropped Flag
            case 43681: // Inactive
            case 44535: // Spirit Heal (mana)
                if (mapEntry == null)
                    return SpellCastResult.IncorrectArea;

                return zoneID == (uint)AreaId.Wintergrasp || (mapEntry.IsBattleground() && player is { InBattleground: true }) ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;

            case 44521: // Preparation
            {
                if (player == null)
                    return SpellCastResult.RequiresArea;

                if (mapEntry == null)
                    return SpellCastResult.IncorrectArea;

                if (!mapEntry.IsBattleground())
                    return SpellCastResult.RequiresArea;

                var bg = player.Battleground;

                return bg is { Status: BattlegroundStatus.WaitJoin } ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
            }
            case 32724: // Gold Team (Alliance)
            case 32725: // Green Team (Alliance)
            case 35774: // Gold Team (Horde)
            case 35775: // Green Team (Horde)
                if (mapEntry == null)
                    return SpellCastResult.IncorrectArea;

                return mapEntry.IsBattleArena() && player is { InBattleground: true } ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;

            case 32727: // Arena Preparation
            {
                if (player == null)
                    return SpellCastResult.RequiresArea;

                if (mapEntry == null)
                    return SpellCastResult.IncorrectArea;

                if (!mapEntry.IsBattleArena())
                    return SpellCastResult.RequiresArea;

                var bg = player.Battleground;

                return bg is { Status: BattlegroundStatus.WaitJoin } ? SpellCastResult.SpellCastOk : SpellCastResult.RequiresArea;
            }
        }

        // aura limitations
        if (player == null)
            return SpellCastResult.SpellCastOk;

        foreach (var effectInfo in Effects)
        {
            if (!effectInfo.IsAura)
                continue;

            switch (effectInfo.ApplyAuraName)
            {
                case AuraType.ModShapeshift:
                {
                    if (_cliDB.SpellShapeshiftFormStorage.TryGetValue((uint)effectInfo.MiscValue, out var spellShapeshiftForm))
                    {
                        uint mountType = spellShapeshiftForm.MountTypeID;

                        if (mountType != 0)
                            if (player.GetMountCapability(mountType) == null)
                                return SpellCastResult.NotHere;
                    }

                    break;
                }
                case AuraType.Mounted:
                {
                    var mountType = (uint)effectInfo.MiscValueB;
                    var mountEntry = _db2Manager.GetMount(Id);

                    if (mountEntry != null)
                        mountType = mountEntry.MountTypeID;

                    if (mountType != 0 && player.GetMountCapability(mountType) == null)
                        return SpellCastResult.NotHere;

                    break;
                }
            }
        }

        return SpellCastResult.SpellCastOk;
    }

    public SpellCastResult CheckShapeshift(ShapeShiftForm form)
    {
        // talents that learn spells can have stance requirements that need ignore
        // (this requirement only for client-side stance show in talent description)
        /* TODO: 6.x fix this in proper way (probably spell flags/attributes?)
        if (_cliDB.GetTalentSpellCost(Id) > 0 && HasEffect(SpellEffects.LearnSpell))
        return SpellCastResult.SpellCastOk;
        */

        //if (HasAttribute(SPELL_ATTR13_ACTIVATES_REQUIRED_SHAPESHIFT))
        //    return SPELL_CAST_OK;

        var stanceMask = form != 0 ? 1ul << ((int)form - 1) : 0;

        if (Convert.ToBoolean(stanceMask & StancesNot)) // can explicitly not be casted in this stance
            return SpellCastResult.NotShapeshift;

        if (Convert.ToBoolean(stanceMask & Stances)) // can explicitly be casted in this stance
            return SpellCastResult.SpellCastOk;

        var actAsShifted = false;
        SpellShapeshiftFormRecord shapeInfo = null;

        if (form > 0)
        {
            shapeInfo = _cliDB.SpellShapeshiftFormStorage.LookupByKey(form);

            if (shapeInfo == null)
            {
                Log.Logger.Error("GetErrorAtShapeshiftedCast: unknown shapeshift {0}", form);

                return SpellCastResult.SpellCastOk;
            }

            actAsShifted = !shapeInfo.Flags.HasAnyFlag(SpellShapeshiftFormFlags.Stance);
        }

        if (actAsShifted)
        {
            if (HasAttribute(SpellAttr0.NotShapeshifted) || shapeInfo.Flags.HasAnyFlag(SpellShapeshiftFormFlags.CanOnlyCastShapeshiftSpells)) // not while shapeshifted
                return SpellCastResult.NotShapeshift;

            if (Stances != 0) // needs other shapeshift
                return SpellCastResult.OnlyShapeshift;
        }
        else
        {
            // needs shapeshift
            if (!HasAttribute(SpellAttr2.AllowWhileNotShapeshiftedCasterForm) && Stances != 0)
                return SpellCastResult.OnlyShapeshift;
        }

        return SpellCastResult.SpellCastOk;
    }

    public SpellCastResult CheckTarget(WorldObject caster, WorldObject target, bool implicitCast = true)
    {
        if (HasAttribute(SpellAttr1.ExcludeCaster) && caster == target)
            return SpellCastResult.BadTargets;

        // check visibility - ignore stealth for implicit (area) targets
        if (!HasAttribute(SpellAttr6.IgnorePhaseShift) && !caster.Visibility.CanSeeOrDetect(target, implicitCast))
            return SpellCastResult.BadTargets;

        var unitTarget = target.AsUnit;

        // creature/player specific target checks
        if (unitTarget != null)
        {
            // spells cannot be cast if target has a pet in combat either
            if (HasAttribute(SpellAttr1.OnlyPeacefulTargets) && (unitTarget.IsInCombat || unitTarget.HasUnitFlag(UnitFlags.PetInCombat)))
                return SpellCastResult.TargetAffectingCombat;

            // only spells with SPELL_ATTR3_ONLY_TARGET_GHOSTS can target ghosts
            if (HasAttribute(SpellAttr3.OnlyOnGhosts) != unitTarget.HasAuraType(AuraType.Ghost))
                return HasAttribute(SpellAttr3.OnlyOnGhosts) ? SpellCastResult.TargetNotGhost : SpellCastResult.BadTargets;

            if (caster != unitTarget)
                if (caster.IsTypeId(TypeId.Player))
                {
                    // Do not allow these spells to target creatures not tapped by us (Banish, Polymorph, many quest spells)
                    if (HasAttribute(SpellAttr2.CannotCastOnTapped))
                    {
                        var targetCreature = unitTarget.AsCreature;

                        if (targetCreature != null)
                            if (targetCreature.HasLootRecipient && !targetCreature.IsTappedBy(caster.AsPlayer))
                                return SpellCastResult.CantCastOnTapped;
                    }

                    if (HasAttribute(SpellCustomAttributes.PickPocket))
                    {
                        var targetCreature = unitTarget.AsCreature;

                        if (targetCreature == null)
                            return SpellCastResult.BadTargets;

                        if (!targetCreature.CanHaveLoot || !_lootStoreBox.Pickpocketing.HaveLootFor(targetCreature.Template.PickPocketId))
                            return SpellCastResult.TargetNoPockets;
                    }

                    // Not allow disarm unarmed player
                    if (Mechanic == Mechanics.Disarm)
                    {
                        if (unitTarget.IsTypeId(TypeId.Player))
                        {
                            var player = unitTarget.AsPlayer;

                            if (player.GetWeaponForAttack(WeaponAttackType.BaseAttack) == null || !player.IsUseEquipedWeapon(true))
                                return SpellCastResult.TargetNoWeapons;
                        }
                        else if (unitTarget.GetVirtualItemId(0) == 0)
                            return SpellCastResult.TargetNoWeapons;
                    }
                }
        }
        // corpse specific target checks
        else if (target.IsTypeId(TypeId.Corpse))
        {
            var corpseTarget = target.AsCorpse;

            // cannot target bare bones
            if (corpseTarget.CorpseType == CorpseType.Bones)
                return SpellCastResult.BadTargets;

            // we have to use owner for some checks (aura preventing resurrection for example)
            var owner = _objectAccessor.FindPlayer(corpseTarget.OwnerGUID);

            if (owner != null)
                unitTarget = owner;
            // we're not interested in corpses without owner
            else
                return SpellCastResult.BadTargets;
        }
        // other types of objects - always valid
        else
            return SpellCastResult.SpellCastOk;

        // corpseOwner and unit specific target checks
        if (!unitTarget.IsPlayer)
        {
            if (HasAttribute(SpellAttr3.OnlyOnPlayer))
                return SpellCastResult.TargetNotPlayer;

            if (HasAttribute(SpellAttr5.NotOnPlayerControlledNpc) && unitTarget.ControlledByPlayer)
                return SpellCastResult.TargetIsPlayerControlled;
        }
        else if (HasAttribute(SpellAttr5.NotOnPlayer))
            return SpellCastResult.TargetIsPlayer;

        if (!IsAllowingDeadTarget && !unitTarget.IsAlive)
            return SpellCastResult.TargetsDead;

        // check this Id only for implicit targets (chain and area), allow to explicitly target units for spells like Shield of Righteousness
        if (implicitCast && HasAttribute(SpellAttr6.DoNotChainToCrowdControlledTargets) && !unitTarget.CanFreeMove())
            return SpellCastResult.BadTargets;

        if (!CheckTargetCreatureType(unitTarget))
        {
            if (target.IsTypeId(TypeId.Player))
                return SpellCastResult.TargetIsPlayer;

            return SpellCastResult.BadTargets;
        }

        // check GM mode and GM invisibility - only for player casts (npc casts are controlled by AI) and negative spells
        if (unitTarget != caster && (caster.AffectingPlayer != null || !IsPositive) && unitTarget.IsTypeId(TypeId.Player))
        {
            if (!unitTarget.AsPlayer.IsVisible())
                return SpellCastResult.BmOrInvisgod;

            if (unitTarget.AsPlayer.IsGameMaster)
                return SpellCastResult.BmOrInvisgod;
        }

        // not allow casting on flying player
        if (unitTarget.HasUnitState(UnitState.InFlight) && !HasAttribute(SpellCustomAttributes.AllowInflightTarget))
            return SpellCastResult.BadTargets;

        /* TARGET_UNIT_MASTER gets blocked here for passengers, because the whole idea of this check is to
        not allow passengers to be implicitly hit by spells, however this target type should be an exception,
        if this is left it kills spells that award kill credit from vehicle to master (few spells),
        the use of these 2 covers passenger target check, logically, if vehicle cast this to master it should always hit
        him, because it would be it's passenger, there's no such case where this gets to fail legitimacy, this problem
        cannot be solved from within the check in other way since target type cannot be called for the spell currently
        Spell examples: [ID - 52864 Devour Water, ID - 52862 Devour Wind, ID - 49370 Wyrmrest Defender: Destabilize Azure Dragonshrine Effect] */
        var unitCaster = caster.AsUnit;

        if (unitCaster != null)
            if (!unitCaster.IsVehicle && unitCaster.CharmerOrOwner != target)
            {
                if (TargetAuraState != 0 && !unitTarget.HasAuraState(TargetAuraState, this, unitCaster))
                    return SpellCastResult.TargetAurastate;

                if (ExcludeTargetAuraState != 0 && unitTarget.HasAuraState(ExcludeTargetAuraState, this, unitCaster))
                    return SpellCastResult.TargetAurastate;
            }

        if (TargetAuraSpell != 0 && !unitTarget.HasAura(TargetAuraSpell))
            return SpellCastResult.TargetAurastate;

        if (ExcludeTargetAuraSpell != 0 && unitTarget.HasAura(ExcludeTargetAuraSpell))
            return SpellCastResult.TargetAurastate;

        if (unitTarget.HasAuraType(AuraType.PreventResurrection) && !HasAttribute(SpellAttr7.BypassNoResurrectAura))
            if (HasEffect(SpellEffectName.SelfResurrect) || HasEffect(SpellEffectName.Resurrect))
                return SpellCastResult.TargetCannotBeResurrected;

        if (!HasAttribute(SpellAttr8.BattleResurrection))
            return SpellCastResult.SpellCastOk;

        var map = caster.Location.Map;

        var iMap = map?.ToInstanceMap;

        var instance = iMap?.InstanceScript;

        if (instance == null)
            return SpellCastResult.SpellCastOk;

        if (instance.GetCombatResurrectionCharges() == 0 && instance.IsEncounterInProgress())
            return SpellCastResult.TargetCannotBeResurrected;

        return SpellCastResult.SpellCastOk;
    }

    public bool CheckTargetCreatureType(Unit target)
    {
        // Curse of Doom & Exorcism: not find another way to fix spell target check :/
        if (SpellFamilyName == SpellFamilyNames.Warlock && Category == 1179)
            // not allow cast at player
            return !target.IsTypeId(TypeId.Player);

        // if target is magnet (i.e Grounding Totem) the check is skipped
        if (target.IsMagnet)
            return true;

        var creatureType = target.CreatureTypeMask;

        return TargetCreatureType == 0 || creatureType == 0 || Convert.ToBoolean(creatureType & TargetCreatureType);
    }

    public SpellCastResult CheckVehicle(Unit caster)
    {
        // All creatures should be able to cast as passengers freely, restriction and attribute are only for players
        if (!caster.IsTypeId(TypeId.Player))
            return SpellCastResult.SpellCastOk;

        var vehicle = caster.Vehicle;

        if (vehicle == null)
            return SpellCastResult.SpellCastOk;

        VehicleSeatFlags checkMask = 0;

        foreach (var effectInfo in Effects)
            if (effectInfo.IsAuraType(AuraType.ModShapeshift))
            {
                var shapeShiftFromEntry = _cliDB.SpellShapeshiftFormStorage.LookupByKey((uint)effectInfo.MiscValue);

                if (shapeShiftFromEntry != null && !shapeShiftFromEntry.Flags.HasAnyFlag(SpellShapeshiftFormFlags.Stance))
                    checkMask |= VehicleSeatFlags.Uncontrolled;

                break;
            }

        if (HasAura(AuraType.Mounted))
            checkMask |= VehicleSeatFlags.CanCastMountSpell;

        if (checkMask == 0)
            checkMask = VehicleSeatFlags.CanAttack;

        var vehicleSeat = vehicle.GetSeatForPassenger(caster);

        if (!HasAttribute(SpellAttr6.AllowWhileRidingVehicle) && !HasAttribute(SpellAttr0.AllowWhileMounted) && (vehicleSeat.Flags & (int)checkMask) != (int)checkMask)
            return SpellCastResult.CantDoThatRightNow;

        // Can only summon uncontrolled minions/guardians when on controlled vehicle
        if (!vehicleSeat.HasFlag(VehicleSeatFlags.CanControl | VehicleSeatFlags.Unk2))
            return SpellCastResult.SpellCastOk;

        foreach (var effectInfo in Effects)
        {
            if (!effectInfo.IsEffectName(SpellEffectName.Summon))
                continue;

            var props = _cliDB.SummonPropertiesStorage.LookupByKey(effectInfo.MiscValueB);

            if (props != null && props.Control != SummonCategory.Wild)
                return SpellCastResult.CantDoThatRightNow;
        }

        return SpellCastResult.SpellCastOk;
    }

    public ulong GetAllEffectsMechanicMask()
    {
        ulong mask = 0;

        if (Mechanic != 0)
            mask |= 1ul << (int)Mechanic;

        foreach (var effectInfo in Effects)
            if (effectInfo.IsEffect && effectInfo.Mechanic != 0)
                mask |= 1ul << (int)effectInfo.Mechanic;

        return mask;
    }

    public WeaponAttackType GetAttackType()
    {
        WeaponAttackType result;

        switch (DmgClass)
        {
            case SpellDmgClass.Melee:
                result = HasAttribute(SpellAttr3.RequiresOffHandWeapon) ? WeaponAttackType.OffAttack : WeaponAttackType.BaseAttack;

                break;

            case SpellDmgClass.Ranged:
                result = IsRangedWeaponSpell ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack;

                break;

            default:
                // Wands
                result = IsAutoRepeatRangedSpell ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack;

                break;
        }

        return result;
    }

    public SpellInfo GetAuraRankForLevel(uint level)
    {
        // ignore passive spells
        if (IsPassive)
            return this;

        // Client ignores spell with these attributes (sub_53D9D0)
        if (HasAttribute(SpellAttr0.AuraIsDebuff) || HasAttribute(SpellAttr2.AllowLowLevelBuff) || HasAttribute(SpellAttr3.OnlyProcOnCaster))
            return this;

        var needRankSelection = false;

        foreach (var effectInfo in Effects)
            if (IsPositiveEffect(effectInfo.EffectIndex) &&
                (effectInfo.IsEffectName(SpellEffectName.ApplyAura) ||
                 effectInfo.IsEffectName(SpellEffectName.ApplyAreaAuraParty) ||
                 effectInfo.IsEffectName(SpellEffectName.ApplyAreaAuraRaid)) &&
                effectInfo.Scaling.Coefficient != 0)
            {
                needRankSelection = true;

                break;
            }

        // not required
        if (!needRankSelection)
            return this;

        for (var nextSpellInfo = this; nextSpellInfo != null; nextSpellInfo = nextSpellInfo.GetPrevRankSpell())
            // if found appropriate level
            if (level + 10 >= nextSpellInfo.SpellLevel)
                return nextSpellInfo;

        // one rank less then
        // not found
        return null;
    }

    public SpellEffectInfo GetEffect(int index)
    {
        return Effects[index];
    }

    public Mechanics GetEffectMechanic(int effIndex)
    {
        if (GetEffect(effIndex).IsEffect && GetEffect(effIndex).Mechanic != 0)
            return GetEffect(effIndex).Mechanic;

        return Mechanic != 0 ? Mechanic : Mechanics.None;
    }

    public ulong GetEffectMechanicMask(int effIndex)
    {
        ulong mask = 0;

        if (Mechanic != 0)
            mask |= 1ul << (int)Mechanic;

        if (GetEffect(effIndex).IsEffect && GetEffect(effIndex).Mechanic != 0)
            mask |= 1ul << (int)GetEffect(effIndex).Mechanic;

        return mask;
    }

    public float GetMaxRange(bool positive = false, WorldObject caster = null, Spell spell = null)
    {
        if (RangeEntry == null)
            return 0.0f;

        var range = RangeEntry.RangeMax[positive ? 1 : 0];

        var modOwner = caster?.SpellModOwner;

        modOwner?.ApplySpellMod(this, SpellModOp.Range, ref range, spell);

        return range;
    }

    public ulong GetMechanicImmunityMask(Unit caster)
    {
        var casterMechanicImmunityMask = caster.MechanicImmunityMask;
        ulong mechanicImmunityMask = 0;

        if (!CanBeInterrupted(null, caster, true))
            return mechanicImmunityMask;

        if ((casterMechanicImmunityMask & (1 << (int)Mechanics.Silence)) != 0)
            mechanicImmunityMask |= 1 << (int)Mechanics.Silence;

        if ((casterMechanicImmunityMask & (1 << (int)Mechanics.Interrupt)) != 0)
            mechanicImmunityMask |= 1 << (int)Mechanics.Interrupt;

        return mechanicImmunityMask;
    }

    public float GetMinRange(bool positive = false)
    {
        return RangeEntry == null ? 0.0f : RangeEntry.RangeMin[positive ? 1 : 0];
    }

    public ulong GetSpellMechanicMaskByEffectMask(HashSet<int> effectMask)
    {
        ulong mask = 0;

        if (Mechanic != 0)
            mask |= 1ul << (int)Mechanic;

        foreach (var effectInfo in Effects)
            if (effectMask.Contains(effectInfo.EffectIndex) && effectInfo.Mechanic != 0)
                mask |= 1ul << (int)effectInfo.Mechanic;

        return mask;
    }

    public uint GetSpellVisual(WorldObject caster = null, WorldObject viewer = null)
    {
        return _cliDB.SpellXSpellVisualStorage.TryGetValue(GetSpellXSpellVisualId(caster, viewer), out var visual) ? visual.SpellVisualID : 0;
    }

    public uint GetSpellXSpellVisualId(WorldObject caster = null, WorldObject viewer = null)
    {
        foreach (var visual in SpellVisuals)
        {
            if (_cliDB.PlayerConditionStorage.TryGetValue(visual.CasterPlayerConditionID, out var playerCondition))
                if (caster is not { IsPlayer: true } || !_conditionManager.IsPlayerMeetingCondition(caster.AsPlayer, playerCondition))
                    continue;

            if (!_cliDB.UnitConditionStorage.TryGetValue(visual.CasterUnitConditionID, out var unitCondition))
                return visual.Id;

            if (caster is not { IsUnit: true } || !_conditionManager.IsUnitMeetingCondition(caster.AsUnit, viewer?.AsUnit, unitCondition))
                continue;

            return visual.Id;
        }

        return 0;
    }

    public bool HasAttribute(SpellAttr0 attribute)
    {
        return Convert.ToBoolean(Attributes & attribute);
    }

    public bool HasAttribute(SpellAttr1 attribute)
    {
        return Convert.ToBoolean(AttributesEx & attribute);
    }

    public bool HasAttribute(SpellAttr2 attribute)
    {
        return Convert.ToBoolean(AttributesEx2 & attribute);
    }

    public bool HasAttribute(SpellAttr3 attribute)
    {
        return Convert.ToBoolean(AttributesEx3 & attribute);
    }

    public bool HasAttribute(SpellAttr4 attribute)
    {
        return Convert.ToBoolean(AttributesEx4 & attribute);
    }

    public bool HasAttribute(SpellAttr5 attribute)
    {
        return Convert.ToBoolean(AttributesEx5 & attribute);
    }

    public bool HasAttribute(SpellAttr6 attribute)
    {
        return Convert.ToBoolean(AttributesEx6 & attribute);
    }

    public bool HasAttribute(SpellAttr7 attribute)
    {
        return Convert.ToBoolean(AttributesEx7 & attribute);
    }

    public bool HasAttribute(SpellAttr8 attribute)
    {
        return Convert.ToBoolean(AttributesEx8 & attribute);
    }

    public bool HasAttribute(SpellAttr9 attribute)
    {
        return Convert.ToBoolean(AttributesEx9 & attribute);
    }

    public bool HasAttribute(SpellAttr10 attribute)
    {
        return Convert.ToBoolean(AttributesEx10 & attribute);
    }

    public bool HasAttribute(SpellAttr11 attribute)
    {
        return Convert.ToBoolean(AttributesEx11 & attribute);
    }

    public bool HasAttribute(SpellAttr12 attribute)
    {
        return Convert.ToBoolean(AttributesEx12 & attribute);
    }

    public bool HasAttribute(SpellAttr13 attribute)
    {
        return Convert.ToBoolean(AttributesEx13 & attribute);
    }

    public bool HasAttribute(SpellAttr14 attribute)
    {
        return Convert.ToBoolean(AttributesEx14 & attribute);
    }

    public bool HasAttribute(SpellCustomAttributes attribute)
    {
        return Convert.ToBoolean(AttributesCu & attribute);
    }

    public bool HasAura(AuraType aura)
    {
        return Effects.Any(effectInfo => effectInfo.IsAuraType(aura));
    }

    public bool HasAuraInterruptFlag(SpellAuraInterruptFlags flag)
    {
        return AuraInterruptFlags.HasAnyFlag(flag);
    }

    public bool HasAuraInterruptFlag(SpellAuraInterruptFlags2 flag)
    {
        return AuraInterruptFlags2.HasAnyFlag(flag);
    }

    public bool HasChannelInterruptFlag(SpellAuraInterruptFlags flag)
    {
        return ChannelInterruptFlags.HasAnyFlag(flag);
    }

    public bool HasChannelInterruptFlag(SpellAuraInterruptFlags2 flag)
    {
        return ChannelInterruptFlags2.HasAnyFlag(flag);
    }

    public bool HasEffect(SpellEffectName effect)
    {
        return Effects.Any(effectInfo => effectInfo.IsEffectName(effect));
    }

    public bool HasLabel(uint labelId)
    {
        return Labels.Contains(labelId);
    }

    public bool HasTargetType(Targets target)
    {
        return Effects.Any(effectInfo => effectInfo.TargetA.Target == target || effectInfo.TargetB.Target == target);
    }

    public void InitializeExplicitTargetMask()
    {
        var srcSet = false;
        var dstSet = false;
        var targetMask = Targets;

        // prepare target mask using effect target entries
        foreach (var effectInfo in Effects)
        {
            if (!effectInfo.IsEffect)
                continue;

            targetMask |= effectInfo.TargetA.GetExplicitTargetMask(ref srcSet, ref dstSet);
            targetMask |= effectInfo.TargetB.GetExplicitTargetMask(ref srcSet, ref dstSet);

            // add explicit target flags based on spell effects which have SpellEffectImplicitTargetTypes.Explicit and no valid target provided
            if (effectInfo.ImplicitTargetType != SpellEffectImplicitTargetTypes.Explicit)
                continue;

            // extend explicit target mask only if valid targets for effect could not be provided by target types
            var effectTargetMask = effectInfo.GetMissingTargetMask(srcSet, dstSet, targetMask);

            // don't add explicit object/dest flags when spell has no max range
            if (GetMaxRange(true) == 0.0f && GetMaxRange() == 0.0f)
                effectTargetMask &= ~(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.CorpseMask | SpellCastTargetFlags.DestLocation);

            targetMask |= effectTargetMask;
        }

        ExplicitTargetMask = targetMask;
    }

    public void InitializeSpellPositivity()
    {
        List<Tuple<SpellInfo, int>> visited = new();

        foreach (var effect in Effects.Where(effect => !IsPositiveEffectImpl(this, effect, visited)))
            NegativeEffects.Add(effect.EffectIndex);

        // additional checks after effects marked
        foreach (var spellEffectInfo in Effects.Where(spellEffectInfo => spellEffectInfo.IsEffect && IsPositiveEffect(spellEffectInfo.EffectIndex)))
            switch (spellEffectInfo.ApplyAuraName)
            {
                // has other non positive effect?
                // then it should be marked negative if has same target as negative effect (ex 8510, 8511, 8893, 10267)
                case AuraType.Dummy:
                case AuraType.ModStun:
                case AuraType.ModFear:
                case AuraType.ModTaunt:
                case AuraType.Transform:
                case AuraType.ModAttackspeed:
                case AuraType.ModDecreaseSpeed:
                {
                    for (var j = spellEffectInfo.EffectIndex + 1; j < Effects.Count; ++j)
                        if (!IsPositiveEffect(j) && spellEffectInfo.TargetA.Target == GetEffect(j).TargetA.Target && spellEffectInfo.TargetB.Target == GetEffect(j).TargetB.Target)
                            NegativeEffects.Add(spellEffectInfo.EffectIndex);

                    break;
                }
            }
    }

    public bool IsAbilityOfSkillType(SkillType skillType)
    {
        var bounds = _spellManager.GetSkillLineAbilityMapBounds(Id);

        return bounds.Any(spellIdx => spellIdx.SkillLine == (uint)skillType);
    }

    public bool IsAffected(SpellFamilyNames familyName, FlagArray128 familyFlags)
    {
        if (familyName == 0)
            return true;

        if (familyName != SpellFamilyName)
            return false;

        return !familyFlags || familyFlags & SpellFamilyFlags;
    }

    public bool IsAffectedBySpellMod(SpellModifier mod)
    {
        if (!IsAffectedBySpellMods)
            return false;

        var affectSpell = _spellManager.GetSpellInfo(mod.SpellId, Difficulty);

        if (affectSpell == null)
            return false;

        return mod.Type switch
        {
            SpellModType.Flat =>
                // TEMP: dont use IsAffected - !familyName and !familyFlags are not valid options for spell mods
                // TODO: investigate if the !familyName and !familyFlags conditions are even valid for all other (nonmod) uses of SpellInfo::IsAffected
                affectSpell.SpellFamilyName == SpellFamilyName && (mod as SpellModifierByClassMask)?.Mask & SpellFamilyFlags,
            SpellModType.Pct =>
                // TEMP: dont use IsAffected - !familyName and !familyFlags are not valid options for spell mods
                // TODO: investigate if the !familyName and !familyFlags conditions are even valid for all other (nonmod) uses of SpellInfo::IsAffected
                affectSpell.SpellFamilyName == SpellFamilyName && (mod as SpellModifierByClassMask)?.Mask & SpellFamilyFlags,
            SpellModType.LabelFlat => HasLabel((uint)((SpellFlatModifierByLabel)mod).Value.LabelID),
            SpellModType.LabelPct => HasLabel((uint)((SpellPctModifierByLabel)mod).Value.LabelID),
            _ => false
        };
    }

    public bool IsAuraExclusiveBySpecificPerCasterWith(SpellInfo spellInfo)
    {
        var spellSpec = SpellSpecific;

        return spellSpec switch
        {
            SpellSpecificType.Seal => spellSpec == spellInfo.SpellSpecific,
            SpellSpecificType.Hand => spellSpec == spellInfo.SpellSpecific,
            SpellSpecificType.Aura => spellSpec == spellInfo.SpellSpecific,
            SpellSpecificType.Sting => spellSpec == spellInfo.SpellSpecific,
            SpellSpecificType.Curse => spellSpec == spellInfo.SpellSpecific,
            SpellSpecificType.Bane => spellSpec == spellInfo.SpellSpecific,
            SpellSpecificType.Aspect => spellSpec == spellInfo.SpellSpecific,
            SpellSpecificType.WarlockCorruption => spellSpec == spellInfo.SpellSpecific,
            _ => false
        };
    }

    public bool IsAuraExclusiveBySpecificWith(SpellInfo spellInfo)
    {
        var spellSpec1 = SpellSpecific;
        var spellSpec2 = spellInfo.SpellSpecific;

        return spellSpec1 switch
        {
            SpellSpecificType.WarlockArmor => spellSpec1 == spellSpec2,
            SpellSpecificType.MageArmor => spellSpec1 == spellSpec2,
            SpellSpecificType.ElementalShield => spellSpec1 == spellSpec2,
            SpellSpecificType.MagePolymorph => spellSpec1 == spellSpec2,
            SpellSpecificType.Presence => spellSpec1 == spellSpec2,
            SpellSpecificType.Charm => spellSpec1 == spellSpec2,
            SpellSpecificType.Scroll => spellSpec1 == spellSpec2,
            SpellSpecificType.WarriorEnrage => spellSpec1 == spellSpec2,
            SpellSpecificType.MageArcaneBrillance => spellSpec1 == spellSpec2,
            SpellSpecificType.PriestDivineSpirit => spellSpec1 == spellSpec2,
            SpellSpecificType.Food => spellSpec2 is SpellSpecificType.Food or SpellSpecificType.FoodAndDrink,
            SpellSpecificType.Drink => spellSpec2 is SpellSpecificType.Drink or SpellSpecificType.FoodAndDrink,
            SpellSpecificType.FoodAndDrink => spellSpec2 is SpellSpecificType.Food or SpellSpecificType.Drink or SpellSpecificType.FoodAndDrink,
            _ => false
        };
    }

    public bool IsDifferentRankOf(SpellInfo spellInfo)
    {
        return Id != spellInfo.Id && IsRankOf(spellInfo);
    }

    public bool IsHighRankOf(SpellInfo spellInfo)
    {
        if (ChainEntry == null || spellInfo.ChainEntry == null)
            return false;

        if (ChainEntry.First != spellInfo.ChainEntry.First)
            return false;

        return ChainEntry.Rank > spellInfo.ChainEntry.Rank;
    }

    public bool IsItemFitToSpellRequirements(Item item)
    {
        // item neutral spell
        if (EquippedItemClass == ItemClass.None)
            return true;

        // item dependent spell
        return item != null && item.IsFitToSpellRequirements(this);
    }

    public bool IsPositiveEffect(int effIndex)
    {
        return !NegativeEffects.Contains(effIndex);
    }

    public bool IsPositiveTarget(SpellEffectInfo effect)
    {
        if (!effect.IsEffect)
            return true;

        return effect.TargetA.CheckType != SpellTargetCheckTypes.Enemy &&
               effect.TargetB.CheckType != SpellTargetCheckTypes.Enemy;
    }

    public bool IsRankOf(SpellInfo spellInfo)
    {
        return FirstRankSpell == spellInfo.FirstRankSpell;
    }

    public bool IsSingleTarget()
    {
        // all other single target spells have if it has AttributesEx5
        return HasAttribute(SpellAttr5.LimitN);
    }

    public bool MeetsFutureSpellPlayerCondition(Player player)
    {
        if (ShowFutureSpellPlayerConditionId == 0)
            return false;

        var playerCondition = _cliDB.PlayerConditionStorage.LookupByKey(ShowFutureSpellPlayerConditionId);

        return playerCondition == null || _conditionManager.IsPlayerMeetingCondition(player, playerCondition);
    }

    public bool NeedsToBeTriggeredByCaster(SpellInfo triggeringSpell)
    {
        if (NeedsExplicitUnitTarget)
            return true;

        if (!triggeringSpell.IsChanneled)
            return false;

        SpellCastTargetFlags mask = 0;

        foreach (var effectInfo in Effects)
            if (effectInfo.TargetA.Target != Framework.Constants.Targets.UnitCaster && effectInfo.TargetA.Target != Framework.Constants.Targets.DestCaster && effectInfo.TargetB.Target != Framework.Constants.Targets.UnitCaster && effectInfo.TargetB.Target != Framework.Constants.Targets.DestCaster)
                mask |= effectInfo.ProvidedTargetMask;

        return mask.HasAnyFlag(SpellCastTargetFlags.UnitMask);
    }

    public bool SpellCancelsAuraEffect(AuraEffect aurEff)
    {
        if (!HasAttribute(SpellAttr1.ImmunityPurgesEffect))
            return false;

        if (aurEff.SpellInfo.HasAttribute(SpellAttr0.NoImmunities))
            return false;

        foreach (var effectInfo in Effects)
        {
            if (!effectInfo.IsEffectName(SpellEffectName.ApplyAura))
                continue;

            var miscValue = (uint)effectInfo.MiscValue;

            switch (effectInfo.ApplyAuraName)
            {
                case AuraType.StateImmunity:
                    if (miscValue != (uint)aurEff.AuraType)
                        continue;

                    break;

                case AuraType.SchoolImmunity:
                case AuraType.ModImmuneAuraApplySchool:
                    if (aurEff.SpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities) || !Convert.ToBoolean((uint)aurEff.SpellInfo.SchoolMask & miscValue))
                        continue;

                    break;

                case AuraType.DispelImmunity:
                    if (miscValue != (uint)aurEff.SpellInfo.Dispel)
                        continue;

                    break;

                case AuraType.MechanicImmunity:
                    if (miscValue != (uint)aurEff.SpellInfo.Mechanic)
                        if (miscValue != (uint)aurEff.SpellEffectInfo.Mechanic)
                            continue;

                    break;

                default:
                    continue;
            }

            return true;
        }

        return false;
    }

    public bool TryGetEffect(int index, out SpellEffectInfo spellEffectInfo)
    {
        spellEffectInfo = null;

        if (Effects.Count < index)
            return false;

        spellEffectInfo = Effects[index];

        return spellEffectInfo != null;
    }

    public void UnloadImplicitTargetConditionLists()
    {
        // find the same instances of ConditionList and delete them.
        foreach (var effectInfo in Effects)
        {
            var cur = effectInfo.ImplicitTargetConditions;

            if (cur == null)
                continue;

            for (var j = effectInfo.EffectIndex; j < Effects.Count; ++j)
            {
                var eff = Effects[j];

                if (eff.ImplicitTargetConditions == cur)
                    eff.ImplicitTargetConditions = null;
            }
        }
    }

    private double CalcPPMCritMod(SpellProcsPerMinuteModRecord mod, Unit caster)
    {
        var player = caster.AsPlayer;

        if (player == null)
            return 0.0f;

        double crit = player.ActivePlayerData.CritPercentage;
        double rangedCrit = player.ActivePlayerData.RangedCritPercentage;
        double spellCrit = player.ActivePlayerData.SpellCritPercentage;

        return mod.Param switch
        {
            1 => crit * mod.Coeff * 0.01f,
            2 => rangedCrit * mod.Coeff * 0.01f,
            3 => spellCrit * mod.Coeff * 0.01f,
            4 => Math.Min(Math.Min(crit, rangedCrit), spellCrit) * mod.Coeff * 0.01f,
            _ => 0.0f
        };
    }

    private double CalcPPMHasteMod(SpellProcsPerMinuteModRecord mod, Unit caster)
    {
        double haste = caster.UnitData.ModHaste;
        double rangedHaste = caster.UnitData.ModRangedHaste;
        double spellHaste = caster.UnitData.ModSpellHaste;
        double regenHaste = caster.UnitData.ModHasteRegen;

        return mod.Param switch
        {
            1 => (1.0f / haste - 1.0f) * mod.Coeff,
            2 => (1.0f / rangedHaste - 1.0f) * mod.Coeff,
            3 => (1.0f / spellHaste - 1.0f) * mod.Coeff,
            4 => (1.0f / regenHaste - 1.0f) * mod.Coeff,
            5 => (1.0f / Math.Min(Math.Min(Math.Min(haste, rangedHaste), spellHaste), regenHaste) - 1.0f) * mod.Coeff,
            _ => 0.0f
        };
    }

    private double CalcPPMItemLevelMod(SpellProcsPerMinuteModRecord mod, int itemLevel)
    {
        if (itemLevel == mod.Param)
            return 0.0f;

        double itemLevelPoints = _itemEnchantmentManager.GetRandomPropertyPoints((uint)itemLevel, ItemQuality.Rare, InventoryType.Chest, 0);
        double basePoints = _itemEnchantmentManager.GetRandomPropertyPoints(mod.Param, ItemQuality.Rare, InventoryType.Chest, 0);

        if (itemLevelPoints == basePoints)
            return 0.0f;

        return (itemLevelPoints / basePoints - 1.0f) * mod.Coeff;
    }

    private bool CanSpellProvideImmunityAgainstAura(SpellInfo auraSpellInfo)
    {
        if (auraSpellInfo == null)
            return false;

        foreach (var effectInfo in Effects)
        {
            if (!effectInfo.IsEffect)
                continue;

            var immuneInfo = effectInfo.ImmunityInfo;

            if (!auraSpellInfo.HasAttribute(SpellAttr1.ImmunityToHostileAndFriendlyEffects) && !auraSpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities))
            {
                var schoolImmunity = immuneInfo.SchoolImmuneMask;

                if (schoolImmunity != 0)
                    if (((uint)auraSpellInfo.SchoolMask & schoolImmunity) != 0)
                        return true;
            }

            var mechanicImmunity = immuneInfo.MechanicImmuneMask;

            if (mechanicImmunity != 0)
                if ((mechanicImmunity & (1ul << (int)auraSpellInfo.Mechanic)) != 0)
                    return true;

            var dispelImmunity = immuneInfo.DispelImmune;

            if (dispelImmunity != 0)
                if ((uint)auraSpellInfo.Dispel == dispelImmunity)
                    return true;

            var immuneToAllEffects = true;

            foreach (var auraSpellEffectInfo in auraSpellInfo.Effects)
            {
                if (!auraSpellEffectInfo.IsEffect)
                    continue;

                if (!immuneInfo.SpellEffectImmune.Contains(auraSpellEffectInfo.Effect))
                {
                    immuneToAllEffects = false;

                    break;
                }

                var mechanic = (uint)auraSpellEffectInfo.Mechanic;

                if (mechanic != 0)
                    if (!Convert.ToBoolean(immuneInfo.MechanicImmuneMask & (1ul << (int)mechanic)))
                    {
                        immuneToAllEffects = false;

                        break;
                    }

                if (auraSpellInfo.HasAttribute(SpellAttr3.AlwaysHit))
                    continue;

                var auraName = auraSpellEffectInfo.ApplyAuraName;

                if (auraName == 0)
                    continue;

                var isImmuneToAuraEffectApply = !immuneInfo.AuraTypeImmune.Contains(auraName);

                if (!isImmuneToAuraEffectApply && !auraSpellInfo.IsPositiveEffect(auraSpellEffectInfo.EffectIndex) && !auraSpellInfo.HasAttribute(SpellAttr2.NoSchoolImmunities))
                {
                    var applyHarmfulAuraImmunityMask = immuneInfo.ApplyHarmfulAuraImmuneMask;

                    if (applyHarmfulAuraImmunityMask != 0)
                        if (((uint)auraSpellInfo.SchoolMask & applyHarmfulAuraImmunityMask) != 0)
                            isImmuneToAuraEffectApply = true;
                }

                if (isImmuneToAuraEffectApply)
                    continue;

                immuneToAllEffects = false;

                break;
            }

            if (immuneToAllEffects)
                return true;
        }

        return false;
    }

    // TODO Pandaros - Move to Scripts.
    private DiminishingGroup DiminishingGroupCompute()
    {
        if (IsPositive)
            return DiminishingGroup.None;

        if (HasAura(AuraType.ModTaunt))
            return DiminishingGroup.Taunt;

        switch (Id)
        {
            case 20549:  // War Stomp (Racial - Tauren)
            case 24394:  // Intimidation
            case 118345: // Pulverize (Primal Earth Elemental)
            case 118905: // Static Charge (Capacitor Totem)
                return DiminishingGroup.Stun;

            case 107079: // Quaking Palm
                return DiminishingGroup.Incapacitate;

            case 155145: // Arcane Torrent (Racial - Blood Elf)
                return DiminishingGroup.Silence;

            case 108199: // Gorefiend's Grasp
            case 191244: // Sticky Bomb
                return DiminishingGroup.AOEKnockback;
        }

        // Explicit Diminishing Groups
        switch (SpellFamilyName)
        {
            case SpellFamilyNames.Generic:
                // Frost Tomb
                if (Id == 48400)
                    return DiminishingGroup.None;
                // Gnaw

                if (Id == 47481)
                    return DiminishingGroup.Stun;

                // ToC Icehowl Arctic Breath
                if (Id == 66689)
                    return DiminishingGroup.None;

                // Black Plague
                if (Id == 64155)
                    return DiminishingGroup.None;

                // Screams of the Dead (King Ymiron)
                if (Id == 51750)
                    return DiminishingGroup.None;

                // Crystallize (Keristrasza heroic)
                if (Id == 48179)
                    return DiminishingGroup.None;

                break;

            case SpellFamilyNames.Mage:
            {
                // Frost Nova -- 122
                if (SpellFamilyFlags[0].HasAnyFlag(0x40u))
                    return DiminishingGroup.Root;

                // Freeze (Water Elemental) -- 33395
                if (SpellFamilyFlags[2].HasAnyFlag(0x200u))
                    return DiminishingGroup.Root;

                // Dragon's Breath -- 31661
                if (SpellFamilyFlags[0].HasAnyFlag(0x800000u))
                    return DiminishingGroup.Incapacitate;

                // Polymorph -- 118
                if (SpellFamilyFlags[0].HasAnyFlag(0x1000000u))
                    return DiminishingGroup.Incapacitate;

                // Ring of Frost -- 82691
                if (SpellFamilyFlags[2].HasAnyFlag(0x40u))
                    return DiminishingGroup.Incapacitate;

                // Ice Nova -- 157997
                if (SpellFamilyFlags[2].HasAnyFlag(0x800000u))
                    return DiminishingGroup.Incapacitate;

                break;
            }
            case SpellFamilyNames.Warrior:
            {
                // Shockwave -- 132168
                if (SpellFamilyFlags[1].HasAnyFlag(0x8000u))
                    return DiminishingGroup.Stun;

                // Storm Bolt -- 132169
                if (SpellFamilyFlags[2].HasAnyFlag(0x1000u))
                    return DiminishingGroup.Stun;

                // Intimidating Shout -- 5246
                if (SpellFamilyFlags[0].HasAnyFlag(0x40000u))
                    return DiminishingGroup.Disorient;

                break;
            }
            case SpellFamilyNames.Warlock:
            {
                // Mortal Coil -- 6789
                if (SpellFamilyFlags[0].HasAnyFlag(0x80000u))
                    return DiminishingGroup.Incapacitate;

                // Banish -- 710
                if (SpellFamilyFlags[1].HasAnyFlag(0x8000000u))
                    return DiminishingGroup.Incapacitate;

                // Fear -- 118699
                if (SpellFamilyFlags[1].HasAnyFlag(0x400u))
                    return DiminishingGroup.Disorient;

                // Howl of Terror -- 5484
                if (SpellFamilyFlags[1].HasAnyFlag(0x8u))
                    return DiminishingGroup.Disorient;

                // Shadowfury -- 30283
                if (SpellFamilyFlags[1].HasAnyFlag(0x1000u))
                    return DiminishingGroup.Stun;

                // Summon Infernal -- 22703
                if (SpellFamilyFlags[0].HasAnyFlag(0x1000u))
                    return DiminishingGroup.Stun;

                // 170995 -- Cripple
                if (Id == 170995)
                    return DiminishingGroup.LimitOnly;

                break;
            }
            case SpellFamilyNames.WarlockPet:
            {
                // Fellash -- 115770
                // Whiplash -- 6360
                if (SpellFamilyFlags[0].HasAnyFlag(0x8000000u))
                    return DiminishingGroup.AOEKnockback;

                // Mesmerize (Shivarra pet) -- 115268
                // Seduction (Succubus pet) -- 6358
                if (SpellFamilyFlags[0].HasAnyFlag(0x2000000u))
                    return DiminishingGroup.Disorient;

                // Axe Toss (Felguard pet) -- 89766
                if (SpellFamilyFlags[1].HasAnyFlag(0x4u))
                    return DiminishingGroup.Stun;

                break;
            }
            case SpellFamilyNames.Druid:
            {
                // Maim -- 22570
                if (SpellFamilyFlags[1].HasAnyFlag(0x80u))
                    return DiminishingGroup.Stun;

                // Mighty Bash -- 5211
                if (SpellFamilyFlags[0].HasAnyFlag(0x2000u))
                    return DiminishingGroup.Stun;

                // Rake -- 163505 -- no flags on the stun
                if (Id == 163505)
                    return DiminishingGroup.Stun;

                // Incapacitating Roar -- 99, no flags on the stun, 14
                if (SpellFamilyFlags[1].HasAnyFlag(0x1u))
                    return DiminishingGroup.Incapacitate;

                // Cyclone -- 33786
                if (SpellFamilyFlags[1].HasAnyFlag(0x20u))
                    return DiminishingGroup.Disorient;

                // Solar Beam -- 81261
                if (Id == 81261)
                    return DiminishingGroup.Silence;

                // Typhoon -- 61391
                if (SpellFamilyFlags[1].HasAnyFlag(0x1000000u))
                    return DiminishingGroup.AOEKnockback;

                // Ursol's Vortex -- 118283, no family flags
                if (Id == 118283)
                    return DiminishingGroup.AOEKnockback;

                // Entangling Roots -- 339
                if (SpellFamilyFlags[0].HasAnyFlag(0x200u))
                    return DiminishingGroup.Root;

                // Mass Entanglement -- 102359
                if (SpellFamilyFlags[2].HasAnyFlag(0x4u))
                    return DiminishingGroup.Root;

                break;
            }
            case SpellFamilyNames.Rogue:
            {
                // Between the Eyes -- 199804
                if (SpellFamilyFlags[0].HasAnyFlag(0x800000u))
                    return DiminishingGroup.Stun;

                // Cheap Shot -- 1833
                if (SpellFamilyFlags[0].HasAnyFlag(0x400u))
                    return DiminishingGroup.Stun;

                // Kidney Shot -- 408
                if (SpellFamilyFlags[0].HasAnyFlag(0x200000u))
                    return DiminishingGroup.Stun;

                // Gouge -- 1776
                if (SpellFamilyFlags[0].HasAnyFlag(0x8u))
                    return DiminishingGroup.Incapacitate;

                // Sap -- 6770
                if (SpellFamilyFlags[0].HasAnyFlag(0x80u))
                    return DiminishingGroup.Incapacitate;

                // Blind -- 2094
                if (SpellFamilyFlags[0].HasAnyFlag(0x1000000u))
                    return DiminishingGroup.Disorient;

                // Garrote -- 1330
                if (SpellFamilyFlags[1].HasAnyFlag(0x20000000u))
                    return DiminishingGroup.Silence;

                break;
            }
            case SpellFamilyNames.Hunter:
            {
                // Charge (Tenacity pet) -- 53148, no flags
                if (Id is 53148 or 200108 or 212638)
                    return DiminishingGroup.Root;

                // Ranger's Net -- 200108
                // Tracker's Net -- 212638

                // Binding Shot -- 117526, no flags
                if (Id == 117526)
                    return DiminishingGroup.Stun;

                // Freezing Trap -- 3355
                if (SpellFamilyFlags[0].HasAnyFlag(0x8u))
                    return DiminishingGroup.Incapacitate;

                // Wyvern Sting -- 19386
                if (SpellFamilyFlags[1].HasAnyFlag(0x1000u))
                    return DiminishingGroup.Incapacitate;

                // Bursting Shot -- 224729
                if (SpellFamilyFlags[2].HasAnyFlag(0x40u))
                    return DiminishingGroup.Disorient;

                // Scatter Shot -- 213691
                if (SpellFamilyFlags[2].HasAnyFlag(0x8000u))
                    return DiminishingGroup.Disorient;

                // Spider Sting -- 202933
                if (Id == 202933)
                    return DiminishingGroup.Silence;

                break;
            }
            case SpellFamilyNames.Paladin:
            {
                // Repentance -- 20066
                if (SpellFamilyFlags[0].HasAnyFlag(0x4u))
                    return DiminishingGroup.Incapacitate;

                // Blinding Light -- 105421
                if (Id == 105421)
                    return DiminishingGroup.Disorient;

                // Avenger's Shield -- 31935
                if (SpellFamilyFlags[0].HasAnyFlag(0x4000u))
                    return DiminishingGroup.Silence;

                // Hammer of Justice -- 853
                if (SpellFamilyFlags[0].HasAnyFlag(0x800u))
                    return DiminishingGroup.Stun;

                break;
            }
            case SpellFamilyNames.Shaman:
            {
                // Hex -- 51514
                // Hex -- 196942 (Voodoo Totem)
                if (SpellFamilyFlags[1].HasAnyFlag(0x8000u))
                    return DiminishingGroup.Incapacitate;

                // Thunderstorm -- 51490
                if (SpellFamilyFlags[1].HasAnyFlag(0x2000u))
                    return DiminishingGroup.AOEKnockback;

                // Earthgrab Totem -- 64695
                if (SpellFamilyFlags[2].HasAnyFlag(0x4000u))
                    return DiminishingGroup.Root;

                // Lightning Lasso -- 204437
                if (SpellFamilyFlags[3].HasAnyFlag(0x2000000u))
                    return DiminishingGroup.Stun;

                break;
            }
            case SpellFamilyNames.Deathknight:
            {
                // Chains of Ice -- 96294
                if (Id == 96294)
                    return DiminishingGroup.Root;

                // Blinding Sleet -- 207167
                if (Id == 207167)
                    return DiminishingGroup.Disorient;

                // Strangulate -- 47476
                if (SpellFamilyFlags[0].HasAnyFlag(0x200u))
                    return DiminishingGroup.Silence;

                // Asphyxiate -- 108194
                if (SpellFamilyFlags[2].HasAnyFlag(0x100000u))
                    return DiminishingGroup.Stun;

                // Gnaw (Ghoul) -- 91800, no flags
                if (Id is 91800 or 91797 or 207171)
                    return DiminishingGroup.Stun;

                // Monstrous Blow (Ghoul w/ Dark Transformation active) -- 91797

                // Winter is Coming -- 207171

                break;
            }
            case SpellFamilyNames.Priest:
            {
                // Holy Word: Chastise -- 200200
                if (SpellFamilyFlags[2].HasAnyFlag(0x20u) && GetSpellVisual() == 52021)
                    return DiminishingGroup.Stun;

                // Mind Bomb -- 226943
                if (Id == 226943)
                    return DiminishingGroup.Stun;

                // Mind Control -- 605
                if (SpellFamilyFlags[0].HasAnyFlag(0x20000u) && GetSpellVisual() == 39068)
                    return DiminishingGroup.Incapacitate;

                // Holy Word: Chastise -- 200196
                if (SpellFamilyFlags[2].HasAnyFlag(0x20u) && GetSpellVisual() == 52019)
                    return DiminishingGroup.Incapacitate;

                // Psychic Scream -- 8122
                if (SpellFamilyFlags[0].HasAnyFlag(0x10000u))
                    return DiminishingGroup.Disorient;

                // Silence -- 15487
                if (SpellFamilyFlags[1].HasAnyFlag(0x200000u) && GetSpellVisual() == 39025)
                    return DiminishingGroup.Silence;

                // Shining Force -- 204263
                if (Id == 204263)
                    return DiminishingGroup.AOEKnockback;

                break;
            }
            case SpellFamilyNames.Monk:
            {
                // Disable -- 116706, no flags
                if (Id == 116706)
                    return DiminishingGroup.Root;

                // Fists of Fury -- 120086
                if (SpellFamilyFlags[1].HasAnyFlag(0x800000u) && !SpellFamilyFlags[2].HasAnyFlag(0x8u))
                    return DiminishingGroup.Stun;

                // Leg Sweep -- 119381
                if (SpellFamilyFlags[1].HasAnyFlag(0x200u))
                    return DiminishingGroup.Stun;

                // Incendiary Breath (honor talent) -- 202274, no flags
                if (Id == 202274)
                    return DiminishingGroup.Incapacitate;

                // Paralysis -- 115078
                if (SpellFamilyFlags[2].HasAnyFlag(0x800000u))
                    return DiminishingGroup.Incapacitate;

                // Song of Chi-Ji -- 198909
                if (Id == 198909)
                    return DiminishingGroup.Disorient;

                break;
            }
            case SpellFamilyNames.DemonHunter:
                switch (Id)
                {
                    case 179057: // Chaos Nova
                    case 211881: // Fel Eruption
                    case 200166: // Metamorphosis
                    case 205630: // Illidan's Grasp
                        return DiminishingGroup.Stun;

                    case 217832: // Imprison
                    case 221527: // Imprison
                        return DiminishingGroup.Incapacitate;
                }

                break;
        }

        return DiminishingGroup.None;
    }

    private int DiminishingLimitDurationCompute()
    {
        // Explicit diminishing duration
        switch (SpellFamilyName)
        {
            case SpellFamilyNames.Mage:
                // Dragon's Breath - 3 seconds in PvP
                if (SpellFamilyFlags[0].HasAnyFlag(0x800000u))
                    return 3 * Time.IN_MILLISECONDS;

                break;

            case SpellFamilyNames.Warlock:
                // Cripple - 4 seconds in PvP
                if (Id == 170995)
                    return 4 * Time.IN_MILLISECONDS;

                break;

            case SpellFamilyNames.Hunter:
                // Binding Shot - 3 seconds in PvP
                if (Id == 117526)
                    return 3 * Time.IN_MILLISECONDS;

                // Wyvern Sting - 6 seconds in PvP
                if (SpellFamilyFlags[1].HasAnyFlag(0x1000u))
                    return 6 * Time.IN_MILLISECONDS;

                break;

            case SpellFamilyNames.Monk:
                // Paralysis - 4 seconds in PvP regardless of if they are facing you
                if (SpellFamilyFlags[2].HasAnyFlag(0x800000u))
                    return 4 * Time.IN_MILLISECONDS;

                break;

            case SpellFamilyNames.DemonHunter:
                switch (Id)
                {
                    case 217832: // Imprison
                    case 221527: // Imprison
                        return 4 * Time.IN_MILLISECONDS;
                }

                break;
        }

        return 8 * Time.IN_MILLISECONDS;
    }

    private DiminishingLevels DiminishingMaxLevelCompute(DiminishingGroup group)
    {
        return group switch
        {
            DiminishingGroup.Taunt => DiminishingLevels.TauntImmune,
            DiminishingGroup.AOEKnockback => DiminishingLevels.Level2,
            _ => DiminishingLevels.Immune
        };
    }

    private DiminishingReturnsType DiminishingTypeCompute(DiminishingGroup group)
    {
        return group switch
        {
            DiminishingGroup.Taunt => DiminishingReturnsType.All,
            DiminishingGroup.Stun => DiminishingReturnsType.All,
            DiminishingGroup.LimitOnly => DiminishingReturnsType.None,
            DiminishingGroup.None => DiminishingReturnsType.None,
            _ => DiminishingReturnsType.Player
        };
    }

    private SpellInfo GetPrevRankSpell()
    {
        return ChainEntry?.Prev;
    }

    private bool IsPositiveEffectImpl(SpellInfo spellInfo, SpellEffectInfo effect, List<Tuple<SpellInfo, int>> visited)
    {
        if (!effect.IsEffect)
            return true;

        // attribute may be already set in DB
        if (!spellInfo.IsPositiveEffect(effect.EffectIndex))
            return false;

        // passive auras like talents are all positive
        if (spellInfo.IsPassive)
            return true;

        // not found a single positive spell with this attribute
        if (spellInfo.HasAttribute(SpellAttr0.AuraIsDebuff))
            return false;

        if (spellInfo.HasAttribute(SpellAttr4.AuraIsBuff))
            return true;

        visited.Add(Tuple.Create(spellInfo, effect.EffectIndex));

        //We need scaling level info for some auras that compute bp 0 or positive but should be debuffs
        var bpScalePerLevel = effect.RealPointsPerLevel;
        var bp = effect.CalcValue();

        switch (spellInfo.SpellFamilyName)
        {
            case SpellFamilyNames.Generic:
                switch (spellInfo.Id)
                {
                    case 40268: // Spiritual Vengeance, Teron Gorefiend, Black Temple
                    case 61987: // Avenging Wrath Marker
                    case 61988: // Divine Shield exclude aura
                    case 64412: // Phase Punch, Algalon the Observer, Ulduar
                    case 72410: // Rune of Blood, Saurfang, Icecrown Citadel
                    case 71204: // Touch of Insignificance, Lady Deathwhisper, Icecrown Citadel
                        return false;

                    case 24732: // Bat Costume
                    case 30877: // Tag Murloc
                    case 61716: // Rabbit Costume
                    case 61734: // Noblegarden Bunny
                    case 62344: // Fists of Stone
                    case 50344: // Dream Funnel
                    case 61819: // Manabonked! (item)
                    case 61834: // Manabonked! (minigob)
                    case 73523: // Rigor Mortis
                        return true;
                }

                break;

            case SpellFamilyNames.Rogue:
                switch (spellInfo.Id)
                {
                    // Envenom must be considered as a positive effect even though it deals damage
                    case 32645: // Envenom
                        return true;

                    case 40251: // Shadow of Death, Teron Gorefiend, Black Temple
                        return false;
                }

                break;

            case SpellFamilyNames.Warrior:
                // Slam, Execute
                if ((spellInfo.SpellFamilyFlags[0] & 0x20200000) != 0)
                    return false;

                break;
        }

        switch (spellInfo.Mechanic)
        {
            case Mechanics.ImmuneShield:
                return true;
        }

        // Special case: effects which determine positivity of whole spell
        if (spellInfo.HasAttribute(SpellAttr1.AuraUnique))
            // check for targets, there seems to be an assortment of dummy triggering spells that should be negative
            foreach (var otherEffect in spellInfo.Effects)
                if (!IsPositiveTarget(otherEffect))
                    return false;

        foreach (var otherEffect in spellInfo.Effects)
        {
            switch (otherEffect.Effect)
            {
                case SpellEffectName.Heal:
                case SpellEffectName.LearnSpell:
                case SpellEffectName.SkillStep:
                case SpellEffectName.HealPct:
                    return true;

                case SpellEffectName.Instakill:
                    if (otherEffect.EffectIndex != effect.EffectIndex && // for spells like 38044: instakill effect is negative but auras on target must count as buff
                        otherEffect.TargetA.Target == effect.TargetA.Target &&
                        otherEffect.TargetB.Target == effect.TargetB.Target)
                        return false;

                    break;
            }

            if (otherEffect.IsAura)
                switch (otherEffect.ApplyAuraName)
                {
                    case AuraType.ModStealth:
                    case AuraType.ModUnattackable:
                        return true;

                    case AuraType.SchoolHealAbsorb:
                    case AuraType.Empathy:
                    case AuraType.ModSpellDamageFromCaster:
                    case AuraType.PreventsFleeing:
                        return false;
                }
        }

        switch (effect.Effect)
        {
            case SpellEffectName.WeaponDamage:
            case SpellEffectName.WeaponDamageNoSchool:
            case SpellEffectName.NormalizedWeaponDmg:
            case SpellEffectName.WeaponPercentDamage:
            case SpellEffectName.SchoolDamage:
            case SpellEffectName.EnvironmentalDamage:
            case SpellEffectName.HealthLeech:
            case SpellEffectName.Instakill:
            case SpellEffectName.PowerDrain:
            case SpellEffectName.StealBeneficialBuff:
            case SpellEffectName.InterruptCast:
            case SpellEffectName.Pickpocket:
            case SpellEffectName.GameObjectDamage:
            case SpellEffectName.DurabilityDamage:
            case SpellEffectName.DurabilityDamagePct:
            case SpellEffectName.ApplyAreaAuraEnemy:
            case SpellEffectName.Tamecreature:
            case SpellEffectName.Distract:
                return false;

            case SpellEffectName.Energize:
            case SpellEffectName.EnergizePct:
            case SpellEffectName.HealPct:
            case SpellEffectName.HealMaxHealth:
            case SpellEffectName.HealMechanical:
                return true;

            case SpellEffectName.KnockBack:
            case SpellEffectName.Charge:
            case SpellEffectName.PersistentAreaAura:
            case SpellEffectName.AttackMe:
            case SpellEffectName.PowerBurn:
                // check targets
                if (!IsPositiveTarget(effect))
                    return false;

                break;

            case SpellEffectName.Dispel:
                // non-positive dispel
                switch ((DispelType)effect.MiscValue)
                {
                    case DispelType.Stealth:
                    case DispelType.Invisibility:
                    case DispelType.Enrage:
                        return false;
                }

                // also check targets
                if (!IsPositiveTarget(effect))
                    return false;

                break;

            case SpellEffectName.DispelMechanic:
                if (!IsPositiveTarget(effect))
                    // non-positive mechanic dispel on negative target
                    switch ((Mechanics)effect.MiscValue)
                    {
                        case Mechanics.Bandage:
                        case Mechanics.Shield:
                        case Mechanics.Mount:
                        case Mechanics.Invulnerability:
                            return false;
                    }

                break;

            case SpellEffectName.Threat:
            case SpellEffectName.ModifyThreatPercent:
                // check targets AND basepoints
                if (!IsPositiveTarget(effect) && bp > 0)
                    return false;

                break;
        }

        if (effect.IsAura)
            // non-positive aura use
            switch (effect.ApplyAuraName)
            {
                case AuraType.ModStat: // dependent from basepoint sign (negative -> negative)
                case AuraType.ModSkill:
                case AuraType.ModSkill2:
                case AuraType.ModDodgePercent:
                case AuraType.ModHealingDone:
                case AuraType.ModDamageDoneCreature:
                case AuraType.ObsModHealth:
                case AuraType.ObsModPower:
                case AuraType.ModCritPct:
                case AuraType.ModHitChance:
                case AuraType.ModSpellHitChance:
                case AuraType.ModSpellCritChance:
                case AuraType.ModRangedHaste:
                case AuraType.ModMeleeRangedHaste:
                case AuraType.ModCastingSpeedNotStack:
                case AuraType.HasteSpells:
                case AuraType.ModRecoveryRateBySpellLabel:
                case AuraType.ModDetectRange:
                case AuraType.ModIncreaseHealthPercent:
                case AuraType.ModTotalStatPercentage:
                case AuraType.ModIncreaseSwimSpeed:
                case AuraType.ModPercentStat:
                case AuraType.ModIncreaseHealth:
                case AuraType.ModSpeedAlways:
                    if (bp < 0 || bpScalePerLevel < 0) //TODO: What if both are 0? Should it be a buff or debuff?
                        return false;

                    break;

                case AuraType.ModAttackspeed: // some buffs have negative bp, check both target and bp
                case AuraType.ModMeleeHaste:
                case AuraType.ModDamageDone:
                case AuraType.ModResistance:
                case AuraType.ModResistancePct:
                case AuraType.ModRating:
                case AuraType.ModAttackPower:
                case AuraType.ModRangedAttackPower:
                case AuraType.ModDamagePercentDone:
                case AuraType.ModSpeedSlowAll:
                case AuraType.MeleeSlow:
                case AuraType.ModAttackPowerPct:
                case AuraType.ModHealingDonePercent:
                case AuraType.ModHealingPct:
                    if (!IsPositiveTarget(effect) || bp < 0)
                        return false;

                    break;

                case AuraType.ModDamageTaken: // dependent from basepoint sign (positive . negative)
                case AuraType.ModMeleeDamageTaken:
                case AuraType.ModMeleeDamageTakenPct:
                case AuraType.ModPowerCostSchool:
                case AuraType.ModPowerCostSchoolPct:
                case AuraType.ModMechanicDamageTakenPercent:
                    if (bp > 0)
                        return false;

                    break;

                case AuraType.ModDamagePercentTaken: // check targets and basepoints (ex Recklessness)
                    if (!IsPositiveTarget(effect) && bp > 0)
                        return false;

                    break;

                case AuraType.ModHealthRegenPercent: // check targets and basepoints (target enemy and negative bp -> negative)
                    if (!IsPositiveTarget(effect) && bp < 0)
                        return false;

                    break;

                case AuraType.AddTargetTrigger:
                    return true;

                case AuraType.PeriodicTriggerSpellWithValue:
                case AuraType.PeriodicTriggerSpellFromClient:
                    var spellTriggeredProto = _spellManager.GetSpellInfo(effect.TriggerSpell, spellInfo.Difficulty);

                    if (spellTriggeredProto != null)
                        // negative targets of main spell return early
                        foreach (var spellTriggeredEffect in spellTriggeredProto.Effects)
                        {
                            // already seen this
                            if (visited.Contains(Tuple.Create(spellTriggeredProto, spellTriggeredEffect.EffectIndex)))
                                continue;

                            if (!spellTriggeredEffect.IsEffect)
                                continue;

                            // if non-positive trigger cast targeted to positive target this main cast is non-positive
                            // this will place this spell auras as debuffs
                            if (IsPositiveTarget(spellTriggeredEffect) && !IsPositiveEffectImpl(spellTriggeredProto, spellTriggeredEffect, visited))
                                return false;
                        }

                    break;

                case AuraType.PeriodicTriggerSpell:
                case AuraType.ModStun:
                case AuraType.Transform:
                case AuraType.ModDecreaseSpeed:
                case AuraType.ModFear:
                case AuraType.ModTaunt:
                // special auras: they may have non negative target but still need to be marked as debuff
                // checked again after all effects (SpellInfo::_InitializeSpellPositivity)
                case AuraType.ModPacify:
                case AuraType.ModPacifySilence:
                case AuraType.ModDisarm:
                case AuraType.ModDisarmOffhand:
                case AuraType.ModDisarmRanged:
                case AuraType.ModCharm:
                case AuraType.AoeCharm:
                case AuraType.ModPossess:
                case AuraType.ModLanguage:
                case AuraType.DamageShield:
                case AuraType.ProcTriggerSpell:
                case AuraType.ModAttackerMeleeHitChance:
                case AuraType.ModAttackerRangedHitChance:
                case AuraType.ModAttackerSpellHitChance:
                case AuraType.ModAttackerMeleeCritChance:
                case AuraType.ModAttackerRangedCritChance:
                case AuraType.ModAttackerSpellAndWeaponCritChance:
                case AuraType.Dummy:
                case AuraType.PeriodicDummy:
                case AuraType.ModHealing:
                case AuraType.ModWeaponCritPercent:
                case AuraType.PowerBurn:
                case AuraType.ModCooldown:
                case AuraType.ModChargeCooldown:
                case AuraType.ModIncreaseSpeed:
                case AuraType.ModParryPercent:
                case AuraType.SetVehicleId:
                case AuraType.PeriodicEnergize:
                case AuraType.EffectImmunity:
                case AuraType.OverrideClassScripts:
                case AuraType.ModShapeshift:
                case AuraType.ModThreat:
                case AuraType.ProcTriggerSpellWithValue:
                    // check target for positive and negative spells
                    if (!IsPositiveTarget(effect))
                        return false;

                    break;

                case AuraType.ModConfuse:
                case AuraType.ChannelDeathItem:
                case AuraType.ModRoot:
                case AuraType.ModRoot2:
                case AuraType.ModSilence:
                case AuraType.ModDetaunt:
                case AuraType.Ghost:
                case AuraType.ModLeech:
                case AuraType.PeriodicManaLeech:
                case AuraType.ModStalked:
                case AuraType.PreventResurrection:
                case AuraType.PeriodicDamage:
                case AuraType.PeriodicWeaponPercentDamage:
                case AuraType.PeriodicDamagePercent:
                case AuraType.MeleeAttackPowerAttackerBonus:
                case AuraType.RangedAttackPowerAttackerBonus:
                    return false;

                case AuraType.MechanicImmunity:
                {
                    // non-positive immunities
                    switch ((Mechanics)effect.MiscValue)
                    {
                        case Mechanics.Bandage:
                        case Mechanics.Shield:
                        case Mechanics.Mount:
                        case Mechanics.Invulnerability:
                            return false;
                    }

                    break;
                }
                case AuraType.AddFlatModifier: // mods
                case AuraType.AddPctModifier:
                case AuraType.AddFlatModifierBySpellLabel:
                case AuraType.AddPctModifierBySpellLabel:
                {
                    switch ((SpellModOp)effect.MiscValue)
                    {
                        case SpellModOp.ChangeCastTime: // dependent from basepoint sign (positive . negative)
                        case SpellModOp.Period:
                        case SpellModOp.PowerCostOnMiss:
                        case SpellModOp.StartCooldown:
                            if (bp > 0)
                                return false;

                            break;

                        case SpellModOp.Cooldown:
                        case SpellModOp.PowerCost0:
                        case SpellModOp.PowerCost1:
                        case SpellModOp.PowerCost2:
                            if (!spellInfo.IsPositive && bp > 0) // dependent on prev effects too (ex Arcane Power)
                                return false;

                            break;

                        case SpellModOp.PointsIndex0: // always positive
                        case SpellModOp.PointsIndex1:
                        case SpellModOp.PointsIndex2:
                        case SpellModOp.PointsIndex3:
                        case SpellModOp.PointsIndex4:
                        case SpellModOp.Points:
                        case SpellModOp.Hate:
                        case SpellModOp.ChainAmplitude:
                        case SpellModOp.Amplitude:
                            return true;

                        case SpellModOp.Duration:
                        case SpellModOp.CritChance:
                        case SpellModOp.HealingAndDamage:
                        case SpellModOp.ChainTargets:
                            if (!spellInfo.IsPositive && bp < 0) // dependent on prev effects too
                                return false;

                            break;

                        default: // dependent from basepoint sign (negative . negative)
                            if (bp < 0)
                                return false;

                            break;
                    }

                    break;
                }
            }

        // negative spell if triggered spell is negative
        if (effect.ApplyAuraName == 0 && effect.TriggerSpell != 0)
        {
            var spellTriggeredProto = _spellManager.GetSpellInfo(effect.TriggerSpell, spellInfo.Difficulty);

            if (spellTriggeredProto != null)
                // spells with at least one negative effect are considered negative
                // some self-applied spells have negative effects but in self casting case negative check ignored.
                foreach (var spellTriggeredEffect in spellTriggeredProto.Effects)
                {
                    // already seen this
                    if (visited.Contains(Tuple.Create(spellTriggeredProto, spellTriggeredEffect.EffectIndex)))
                        continue;

                    if (!spellTriggeredEffect.IsEffect)
                        continue;

                    if (!IsPositiveEffectImpl(spellTriggeredProto, spellTriggeredEffect, visited))
                        return false;
                }
        }

        // ok, positive
        return true;
    }

    public struct ScalingInfo
    {
        public uint MaxScalingLevel;
        public uint MinScalingLevel;
        public uint ScalesFromItemLevel;
    }
}