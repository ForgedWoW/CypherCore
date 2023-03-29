// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

public class SharedConst
{
	/// <summary>
	///     CliDB Const
	/// </summary>
	public const int GTMaxLevel = 100; // All Gt* DBC store data for 100 levels, some by 100 per class/race

    public const int GTMaxRating = 32; // gtOCTClassCombatRatingScalar.dbc stores data for 32 ratings, look at MAX_COMBAT_RATING for real used amount
    public const int ReputationCap = 42000;
    public const int ReputationBottom = -42000;
    public const int MaxClientMailItems = 12; // max number of items a player is allowed to attach
    public const int MaxMailItems = 16;
    public const int MaxDeclinedNameCases = 5;
    public const int MaxHolidayDurations = 10;
    public const int MaxHolidayDates = 26;
    public const int MaxHolidayFlags = 10;
    public const int DefaultMaxLevel = 70;
    public const int MaxLevel = 123;
    public const int StrongMaxLevel = 255;
    public const int MaxOverrideSpell = 10;
    public const int MaxWorldMapOverlayArea = 4;
    public const int MaxMountCapabilities = 24;
    public const int MaxLockCase = 8;
    public const int MaxAzeriteEmpoweredTier = 5;
    public const int MaxAzeriteEssenceSlot = 4;
    public const int MaxAzeriteEssenceRank = 4;
    public const int AchivementCategoryPetBattles = 15117;

	/// <summary>
	///     BattlePets Const
	/// </summary>
	public const int DefaultMaxBattlePetsPerSpecies = 3;

    public const int BattlePetCageItemId = 82800;
    public const int SpellVisualUncagePet = 222;

    public const int SpellBattlePetTraining = 125610;
    public const int SpellReviveBattlePets = 125439;
    public const int SpellSummonBattlePet = 118301;
    public const int MaxBattlePetLevel = 25;

	/// <summary>
	///     Lfg Const
	/// </summary>
	public const uint LFGTimeRolecheck = 45;

    public const uint LFGTimeBoot = 120;
    public const uint LFGTimeProposal = 45;
    public const uint LFGQueueUpdateInterval = 15 * Time.InMilliseconds;
    public const uint LFGSpellDungeonCooldown = 71328;
    public const uint LFGSpellDungeonDeserter = 71041;
    public const uint LFGSpellLuckOfTheDraw = 72221;
    public const uint LFGKickVotesNeeded = 3;
    public const byte LFGMaxKicks = 3;
    public const int LFGTanksNeeded = 1;
    public const int LFGHealersNeeded = 1;
    public const int LFGDPSNeeded = 3;

	/// <summary>
	///     Loot Const
	/// </summary>
	public const int MaxNRLootItems = 18;

    public const int PlayerCorpseLootEntry = 1;

	/// <summary>
	///     Movement Const
	/// </summary>
	public const double gravity = 19.29110527038574;

    public const float terminalVelocity = 60.148003f;
    public const float terminalSafefallVelocity = 7.0f;
    public const float terminal_length = (float)((terminalVelocity * terminalVelocity) / (2.0f * gravity));
    public const float terminal_safeFall_length = (float)((terminalSafefallVelocity * terminalSafefallVelocity) / (2.0f * gravity));
    public const float terminal_fallTime = (float)(terminalVelocity / gravity);                  // the time that needed to reach terminalVelocity
    public const float terminal_safeFall_fallTime = (float)(terminalSafefallVelocity / gravity); // the time that needed to reach terminalVelocity with safefall

	/// <summary>
	///     Vehicle Const
	/// </summary>
	public const int MaxSpellVehicle = 6;

    public const int VehicleSpellRideHardcoded = 46598;
    public const int VehicleSpellParachute = 45472;

	/// <summary>
	///     Quest Const
	/// </summary>
	public const int MaxQuestLogSize = 25;

    public const int MaxQuestCounts = 24;

    public const int QuestItemDropCount = 4;
    public const int QuestRewardChoicesCount = 6;
    public const int QuestRewardItemCount = 4;
    public const int QuestDeplinkCount = 10;
    public const int QuestRewardReputationsCount = 5;
    public const int QuestEmoteCount = 4;
    public const int QuestRewardCurrencyCount = 4;
    public const int QuestRewardDisplaySpellCount = 3;

	/// <summary>
	///     Smart AI Const
	/// </summary>
	public const int SmartEventParamCount = 4;

    public const int SmartActionParamCount = 7;
    public const uint SmartSummonCounter = 0xFFFFFF;
    public const uint SmartEscortTargets = 0xFFFFFF;

	/// <summary>
	///     BGs / Arena Const
	/// </summary>
	public const int PvpTeamsCount = 2;

    public const uint CountOfPlayersToAverageWaitTime = 10;
    public const uint MaxPlayerBGQueues = 2;
    public const uint BGAwardArenaPointsMinLevel = 71;
    public const int ArenaTimeLimitPointsLoss = -16;
    public const int MaxArenaSlot = 3;

	/// <summary>
	///     Void Storage Const
	/// </summary>
	public const uint VoidStorageUnlockCost = 100 * MoneyConstants.Gold;

    public const uint VoidStorageStoreItemCost = 10 * MoneyConstants.Gold;
    public const uint VoidStorageMaxDeposit = 9;
    public const uint VoidStorageMaxWithdraw = 9;
    public const byte VoidStorageMaxSlot = 160;

	/// <summary>
	///     Calendar Const
	/// </summary>
	public const uint CalendarMaxEvents = 30;

    public const uint CalendarMaxGuildEvents = 100;
    public const uint CalendarMaxInvites = 100;
    public const uint CalendarCreateEventCooldown = 5;
    public const uint CalendarOldEventsDeletionTime = 1 * Time.Month;
    public const uint CalendarDefaultResponseTime = 946684800; // 01/01/2000 00:00:00

	/// <summary>
	///     Misc Const
	/// </summary>
	public const Locale DefaultLocale = Locale.enUS;

    public const int MaxAccountTutorialValues = 8;
    public const int MinAuctionTime = (12 * Time.Hour);
    public const int MaxConditionTargets = 3;

	/// <summary>
	///     Unit Const
	/// </summary>
	public const float BaseMinDamage = 1.0f;

    public const float BaseMaxDamage = 2.0f;
    public const int BaseAttackTime = 2000;
    public const int MaxSummonSlot = 7;
    public const int MaxTotemSlot = 5;
    public const int MaxGameObjectSlot = 4;
    public const float MaxAggroRadius = 45.0f; // yards
    public const int MaxAggroResetTime = 10;
    public const int MaxVehicleSeats = 8;
    public const int AttackDisplayDelay = 200;
    public const float MaxPlayerStealthDetectRange = 30.0f; // max distance for detection targets by player
    public const int MaxEquipmentItems = 3;

	/// <summary>
	///     Creature Const
	/// </summary>
	public const int MaxGossipMenuItems = 64; // client supported items unknown, but provided number must be enough

    public const int DefaultGossipMessage = 0xFFFFFF;
    public const int MaxGossipTextEmotes = 3;
    public const int MaxNpcTextOptions = 8;
    public const int MaxCreatureBaseHp = 4;
    public const int MaxCreatureSpells = 8;
    public const byte MaxVendorItems = 150;
    public const int CreatureAttackRangeZ = 3;
    public const int MaxCreatureKillCredit = 2;
    public const int MaxCreatureDifficulties = 3;
    public const int MaxCreatureSpellDataSlots = 4;
    public const int MaxCreatureNames = 4;
    public const int MaxCreatureModelIds = 4;
    public const int MaxTrainerspellAbilityReqs = 3;
    public const int CreatureRegenInterval = 2 * Time.InMilliseconds;
    public const int PetFocusRegenInterval = 4 * Time.InMilliseconds;
    public const int CreatureNoPathEvadeTime = 5 * Time.InMilliseconds;
    public const int BoundaryVisualizeCreature = 15425;
    public const float BoundaryVisualizeCreatureScale = 0.25f;
    public const int BoundaryVisualizeStepSize = 1;
    public const int BoundaryVisualizeFailsafeLimit = 750;
    public const int BoundaryVisualizeSpawnHeight = 5;
    public const uint AIDefaultCooldown = 5000;
    public const uint CreatureTappersSoftCap = 5;

	/// <summary>
	///     GameObject Const
	/// </summary>
	public const int MaxGOData = 35;

    public const uint MaxTransportStopFrames = 9;

	/// <summary>
	///     AreaTrigger Const
	/// </summary>
	public const int MaxAreatriggerEntityData = 8;

    public const int MaxAreatriggerScale = 7;

	/// <summary>
	///     Pet Const
	/// </summary>
	public const int MaxActivePets = 5;

    public const int MaxPetStables = 200;
    public const uint CallPetSpellId = 883;
    public const float PetFollowDist = 1.0f;
    public const float PetFollowAngle = MathF.PI;
    public const int MaxSpellCharm = 4;
    public const int ActionBarIndexStart = 0;
    public const byte ActionBarIndexPetSpellStart = 3;
    public const int ActionBarIndexPetSpellEnd = 7;
    public const int ActionBarIndexEnd = 10;
    public const int MaxSpellControlBar = 10;
    public const int MaxPetTalentRank = 3;
    public const int ActionBarIndexMax = (ActionBarIndexEnd - ActionBarIndexStart);

	/// <summary>
	///     Object Const
	/// </summary>
	public const float DefaultPlayerBoundingRadius = 0.388999998569489f; // player size, also currently used (correctly?) for any non Unit world objects

    public const float AttackDistance = 5.0f;
    public const float DefaultPlayerCombatReach = 1.5f;
    public const float MinHitboxSum = 3.0f;
    public const float MinMeleeReach = 2.0f;
    public const float NominalMeleeRange = 5.0f;
    public const float MeleeRange = NominalMeleeRange - MinMeleeReach * 2; //center to center for players
    public const float ExtraCellSearchRadius = 40.0f;                      // We need in some cases increase search radius. Allow to find creatures with huge combat reach in a different nearby cell.
    public const float InspectDistance = 28.0f;
    public const float ContactDistance = 0.5f;
    public const float InteractionDistance = 5.0f;
    public const float MaxVisibilityDistance = MapConst.SizeofGrids; // max distance for visible objects
    public const float SightRangeUnit = 50.0f;
    public const float VisibilityDistanceGigantic = 400.0f;
    public const float VisibilityDistanceLarge = 200.0f;
    public const float VisibilityDistanceNormal = 100.0f;
    public const float VisibilityDistanceSmall = 50.0f;
    public const float VisibilityDistanceTiny = 25.0f;
    public const float DefaultVisibilityDistance = VisibilityDistanceNormal; // default visible distance, 100 yards on continents
    public const float DefaultVisibilityInstance = 170.0f;                   // default visible distance in instances, 170 yards
    public const float DefaultVisibilityBGAreans = 533.0f;                   // default visible distance in BG/Arenas, roughly 533 yards
    public const int DefaultVisibilityNotifyPeriod = 1000;

    public const int WorldTrigger = 12999;

    public const uint DisplayIdHiddenMount = 73200;

    public static float[] baseMoveSpeed =
    {
        2.5f,      // MOVE_WALK
        7.0f,      // MOVE_RUN
        4.5f,      // MOVE_RUN_BACK
        4.722222f, // MOVE_SWIM
        2.5f,      // MOVE_SWIM_BACK
        3.141594f, // MOVE_TURN_RATE
        7.0f,      // MOVE_FLIGHT
        4.5f,      // MOVE_FLIGHT_BACK
        3.14f      // MOVE_PITCH_RATE
    };

    public static float[] playerBaseMoveSpeed =
    {
        2.5f,      // MOVE_WALK
        7.0f,      // MOVE_RUN
        4.5f,      // MOVE_RUN_BACK
        4.722222f, // MOVE_SWIM
        2.5f,      // MOVE_SWIM_BACK
        3.141594f, // MOVE_TURN_RATE
        7.0f,      // MOVE_FLIGHT
        4.5f,      // MOVE_FLIGHT_BACK
        3.14f      // MOVE_PITCH_RATE
    };

    public static float[] VisibilityDistances =
    {
        DefaultVisibilityDistance, VisibilityDistanceTiny, VisibilityDistanceSmall, VisibilityDistanceLarge, VisibilityDistanceGigantic, MaxVisibilityDistance
    };

    private static readonly int[] raceBits =
    {
        0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 21, -1, 23, 24, 25, 26, 27, 28, 29, 30, 31, -1, 11, 12, 13, 14
    };

    public static ulong RaceMaskAllPlayable = (ulong)(GetMaskForRace(Race.Human) | GetMaskForRace(Race.Orc) | GetMaskForRace(Race.Dwarf) | GetMaskForRace(Race.NightElf) | GetMaskForRace(Race.Undead) | GetMaskForRace(Race.Tauren) | GetMaskForRace(Race.Gnome) | GetMaskForRace(Race.Troll) | GetMaskForRace(Race.BloodElf) | GetMaskForRace(Race.Draenei) | GetMaskForRace(Race.Goblin) | GetMaskForRace(Race.Worgen) | GetMaskForRace(Race.PandarenNeutral) | GetMaskForRace(Race.PandarenAlliance) | GetMaskForRace(Race.PandarenHorde) | GetMaskForRace(Race.Nightborne) | GetMaskForRace(Race.HighmountainTauren) | GetMaskForRace(Race.VoidElf) | GetMaskForRace(Race.LightforgedDraenei) | GetMaskForRace(Race.ZandalariTroll) | GetMaskForRace(Race.KulTiran) | GetMaskForRace(Race.DarkIronDwarf) | GetMaskForRace(Race.Vulpera) | GetMaskForRace(Race.MagharOrc) | GetMaskForRace(Race.MechaGnome) | GetMaskForRace(Race.DracthyrAlliance) | GetMaskForRace(Race.DracthyrHorde));

    public static ulong RaceMaskAlliance = (ulong)(GetMaskForRace(Race.Human) | GetMaskForRace(Race.Dwarf) | GetMaskForRace(Race.NightElf) | GetMaskForRace(Race.Gnome) | GetMaskForRace(Race.Draenei) | GetMaskForRace(Race.Worgen) | GetMaskForRace(Race.PandarenAlliance) | GetMaskForRace(Race.VoidElf) | GetMaskForRace(Race.LightforgedDraenei) | GetMaskForRace(Race.KulTiran) | GetMaskForRace(Race.DarkIronDwarf) | GetMaskForRace(Race.MechaGnome) | GetMaskForRace(Race.DracthyrAlliance));

    public static ulong RaceMaskHorde = RaceMaskAllPlayable & ~RaceMaskAlliance;

    public static CascLocaleBit[] WowLocaleToCascLocaleBit =
    {
        CascLocaleBit.enUS, CascLocaleBit.koKR, CascLocaleBit.frFR, CascLocaleBit.deDE, CascLocaleBit.zhCN, CascLocaleBit.zhTW, CascLocaleBit.esES, CascLocaleBit.esMX, CascLocaleBit.ruRU, CascLocaleBit.None, CascLocaleBit.ptBR, CascLocaleBit.itIT
    };


    //Todo move these else where
	/// <summary>
	///     Method Const
	/// </summary>
	public static SpellSchools GetFirstSchoolInMask(SpellSchoolMask mask)
    {
        for (SpellSchools i = 0; i < SpellSchools.Max; ++i)
            if (mask.HasAnyFlag((SpellSchoolMask)(1 << (int)i)))
                return i;

        return SpellSchools.Normal;
    }

    public static SkillType SkillByQuestSort(int sort)
    {
        switch ((QuestSort)sort)
        {
            case QuestSort.Herbalism:
                return SkillType.Herbalism;
            case QuestSort.Fishing:
                return SkillType.Fishing;
            case QuestSort.Blacksmithing:
                return SkillType.Blacksmithing;
            case QuestSort.Alchemy:
                return SkillType.Alchemy;
            case QuestSort.Leatherworking:
                return SkillType.Leatherworking;
            case QuestSort.Engineering:
                return SkillType.Engineering;
            case QuestSort.Tailoring:
                return SkillType.Tailoring;
            case QuestSort.Cooking:
                return SkillType.Cooking;
            case QuestSort.Jewelcrafting:
                return SkillType.Jewelcrafting;
            case QuestSort.Inscription:
                return SkillType.Inscription;
            case QuestSort.Archaeology:
                return SkillType.Archaeology;
        }

        return SkillType.None;
    }

    public static SkillType SkillByLockType(LockType locktype)
    {
        switch (locktype)
        {
            case LockType.Herbalism:
                return SkillType.Herbalism;
            case LockType.Mining:
                return SkillType.Mining;
            case LockType.Fishing:
                return SkillType.Fishing;
            case LockType.Inscription:
                return SkillType.Inscription;
            case LockType.Archaeology:
                return SkillType.Archaeology;
            case LockType.LumberMill:
                return SkillType.Logging;
            case LockType.ClassicHerbalism:
                return SkillType.ClassicHerbalism;
            case LockType.OutlandHerbalism:
                return SkillType.OutlandHerbalism;
            case LockType.NorthrendHerbalism:
                return SkillType.NorthrendHerbalism;
            case LockType.CataclysmHerbalism:
                return SkillType.CataclysmHerbalism;
            case LockType.PandariaHerbalism:
                return SkillType.PandariaHerbalism;
            case LockType.DraenorHerbalism:
                return SkillType.DraenorHerbalism;
            case LockType.LegionHerbalism:
                return SkillType.LegionHerbalism;
            case LockType.KulTiranHerbalism:
                return SkillType.KulTiranHerbalism;
            case LockType.ClassicMining:
                return SkillType.ClassicMining;
            case LockType.OutlandMining:
                return SkillType.OutlandMining;
            case LockType.NorthrendMining:
                return SkillType.NorthrendMining;
            case LockType.CataclysmMining:
                return SkillType.CataclysmMining;
            case LockType.PandariaMining:
                return SkillType.PandariaMining;
            case LockType.DraenorMining:
                return SkillType.DraenorMining;
            case LockType.LegionMining:
                return SkillType.LegionMining;
            case LockType.KulTiranMining:
                return SkillType.KulTiranMining;
            case LockType.DragonIslesEnchanting:
                return SkillType.DragonIslesEnchanting;
            case LockType.DragonIslesMining:
                return SkillType.DragonIslesMining;
            case LockType.LegionSkinning:
                return SkillType.LegionSkinning;
            case LockType.ShadowlandsHerbalism:
                return SkillType.ShadowlandsHerbalism;
            case LockType.ShadowlandsMining:
                return SkillType.ShadowlandsMining;
            case LockType.CovenantNightFae:
                return SkillType.CovenantNightFae;
            case LockType.CovenantVenthyr:
                return SkillType.CovenantVenthyr;
            case LockType.CovenantNecrolord:
                return SkillType.CovenantNecrolord;
            case LockType.CovenantKyrian:
                return SkillType.CovenantKyrian;
            case LockType.ProfessionEngineering:
                return SkillType.Engineering;
            case LockType.DragonIslesAlchemy25:
                return SkillType.DragonIslesAlchemy;
            case LockType.DragonIslesBlacksmithing25:
                return SkillType.DragonIslesBlacksmithing;
            case LockType.DragonIslesEnchanting25:
                return SkillType.DragonIslesEnchanting;
            case LockType.DragonIslesEngineering25:
                return SkillType.DragonIslesEngineering;
            case LockType.DragonIslesHerbalism:
            case LockType.DragonIslesHerbalism25:
                return SkillType.DragonIslesHerbalism;
            case LockType.DragonIslesInscription25:
                return SkillType.DragonIslesInscription;
            case LockType.DragonIslesJewelcrafting25:
                return SkillType.DragonIslesJewelcrafting;
            case LockType.DragonIslesLeatherworking25:
                return SkillType.DragonIslesLeatherworking;
            case LockType.DragonIslesMining25:
                return SkillType.DragonIslesMining;
        }

        return SkillType.None;
    }

    public static bool IsValidLocale(Locale locale)
    {
        return locale < Locale.Total && locale != Locale.None;
    }

    public static long GetMaskForRace(Race raceId)
    {
        var raceBit = GetRaceBit(raceId);

        return raceBit >= 0 && raceBit < sizeof(long) * 8 ? (1L << raceBit) : 0;
    }

    public static bool IsActivePetSlot(PetSaveMode slot)
    {
        return slot >= PetSaveMode.FirstActiveSlot && slot < PetSaveMode.LastActiveSlot;
    }

    public static bool IsStabledPetSlot(PetSaveMode slot)
    {
        return slot >= PetSaveMode.FirstStableSlot && slot < PetSaveMode.LastStableSlot;
    }

    public static LootType GetLootTypeForClient(LootType lootType)
    {
        switch (lootType)
        {
            case LootType.Prospecting:
            case LootType.Milling:
                return LootType.Disenchanting;
            case LootType.Insignia:
                return LootType.Skinning;
            case LootType.Fishinghole:
            case LootType.FishingJunk:
                return LootType.Fishing;
            default:
                break;
        }

        return lootType;
    }

    private static int GetRaceBit(Race raceId)
    {
        switch (raceId)
        {
            case Race.Human:
            case Race.Orc:
            case Race.Dwarf:
            case Race.NightElf:
            case Race.Undead:
            case Race.Tauren:
            case Race.Gnome:
            case Race.Troll:
            case Race.Goblin:
            case Race.BloodElf:
            case Race.Draenei:
            case Race.Worgen:
            case Race.PandarenNeutral:
            case Race.PandarenAlliance:
            case Race.PandarenHorde:
            case Race.Nightborne:
            case Race.HighmountainTauren:
            case Race.VoidElf:
            case Race.LightforgedDraenei:
            case Race.ZandalariTroll:
            case Race.KulTiran:
                return (int)raceId - 1;
            case Race.DarkIronDwarf:
                return 11;
            case Race.Vulpera:
                return 12;
            case Race.MagharOrc:
                return 13;
            case Race.MechaGnome:
                return 14;
            case Race.DracthyrAlliance:
                return 16;
            case Race.DracthyrHorde:
                return 15;
            default:
                return -1;
        }
    }
}

// values based at Holidays.dbc

// Db Scripting Commands

// Custom values