// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.E;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.DataStorage.Structs.H;
using Forged.MapServer.DataStorage.Structs.I;
using Forged.MapServer.DataStorage.Structs.J;
using Forged.MapServer.DataStorage.Structs.L;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.DataStorage.Structs.N;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.DataStorage.Structs.Q;
using Forged.MapServer.DataStorage.Structs.R;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.DataStorage.Structs.T;
using Forged.MapServer.DataStorage.Structs.U;
using Forged.MapServer.DataStorage.Structs.W;
using Forged.MapServer.Globals;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.DataStorage;

public class DB2Manager
{
    public readonly MultiMap<int, QuestPOIBlobEntry> QuestPOIBlobEntriesByMapId = new();
    public readonly MultiMap<uint, QuestLineXQuestRecord> QuestLinesByQuest = new();
    private readonly HotfixDatabase _hotfixDatabase;
    private readonly GameObjectManager _gameObjectManager;
    private readonly IConfiguration _configuration;
    private readonly MultiMap<int, HotfixRecord> _hotfixData = new();
    private readonly Dictionary<(uint tableHash, int recordId), byte[]>[] _hotfixBlob = new Dictionary<(uint tableHash, int recordId), byte[]>[(int)Locale.Total];
    private readonly MultiMap<uint, Tuple<uint, AllowedHotfixOptionalData>> _allowedHotfixOptionalData = new();
    private readonly MultiMap<(uint tableHash, int recordId), HotfixOptionalData>[] _hotfixOptionalData = new MultiMap<(uint tableHash, int recordId), HotfixOptionalData>[(int)Locale.Total];
    private readonly MultiMap<uint, uint> _areaGroupMembers = new();
    private readonly MultiMap<uint, AreaPOIRecord> _areaPOIRecords = new();
    private readonly MultiMap<uint, ArtifactPowerRecord> _artifactPowers = new();
    private readonly MultiMap<uint, uint> _artifactPowerLinks = new();
    private readonly Dictionary<Tuple<uint, byte>, ArtifactPowerRankRecord> _artifactPowerRanks = new();
    private readonly Dictionary<uint, AzeriteEmpoweredItemRecord> _azeriteEmpoweredItems = new();
    private readonly Dictionary<(uint azeriteEssenceId, uint rank), AzeriteEssencePowerRecord> _azeriteEssencePowersByIdAndRank = new();
    private readonly AzeriteItemMilestonePowerRecord[] _azeriteItemMilestonePowerByEssenceSlot = new AzeriteItemMilestonePowerRecord[SharedConst.MaxAzeriteEssenceSlot];
    private readonly MultiMap<uint, AzeritePowerSetMemberRecord> _azeritePowers = new();
    private readonly Dictionary<(uint azeriteUnlockSetId, ItemContext itemContext), byte[]> _azeriteTierUnlockLevels = new();
    private readonly Dictionary<(uint itemId, ItemContext itemContext), AzeriteUnlockMappingRecord> _azeriteUnlockMappings = new();
    private readonly Dictionary<(int broadcastTextId, CascLocaleBit cascLocaleBit), int> _broadcastTextDurations = new();
    private readonly ChrClassUIDisplayRecord[] _uiDisplayByClass = new ChrClassUIDisplayRecord[(int)PlayerClass.Max];
    private readonly uint[][] _powersByClass = new uint[(int)PlayerClass.Max][];
    private readonly MultiMap<uint, ChrCustomizationChoiceRecord> _chrCustomizationChoicesByOption = new();
    private readonly Dictionary<Tuple<byte, byte>, ChrModelRecord> _chrModelsByRaceAndGender = new();
    private readonly Dictionary<Tuple<byte, byte, byte>, ShapeshiftFormModelData> _chrCustomizationChoicesForShapeshifts = new();
    private readonly MultiMap<Tuple<byte, byte>, ChrCustomizationOptionRecord> _chrCustomizationOptionsByRaceAndGender = new();
    private readonly Dictionary<uint, MultiMap<uint, uint>> _chrCustomizationRequiredChoices = new();
    private readonly ChrSpecializationRecord[][] _chrSpecializationsByIndex = new ChrSpecializationRecord[(int)PlayerClass.Max + 1][];
    private readonly MultiMap<uint, CurrencyContainerRecord> _currencyContainers = new();
    private readonly MultiMap<uint, CurvePointRecord> _curvePoints = new();
    private readonly Dictionary<Tuple<uint, byte, byte, byte>, EmotesTextSoundRecord> _emoteTextSounds = new();
    private readonly Dictionary<Tuple<uint, int>, ExpectedStatRecord> _expectedStatsByLevel = new();
    private readonly MultiMap<uint, ContentTuningXExpectedRecord> _expectedStatModsByContentTuning = new();
    private readonly MultiMap<uint, uint> _factionTeams = new();
    private readonly MultiMap<uint, FriendshipRepReactionRecord> _friendshipRepReactions = new();
    private readonly Dictionary<uint, HeirloomRecord> _heirlooms = new();
    private readonly MultiMap<uint, uint> _glyphBindableSpells = new();
    private readonly MultiMap<uint, uint> _glyphRequiredSpecs = new();
    private readonly MultiMap<uint, ItemBonusRecord> _itemBonusLists = new();
    private readonly Dictionary<short, uint> _itemLevelDeltaToBonusListContainer = new();
    private readonly MultiMap<uint, ItemBonusTreeNodeRecord> _itemBonusTrees = new();
    private readonly Dictionary<uint, ItemChildEquipmentRecord> _itemChildEquipment = new();
    private readonly ItemClassRecord[] _itemClassByOldEnum = new ItemClassRecord[20];
    private readonly List<uint> _itemsWithCurrencyCost = new();
    private readonly MultiMap<uint, ItemLimitCategoryConditionRecord> _itemCategoryConditions = new();
    private readonly MultiMap<uint, ItemLevelSelectorQualityRecord> _itemLevelQualitySelectorQualities = new();
    private readonly Dictionary<uint, ItemModifiedAppearanceRecord> _itemModifiedAppearancesByItem = new();
    private readonly MultiMap<uint, uint> _itemToBonusTree = new();
    private readonly MultiMap<uint, ItemSetSpellRecord> _itemSetSpells = new();
    private readonly MultiMap<uint, ItemSpecOverrideRecord> _itemSpecOverrides = new();
    private readonly List<JournalTierRecord> _journalTiersByIndex = new();
    private readonly Dictionary<uint, Dictionary<uint, MapDifficultyRecord>> _mapDifficulties = new();
    private readonly MultiMap<uint, Tuple<uint, PlayerConditionRecord>> _mapDifficultyConditions = new();
    private readonly Dictionary<uint, MountRecord> _mountsBySpellId = new();
    private readonly MultiMap<uint, MountTypeXCapabilityRecord> _mountCapabilitiesByType = new();
    private readonly MultiMap<uint, MountXDisplayRecord> _mountDisplays = new();
    private readonly Dictionary<uint, List<NameGenRecord>[]> _nameGenData = new();
    private readonly List<string>[] _nameValidators = new List<string>[(int)Locale.Total + 1];
    private readonly Dictionary<uint, ParagonReputationRecord> _paragonReputations = new();
    private readonly MultiMap<uint, uint> _phasesByGroup = new();
    private readonly Dictionary<PowerType, PowerTypeRecord> _powerTypes = new();
    private readonly Dictionary<uint, byte> _pvpItemBonus = new();
    private readonly PvpTalentSlotUnlockRecord[] _pvpTalentSlotUnlock = new PvpTalentSlotUnlockRecord[PlayerConst.MaxPvpTalentSlots];
    private readonly MultiMap<uint, QuestLineXQuestRecord> _questsByQuestLine = new();
    private readonly Dictionary<uint, Tuple<List<QuestPackageItemRecord>, List<QuestPackageItemRecord>>> _questPackages = new();
    private readonly MultiMap<uint, RewardPackXCurrencyTypeRecord> _rewardPackCurrencyTypes = new();
    private readonly MultiMap<uint, RewardPackXItemRecord> _rewardPackItems = new();
    private readonly MultiMap<uint, SkillLineRecord> _skillLinesByParentSkillLine = new();
    private readonly MultiMap<uint, SkillLineAbilityRecord> _skillLineAbilitiesBySkillupSkill = new();
    private readonly MultiMap<uint, SkillRaceClassInfoRecord> _skillRaceClassInfoBySkill = new();
    private readonly Dictionary<Tuple<int, int>, SoulbindConduitRankRecord> _soulbindConduitRanks = new();
    private readonly MultiMap<uint, SpecializationSpellsRecord> _specializationSpellsBySpec = new();
    private readonly List<Tuple<int, uint>> _specsBySpecSet = new();
    private readonly List<byte> _spellFamilyNames = new();
    private readonly MultiMap<uint, SpellProcsPerMinuteModRecord> _spellProcsPerMinuteMods = new();
    private readonly MultiMap<uint, SpellVisualMissileRecord> _spellVisualMissilesBySet = new();
    private readonly List<TalentRecord>[][][] _talentsByPosition = new List<TalentRecord>[(int)PlayerClass.Max][][];
    private readonly List<uint> _toys = new();
    private readonly Dictionary<uint, TransmogIllusionRecord> _transmogIllusionsByEnchantmentId = new();
    private readonly MultiMap<uint, TransmogSetRecord> _transmogSetsByItemModifiedAppearance = new();
    private readonly MultiMap<uint, TransmogSetItemRecord> _transmogSetItemsByTransmogSet = new();
    private readonly Dictionary<int, UiMapBounds> _uiMapBounds = new();
    private readonly MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByMap = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
    private readonly MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByArea = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
    private readonly MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByWmoDoodadPlacement = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
    private readonly MultiMap<int, UiMapAssignmentRecord>[] _uiMapAssignmentByWmoGroup = new MultiMap<int, UiMapAssignmentRecord>[(int)UiMapSystem.Max];
    private readonly List<int> _uiMapPhases = new();
    private readonly Dictionary<Tuple<short, sbyte, int>, WMOAreaTableRecord> _wmoAreaTableLookup = new();
    private List<AzeriteItemMilestonePowerRecord> _azeriteItemMilestonePowers = new();
    private CliDB _cliDB;
    internal Dictionary<uint, IDB2Storage> Storage { get; } = new();

    public DB2Manager(HotfixDatabase hotfixDatabase, GameObjectManager gameObjectManager, IConfiguration configuration)
    {
        _hotfixDatabase = hotfixDatabase;
        _gameObjectManager = gameObjectManager;
        _configuration = configuration;

        for (uint i = 0; i < (int)PlayerClass.Max; ++i)
        {
            _powersByClass[i] = new uint[(int)PowerType.Max];

            for (uint j = 0; j < (int)PowerType.Max; ++j)
                _powersByClass[i][j] = (uint)PowerType.Max;
        }

        for (uint i = 0; i < (int)Locale.Total + 1; ++i)
            _nameValidators[i] = new List<string>();

        for (var i = 0; i < (int)Locale.Total; ++i)
        {
            _hotfixBlob[i] = new Dictionary<(uint tableHas, int recordId), byte[]>();
            _hotfixOptionalData[i] = new MultiMap<(uint tableHas, int recordId), HotfixOptionalData>();
        }
    }

    public void LoadStores(CliDB cliDB)
    {
        _cliDB = cliDB;

        foreach (var areaGroupMember in _cliDB.AreaGroupMemberStorage.Values)
            _areaGroupMembers.Add(areaGroupMember.AreaGroupID, areaGroupMember.AreaID);

        foreach (var arPoi in _cliDB.AreaPOIStorage.Values)
            _areaPOIRecords.Add((uint)arPoi.AreaID, arPoi);

        foreach (var artifactPower in _cliDB.ArtifactPowerStorage.Values)
            _artifactPowers.Add(artifactPower.ArtifactID, artifactPower);

        foreach (var artifactPowerLink in _cliDB.ArtifactPowerLinkStorage.Values)
        {
            _artifactPowerLinks.Add(artifactPowerLink.PowerA, artifactPowerLink.PowerB);
            _artifactPowerLinks.Add(artifactPowerLink.PowerB, artifactPowerLink.PowerA);
        }

        foreach (var artifactPowerRank in _cliDB.ArtifactPowerRankStorage.Values)
            _artifactPowerRanks[Tuple.Create(artifactPowerRank.ArtifactPowerID, artifactPowerRank.RankIndex)] = artifactPowerRank;

        foreach (var azeriteEmpoweredItem in _cliDB.AzeriteEmpoweredItemStorage.Values)
            _azeriteEmpoweredItems[azeriteEmpoweredItem.ItemID] = azeriteEmpoweredItem;

        foreach (var azeriteEssencePower in _cliDB.AzeriteEssencePowerStorage.Values)
            _azeriteEssencePowersByIdAndRank[((uint)azeriteEssencePower.AzeriteEssenceID, azeriteEssencePower.Tier)] = azeriteEssencePower;

        foreach (var azeriteItemMilestonePower in _cliDB.AzeriteItemMilestonePowerStorage.Values)
            _azeriteItemMilestonePowers.Add(azeriteItemMilestonePower);

        _azeriteItemMilestonePowers = _azeriteItemMilestonePowers.OrderBy(p => p.RequiredLevel).ToList();

        uint azeriteEssenceSlot = 0;

        foreach (var azeriteItemMilestonePower in _azeriteItemMilestonePowers)
        {
            var type = (AzeriteItemMilestoneType)azeriteItemMilestonePower.Type;

            if (type == AzeriteItemMilestoneType.MajorEssence || type == AzeriteItemMilestoneType.MinorEssence)
            {
                //ASSERT(azeriteEssenceSlot < MAX_AZERITE_ESSENCE_SLOT);
                _azeriteItemMilestonePowerByEssenceSlot[azeriteEssenceSlot] = azeriteItemMilestonePower;
                ++azeriteEssenceSlot;
            }
        }

        foreach (var azeritePowerSetMember in _cliDB.AzeritePowerSetMemberStorage.Values)
            if (_cliDB.AzeritePowerStorage.ContainsKey(azeritePowerSetMember.AzeritePowerID))
                _azeritePowers.Add(azeritePowerSetMember.AzeritePowerSetID, azeritePowerSetMember);

        foreach (var azeriteTierUnlock in _cliDB.AzeriteTierUnlockStorage.Values)
        {
            var key = (azeriteTierUnlock.AzeriteTierUnlockSetID, (ItemContext)azeriteTierUnlock.ItemCreationContext);

            if (!_azeriteTierUnlockLevels.ContainsKey(key))
                _azeriteTierUnlockLevels[key] = new byte[SharedConst.MaxAzeriteEmpoweredTier];

            _azeriteTierUnlockLevels[key][azeriteTierUnlock.Tier] = azeriteTierUnlock.AzeriteLevel;
        }

        MultiMap<uint, AzeriteUnlockMappingRecord> azeriteUnlockMappings = new();

        foreach (var azeriteUnlockMapping in _cliDB.AzeriteUnlockMappingStorage.Values)
            azeriteUnlockMappings.Add(azeriteUnlockMapping.AzeriteUnlockMappingSetID, azeriteUnlockMapping);

        foreach (var battlemaster in _cliDB.BattlemasterListStorage.Values)
        {
            if (battlemaster.MaxLevel < battlemaster.MinLevel)
            {
                Log.Logger.Error($"Battlemaster ({battlemaster.Id}) contains bad values for MinLevel ({battlemaster.MinLevel}) and MaxLevel ({battlemaster.MaxLevel}). Swapping values.");
                MathFunctions.Swap(ref battlemaster.MaxLevel, ref battlemaster.MinLevel);
            }

            if (battlemaster.MaxPlayers < battlemaster.MinPlayers)
            {
                Log.Logger.Error($"Battlemaster ({battlemaster.Id}) contains bad values for MinPlayers ({battlemaster.MinPlayers}) and MaxPlayers ({battlemaster.MaxPlayers}). Swapping values.");
                var minPlayers = battlemaster.MinPlayers;
                battlemaster.MinPlayers = (sbyte)battlemaster.MaxPlayers;
                battlemaster.MaxPlayers = minPlayers;
            }
        }

        foreach (var broadcastTextDuration in _cliDB.BroadcastTextDurationStorage.Values)
            _broadcastTextDurations[(broadcastTextDuration.BroadcastTextID, (CascLocaleBit)broadcastTextDuration.Locale)] = broadcastTextDuration.Duration;


        foreach (var uiDisplay in _cliDB.ChrClassUIDisplayStorage.Values)
            _uiDisplayByClass[uiDisplay.ChrClassesID] = uiDisplay;

        var powers = new List<ChrClassesXPowerTypesRecord>();

        foreach (var chrClasses in _cliDB.ChrClassesXPowerTypesStorage.Values)
            powers.Add(chrClasses);

        powers.Sort(new ChrClassesXPowerTypesRecordComparer());

        foreach (var power in powers)
        {
            uint index = 0;

            for (uint j = 0; j < (int)PowerType.Max; ++j)
                if (_powersByClass[power.ClassID][j] != (int)PowerType.Max)
                    ++index;

            _powersByClass[power.ClassID][power.PowerType] = index;
        }

        foreach (var customizationChoice in _cliDB.ChrCustomizationChoiceStorage.Values)
            _chrCustomizationChoicesByOption.Add(customizationChoice.ChrCustomizationOptionID, customizationChoice);

        MultiMap<uint, Tuple<uint, byte>> shapeshiftFormByModel = new();
        Dictionary<uint, ChrCustomizationDisplayInfoRecord> displayInfoByCustomizationChoice = new();

        // build shapeshift form model lookup
        foreach (var customizationElement in _cliDB.ChrCustomizationElementStorage.Values)
        {
            var customizationDisplayInfo = _cliDB.ChrCustomizationDisplayInfoStorage.LookupByKey((uint)customizationElement.ChrCustomizationDisplayInfoID);

            if (customizationDisplayInfo != null)
            {
                var customizationChoice = _cliDB.ChrCustomizationChoiceStorage.LookupByKey(customizationElement.ChrCustomizationChoiceID);

                if (customizationChoice != null)
                {
                    displayInfoByCustomizationChoice[customizationElement.ChrCustomizationChoiceID] = customizationDisplayInfo;
                    var customizationOption = _cliDB.ChrCustomizationOptionStorage.LookupByKey(customizationChoice.ChrCustomizationOptionID);

                    if (customizationOption != null)
                        shapeshiftFormByModel.Add(customizationOption.ChrModelID, Tuple.Create(customizationOption.Id, (byte)customizationDisplayInfo.ShapeshiftFormID));
                }
            }
        }

        MultiMap<uint, ChrCustomizationOptionRecord> customizationOptionsByModel = new();

        foreach (var customizationOption in _cliDB.ChrCustomizationOptionStorage.Values)
            customizationOptionsByModel.Add(customizationOption.ChrModelID, customizationOption);

        foreach (var reqChoice in _cliDB.ChrCustomizationReqChoiceStorage.Values)
        {
            var customizationChoice = _cliDB.ChrCustomizationChoiceStorage.LookupByKey(reqChoice.ChrCustomizationChoiceID);

            if (customizationChoice != null)
            {
                if (!_chrCustomizationRequiredChoices.ContainsKey(reqChoice.ChrCustomizationReqID))
                    _chrCustomizationRequiredChoices[reqChoice.ChrCustomizationReqID] = new MultiMap<uint, uint>();

                _chrCustomizationRequiredChoices[reqChoice.ChrCustomizationReqID].Add(customizationChoice.ChrCustomizationOptionID, reqChoice.ChrCustomizationChoiceID);
            }
        }

        Dictionary<uint, uint> parentRaces = new();

        foreach (var chrRace in _cliDB.ChrRacesStorage.Values)
            if (chrRace.UnalteredVisualRaceID != 0)
                parentRaces[(uint)chrRace.UnalteredVisualRaceID] = chrRace.Id;

        foreach (var raceModel in _cliDB.ChrRaceXChrModelStorage.Values)
        {
            var model = _cliDB.ChrModelStorage.LookupByKey((uint)raceModel.ChrModelID);

            if (model != null)
            {
                _chrModelsByRaceAndGender[Tuple.Create((byte)raceModel.ChrRacesID, (byte)raceModel.Sex)] = model;

                var customizationOptionsForModel = customizationOptionsByModel.LookupByKey(model.Id);

                if (customizationOptionsForModel != null)
                {
                    _chrCustomizationOptionsByRaceAndGender.AddRange(Tuple.Create((byte)raceModel.ChrRacesID, (byte)raceModel.Sex), customizationOptionsForModel);

                    var parentRace = parentRaces.LookupByKey((uint)raceModel.ChrRacesID);

                    if (parentRace != 0)
                        _chrCustomizationOptionsByRaceAndGender.AddRange(Tuple.Create((byte)parentRace, (byte)raceModel.Sex), customizationOptionsForModel);
                }

                // link shapeshift displays to race/gender/form
                foreach (var shapeshiftOptionsForModel in shapeshiftFormByModel.LookupByKey(model.Id))
                {
                    ShapeshiftFormModelData data = new()
                    {
                        OptionID = shapeshiftOptionsForModel.Item1,
                        Choices = _chrCustomizationChoicesByOption.LookupByKey(shapeshiftOptionsForModel.Item1)
                    };

                    if (!data.Choices.Empty())
                        for (var i = 0; i < data.Choices.Count; ++i)
                            data.Displays.Add(displayInfoByCustomizationChoice.LookupByKey(data.Choices[i].Id));

                    _chrCustomizationChoicesForShapeshifts[Tuple.Create((byte)raceModel.ChrRacesID, (byte)raceModel.Sex, shapeshiftOptionsForModel.Item2)] = data;
                }
            }
        }

        foreach (var chrSpec in _cliDB.ChrSpecializationStorage.Values)
        {
            //ASSERT(chrSpec.ClassID < MAX_CLASSES);
            //ASSERT(chrSpec.OrderIndex < MAX_SPECIALIZATIONS);

            uint storageIndex = chrSpec.ClassID;

            if (chrSpec.Flags.HasAnyFlag(ChrSpecializationFlag.PetOverrideSpec))
                //ASSERT(!chrSpec.ClassID);
                storageIndex = (int)PlayerClass.Max;

            if (_chrSpecializationsByIndex[storageIndex] == null)
                _chrSpecializationsByIndex[storageIndex] = new ChrSpecializationRecord[PlayerConst.MaxSpecializations];

            _chrSpecializationsByIndex[storageIndex][chrSpec.OrderIndex] = chrSpec;
        }

        foreach (var contentTuningXExpectedStat in _cliDB.ContentTuningXExpectedStorage.Values)
            if (_cliDB.ExpectedStatModStorage.ContainsKey(contentTuningXExpectedStat.ExpectedStatModID))
                _expectedStatModsByContentTuning.Add(contentTuningXExpectedStat.ContentTuningID, contentTuningXExpectedStat);

        foreach (var currencyContainer in _cliDB.CurrencyContainerStorage.Values)
            _currencyContainers.Add(currencyContainer.CurrencyTypesID, currencyContainer);

        foreach (var curvePoint in _cliDB.CurvePointStorage.Values)
            if (_cliDB.CurveStorage.ContainsKey(curvePoint.CurveID))
                _curvePoints.Add(curvePoint.CurveID, curvePoint);

        foreach (var key in _curvePoints.Keys.ToList())
            _curvePoints[key] = _curvePoints[key].OrderBy(point => point.OrderIndex).ToList();

        foreach (var emoteTextSound in _cliDB.EmotesTextSoundStorage.Values)
            _emoteTextSounds[Tuple.Create(emoteTextSound.EmotesTextId, emoteTextSound.RaceId, emoteTextSound.SexId, emoteTextSound.ClassId)] = emoteTextSound;

        foreach (var expectedStat in _cliDB.ExpectedStatStorage.Values)
            _expectedStatsByLevel[Tuple.Create(expectedStat.Lvl, expectedStat.ExpansionID)] = expectedStat;

        foreach (var faction in _cliDB.FactionStorage.Values)
            if (faction.ParentFactionID != 0)
                _factionTeams.Add(faction.ParentFactionID, faction.Id);

        foreach (var friendshipRepReaction in _cliDB.FriendshipRepReactionStorage.Values)
            _friendshipRepReactions.Add(friendshipRepReaction.FriendshipRepID, friendshipRepReaction);

        foreach (var key in _friendshipRepReactions.Keys)
            _friendshipRepReactions[key].Sort(new FriendshipRepReactionRecordComparer());

        foreach (var gameObjectDisplayInfo in _cliDB.GameObjectDisplayInfoStorage.Values)
        {
            if (gameObjectDisplayInfo.GeoBoxMax.X < gameObjectDisplayInfo.GeoBoxMin.X)
                Extensions.Swap(ref gameObjectDisplayInfo.GeoBox[3], ref gameObjectDisplayInfo.GeoBox[0]);

            if (gameObjectDisplayInfo.GeoBoxMax.Y < gameObjectDisplayInfo.GeoBoxMin.Y)
                Extensions.Swap(ref gameObjectDisplayInfo.GeoBox[4], ref gameObjectDisplayInfo.GeoBox[1]);

            if (gameObjectDisplayInfo.GeoBoxMax.Z < gameObjectDisplayInfo.GeoBoxMin.Z)
                Extensions.Swap(ref gameObjectDisplayInfo.GeoBox[5], ref gameObjectDisplayInfo.GeoBox[2]);
        }

        foreach (var heirloom in _cliDB.HeirloomStorage.Values)
            _heirlooms[heirloom.ItemID] = heirloom;

        foreach (var glyphBindableSpell in _cliDB.GlyphBindableSpellStorage.Values)
            _glyphBindableSpells.Add(glyphBindableSpell.GlyphPropertiesID, (uint)glyphBindableSpell.SpellID);

        foreach (var glyphRequiredSpec in _cliDB.GlyphRequiredSpecStorage.Values)
            _glyphRequiredSpecs.Add(glyphRequiredSpec.GlyphPropertiesID, glyphRequiredSpec.ChrSpecializationID);

        foreach (var bonus in _cliDB.ItemBonusStorage.Values)
            _itemBonusLists.Add(bonus.ParentItemBonusListID, bonus);

        foreach (var itemBonusListLevelDelta in _cliDB.ItemBonusListLevelDeltaStorage.Values)
            _itemLevelDeltaToBonusListContainer[itemBonusListLevelDelta.ItemLevelDelta] = itemBonusListLevelDelta.Id;

        foreach (var bonusTreeNode in _cliDB.ItemBonusTreeNodeStorage.Values)
            _itemBonusTrees.Add(bonusTreeNode.ParentItemBonusTreeID, bonusTreeNode);

        foreach (var itemChildEquipment in _cliDB.ItemChildEquipmentStorage.Values)
            //ASSERT(_itemChildEquipment.find(itemChildEquipment.ParentItemID) == _itemChildEquipment.end(), "Item must have max 1 child item.");
            _itemChildEquipment[itemChildEquipment.ParentItemID] = itemChildEquipment;

        foreach (var itemClass in _cliDB.ItemClassStorage.Values)
            //ASSERT(itemClass.ClassID < _itemClassByOldEnum.size());
            //ASSERT(!_itemClassByOldEnum[itemClass.ClassID]);
            _itemClassByOldEnum[itemClass.ClassID] = itemClass;

        foreach (var itemCurrencyCost in _cliDB.ItemCurrencyCostStorage.Values)
            _itemsWithCurrencyCost.Add(itemCurrencyCost.ItemID);

        foreach (var condition in _cliDB.ItemLimitCategoryConditionStorage.Values)
            _itemCategoryConditions.Add(condition.ParentItemLimitCategoryID, condition);

        foreach (var itemLevelSelectorQuality in _cliDB.ItemLevelSelectorQualityStorage.Values)
            _itemLevelQualitySelectorQualities.Add(itemLevelSelectorQuality.ParentILSQualitySetID, itemLevelSelectorQuality);

        foreach (var appearanceMod in _cliDB.ItemModifiedAppearanceStorage.Values)
            //ASSERT(appearanceMod.ItemID <= 0xFFFFFF);
            _itemModifiedAppearancesByItem[(uint)((int)appearanceMod.ItemID | (appearanceMod.ItemAppearanceModifierID << 24))] = appearanceMod;

        foreach (var itemSetSpell in _cliDB.ItemSetSpellStorage.Values)
            _itemSetSpells.Add(itemSetSpell.ItemSetID, itemSetSpell);

        foreach (var itemSpecOverride in _cliDB.ItemSpecOverrideStorage.Values)
            _itemSpecOverrides.Add(itemSpecOverride.ItemID, itemSpecOverride);

        foreach (var itemBonusTreeAssignment in _cliDB.ItemXBonusTreeStorage.Values)
            _itemToBonusTree.Add(itemBonusTreeAssignment.ItemID, itemBonusTreeAssignment.ItemBonusTreeID);

        foreach (var pair in _azeriteEmpoweredItems)
            LoadAzeriteEmpoweredItemUnlockMappings(azeriteUnlockMappings, pair.Key);

        foreach (var journalTier in _cliDB.JournalTierStorage.Values)
            _journalTiersByIndex.Add(journalTier);

        foreach (var entry in _cliDB.MapDifficultyStorage.Values)
        {
            if (!_mapDifficulties.ContainsKey(entry.MapID))
                _mapDifficulties[entry.MapID] = new Dictionary<uint, MapDifficultyRecord>();

            _mapDifficulties[entry.MapID][entry.DifficultyID] = entry;
        }

        List<MapDifficultyXConditionRecord> mapDifficultyConditions = new();

        foreach (var mapDifficultyCondition in _cliDB.MapDifficultyXConditionStorage.Values)
            mapDifficultyConditions.Add(mapDifficultyCondition);

        mapDifficultyConditions = mapDifficultyConditions.OrderBy(p => p.OrderIndex).ToList();

        foreach (var mapDifficultyCondition in mapDifficultyConditions)
        {
            var playerCondition = _cliDB.PlayerConditionStorage.LookupByKey(mapDifficultyCondition.PlayerConditionID);

            if (playerCondition != null)
                _mapDifficultyConditions.Add(mapDifficultyCondition.MapDifficultyID, Tuple.Create(mapDifficultyCondition.Id, playerCondition));
        }

        foreach (var mount in _cliDB.MountStorage.Values)
            _mountsBySpellId[mount.SourceSpellID] = mount;

        foreach (var mountTypeCapability in _cliDB.MountTypeXCapabilityStorage.Values)
            _mountCapabilitiesByType.Add(mountTypeCapability.MountTypeID, mountTypeCapability);

        foreach (var key in _mountCapabilitiesByType.Keys)
            _mountCapabilitiesByType[key].Sort(new MountTypeXCapabilityRecordComparer());

        foreach (var mountDisplay in _cliDB.MountXDisplayStorage.Values)
            _mountDisplays.Add(mountDisplay.MountID, mountDisplay);

        foreach (var entry in _cliDB.NameGenStorage.Values)
        {
            if (!_nameGenData.ContainsKey(entry.RaceID))
            {
                _nameGenData[entry.RaceID] = new List<NameGenRecord>[2];

                for (var i = 0; i < 2; ++i)
                    _nameGenData[entry.RaceID][i] = new List<NameGenRecord>();
            }

            _nameGenData[entry.RaceID][entry.Sex].Add(entry);
        }

        foreach (var namesProfanity in _cliDB.NamesProfanityStorage.Values)
            if (namesProfanity.Language != -1)
                _nameValidators[namesProfanity.Language].Add(namesProfanity.Name);
            else
                for (uint i = 0; i < (int)Locale.Total; ++i)
                {
                    if (i == (int)Locale.None)
                        continue;

                    _nameValidators[i].Add(namesProfanity.Name);
                }

        foreach (var namesReserved in _cliDB.NamesReservedStorage.Values)
            _nameValidators[(int)Locale.Total].Add(namesReserved.Name);

        foreach (var namesReserved in _cliDB.NamesReservedLocaleStorage.Values)
            for (var i = 0; i < (int)Locale.Total; ++i)
            {
                if (i == (int)Locale.None)
                    continue;

                if (Convert.ToBoolean(namesReserved.LocaleMask & (1 << i)))
                    _nameValidators[i].Add(namesReserved.Name);
            }

        foreach (var paragonReputation in _cliDB.ParagonReputationStorage.Values)
            if (_cliDB.FactionStorage.HasRecord(paragonReputation.FactionID))
                _paragonReputations[paragonReputation.FactionID] = paragonReputation;

        foreach (var group in _cliDB.PhaseXPhaseGroupStorage.Values)
        {
            var phase = _cliDB.PhaseStorage.LookupByKey(group.PhaseId);

            if (phase != null)
                _phasesByGroup.Add(group.PhaseGroupID, phase.Id);
        }

        foreach (var powerType in _cliDB.PowerTypeStorage.Values)
            _powerTypes[powerType.PowerTypeEnum] = powerType;

        foreach (var pvpItem in _cliDB.PvpItemStorage.Values)
            _pvpItemBonus[pvpItem.ItemID] = pvpItem.ItemLevelDelta;

        foreach (var talentUnlock in _cliDB.PvpTalentSlotUnlockStorage.Values)
            for (byte i = 0; i < PlayerConst.MaxPvpTalentSlots; ++i)
                if (Convert.ToBoolean(talentUnlock.Slot & (1 << i)))
                    _pvpTalentSlotUnlock[i] = talentUnlock;


        foreach (var poiBlob in _cliDB.QuestPOIBlobStorage)
            QuestPOIBlobEntriesByMapId.Add(poiBlob.Value.UiMapID, poiBlob.Value);

        foreach (var questLineQuest in _cliDB.QuestLineXQuestStorage.Values)
        {
            _questsByQuestLine.Add(questLineQuest.QuestLineID, questLineQuest);
            QuestLinesByQuest.Add(questLineQuest.QuestID, questLineQuest);
        }

        foreach (var questPackageItem in _cliDB.QuestPackageItemStorage.Values)
        {
            if (!_questPackages.ContainsKey(questPackageItem.PackageID))
                _questPackages[questPackageItem.PackageID] = Tuple.Create(new List<QuestPackageItemRecord>(), new List<QuestPackageItemRecord>());

            if (questPackageItem.DisplayType != QuestPackageFilter.Unmatched)
                _questPackages[questPackageItem.PackageID].Item1.Add(questPackageItem);
            else
                _questPackages[questPackageItem.PackageID].Item2.Add(questPackageItem);
        }

        foreach (var rewardPackXCurrencyType in _cliDB.RewardPackXCurrencyTypeStorage.Values)
            _rewardPackCurrencyTypes.Add(rewardPackXCurrencyType.RewardPackID, rewardPackXCurrencyType);

        foreach (var rewardPackXItem in _cliDB.RewardPackXItemStorage.Values)
            _rewardPackItems.Add(rewardPackXItem.RewardPackID, rewardPackXItem);

        foreach (var skill in _cliDB.SkillLineStorage.Values)
            if (skill.ParentSkillLineID != 0)
                _skillLinesByParentSkillLine.Add(skill.ParentSkillLineID, skill);

        foreach (var skillLineAbility in _cliDB.SkillLineAbilityStorage.Values)
            _skillLineAbilitiesBySkillupSkill.Add(skillLineAbility.SkillupSkillLineID != 0 ? skillLineAbility.SkillupSkillLineID : skillLineAbility.SkillLine, skillLineAbility);

        foreach (var entry in _cliDB.SkillRaceClassInfoStorage.Values)
            if (_cliDB.SkillLineStorage.ContainsKey(entry.SkillID))
                _skillRaceClassInfoBySkill.Add(entry.SkillID, entry);

        foreach (var soulbindConduitRank in _cliDB.SoulbindConduitRankStorage.Values)
            _soulbindConduitRanks[Tuple.Create((int)soulbindConduitRank.SoulbindConduitID, soulbindConduitRank.RankIndex)] = soulbindConduitRank;

        foreach (var specSpells in _cliDB.SpecializationSpellsStorage.Values)
            _specializationSpellsBySpec.Add(specSpells.SpecID, specSpells);

        foreach (var specSetMember in _cliDB.SpecSetMemberStorage.Values)
            _specsBySpecSet.Add(Tuple.Create((int)specSetMember.SpecSetID, specSetMember.ChrSpecializationID));

        foreach (var classOption in _cliDB.SpellClassOptionsStorage.Values)
            _spellFamilyNames.Add(classOption.SpellClassSet);

        foreach (var ppmMod in _cliDB.SpellProcsPerMinuteModStorage.Values)
            _spellProcsPerMinuteMods.Add(ppmMod.SpellProcsPerMinuteID, ppmMod);

        foreach (var spellVisualMissile in _cliDB.SpellVisualMissileStorage.Values)
            _spellVisualMissilesBySet.Add(spellVisualMissile.SpellVisualMissileSetID, spellVisualMissile);

        for (var i = 0; i < (int)PlayerClass.Max; ++i)
        {
            _talentsByPosition[i] = new List<TalentRecord>[PlayerConst.MaxTalentTiers][];

            for (var x = 0; x < PlayerConst.MaxTalentTiers; ++x)
            {
                _talentsByPosition[i][x] = new List<TalentRecord>[PlayerConst.MaxTalentColumns];

                for (var c = 0; c < PlayerConst.MaxTalentColumns; ++c)
                    _talentsByPosition[i][x][c] = new List<TalentRecord>();
            }
        }

        foreach (var talentInfo in _cliDB.TalentStorage.Values)
            //ASSERT(talentInfo.ClassID < MAX_CLASSES);
            //ASSERT(talentInfo.TierID < MAX_TALENT_TIERS, "MAX_TALENT_TIERS must be at least {0}", talentInfo.TierID);
            //ASSERT(talentInfo.ColumnIndex < MAX_TALENT_COLUMNS, "MAX_TALENT_COLUMNS must be at least {0}", talentInfo.ColumnIndex);
            _talentsByPosition[talentInfo.ClassID][talentInfo.TierID][talentInfo.ColumnIndex].Add(talentInfo);

        foreach (var toy in _cliDB.ToyStorage.Values)
            _toys.Add(toy.ItemID);

        foreach (var transmogIllusion in _cliDB.TransmogIllusionStorage.Values)
            _transmogIllusionsByEnchantmentId[(uint)transmogIllusion.SpellItemEnchantmentID] = transmogIllusion;

        foreach (var transmogSetItem in _cliDB.TransmogSetItemStorage.Values)
        {
            var set = _cliDB.TransmogSetStorage.LookupByKey(transmogSetItem.TransmogSetID);

            if (set == null)
                continue;

            _transmogSetsByItemModifiedAppearance.Add(transmogSetItem.ItemModifiedAppearanceID, set);
            _transmogSetItemsByTransmogSet.Add(transmogSetItem.TransmogSetID, transmogSetItem);
        }

        for (var i = 0; i < (int)UiMapSystem.Max; ++i)
        {
            _uiMapAssignmentByMap[i] = new MultiMap<int, UiMapAssignmentRecord>();
            _uiMapAssignmentByArea[i] = new MultiMap<int, UiMapAssignmentRecord>();
            _uiMapAssignmentByWmoDoodadPlacement[i] = new MultiMap<int, UiMapAssignmentRecord>();
            _uiMapAssignmentByWmoGroup[i] = new MultiMap<int, UiMapAssignmentRecord>();
        }

        MultiMap<int, UiMapAssignmentRecord> uiMapAssignmentByUiMap = new();

        foreach (var uiMapAssignment in _cliDB.UiMapAssignmentStorage.Values)
        {
            uiMapAssignmentByUiMap.Add(uiMapAssignment.UiMapID, uiMapAssignment);
            var uiMap = _cliDB.UiMapStorage.LookupByKey((uint)uiMapAssignment.UiMapID);

            if (uiMap != null)
            {
                //ASSERT(uiMap.System < MAX_UI_MAP_SYSTEM, $"MAX_TALENT_TIERS must be at least {uiMap.System + 1}");
                if (uiMapAssignment.MapID >= 0)
                    _uiMapAssignmentByMap[uiMap.System].Add(uiMapAssignment.MapID, uiMapAssignment);

                if (uiMapAssignment.AreaID != 0)
                    _uiMapAssignmentByArea[uiMap.System].Add(uiMapAssignment.AreaID, uiMapAssignment);

                if (uiMapAssignment.WmoDoodadPlacementID != 0)
                    _uiMapAssignmentByWmoDoodadPlacement[uiMap.System].Add(uiMapAssignment.WmoDoodadPlacementID, uiMapAssignment);

                if (uiMapAssignment.WmoGroupID != 0)
                    _uiMapAssignmentByWmoGroup[uiMap.System].Add(uiMapAssignment.WmoGroupID, uiMapAssignment);
            }
        }

        Dictionary<Tuple<int, uint>, UiMapLinkRecord> uiMapLinks = new();

        foreach (var uiMapLink in _cliDB.UiMapLinkStorage.Values)
            uiMapLinks[Tuple.Create(uiMapLink.ParentUiMapID, (uint)uiMapLink.ChildUiMapID)] = uiMapLink;

        foreach (var uiMap in _cliDB.UiMapStorage.Values)
        {
            UiMapBounds bounds = new();
            var parentUiMap = _cliDB.UiMapStorage.LookupByKey((uint)uiMap.ParentUiMapID);

            if (parentUiMap != null)
            {
                if (parentUiMap.GetFlags().HasAnyFlag(UiMapFlag.NoWorldPositions))
                    continue;

                UiMapAssignmentRecord uiMapAssignment = null;
                UiMapAssignmentRecord parentUiMapAssignment = null;

                foreach (var uiMapAssignmentForMap in uiMapAssignmentByUiMap.LookupByKey(uiMap.Id))
                    if (uiMapAssignmentForMap.MapID >= 0 &&
                        uiMapAssignmentForMap.Region[1].X - uiMapAssignmentForMap.Region[0].X > 0 &&
                        uiMapAssignmentForMap.Region[1].Y - uiMapAssignmentForMap.Region[0].Y > 0)
                    {
                        uiMapAssignment = uiMapAssignmentForMap;

                        break;
                    }

                if (uiMapAssignment == null)
                    continue;

                foreach (var uiMapAssignmentForMap in uiMapAssignmentByUiMap.LookupByKey(uiMap.ParentUiMapID))
                    if (uiMapAssignmentForMap.MapID == uiMapAssignment.MapID &&
                        uiMapAssignmentForMap.Region[1].X - uiMapAssignmentForMap.Region[0].X > 0 &&
                        uiMapAssignmentForMap.Region[1].Y - uiMapAssignmentForMap.Region[0].Y > 0)
                    {
                        parentUiMapAssignment = uiMapAssignmentForMap;

                        break;
                    }

                if (parentUiMapAssignment == null)
                    continue;

                var parentXsize = parentUiMapAssignment.Region[1].X - parentUiMapAssignment.Region[0].X;
                var parentYsize = parentUiMapAssignment.Region[1].Y - parentUiMapAssignment.Region[0].Y;
                var bound0Scale = (uiMapAssignment.Region[1].X - parentUiMapAssignment.Region[0].X) / parentXsize;
                var bound0 = ((1.0f - bound0Scale) * parentUiMapAssignment.UiMax.Y) + (bound0Scale * parentUiMapAssignment.UiMin.Y);
                var bound2Scale = (uiMapAssignment.Region[0].X - parentUiMapAssignment.Region[0].X) / parentXsize;
                var bound2 = ((1.0f - bound2Scale) * parentUiMapAssignment.UiMax.Y) + (bound2Scale * parentUiMapAssignment.UiMin.Y);
                var bound1Scale = (uiMapAssignment.Region[1].Y - parentUiMapAssignment.Region[0].Y) / parentYsize;
                var bound1 = ((1.0f - bound1Scale) * parentUiMapAssignment.UiMax.X) + (bound1Scale * parentUiMapAssignment.UiMin.X);
                var bound3Scale = (uiMapAssignment.Region[0].Y - parentUiMapAssignment.Region[0].Y) / parentYsize;
                var bound3 = ((1.0f - bound3Scale) * parentUiMapAssignment.UiMax.X) + (bound3Scale * parentUiMapAssignment.UiMin.X);

                if ((bound3 - bound1) > 0.0f || (bound2 - bound0) > 0.0f)
                {
                    bounds.Bounds[0] = bound0;
                    bounds.Bounds[1] = bound1;
                    bounds.Bounds[2] = bound2;
                    bounds.Bounds[3] = bound3;
                    bounds.IsUiAssignment = true;
                }
            }

            var uiMapLink = uiMapLinks.LookupByKey(Tuple.Create(uiMap.ParentUiMapID, uiMap.Id));

            if (uiMapLink != null)
            {
                bounds.IsUiAssignment = false;
                bounds.IsUiLink = true;
                bounds.Bounds[0] = uiMapLink.UiMin.Y;
                bounds.Bounds[1] = uiMapLink.UiMin.X;
                bounds.Bounds[2] = uiMapLink.UiMax.Y;
                bounds.Bounds[3] = uiMapLink.UiMax.X;
            }

            _uiMapBounds[(int)uiMap.Id] = bounds;
        }

        foreach (var uiMapArt in _cliDB.UiMapXMapArtStorage.Values)
            if (uiMapArt.PhaseID != 0)
                _uiMapPhases.Add(uiMapArt.PhaseID);

        foreach (var entry in _cliDB.WMOAreaTableStorage.Values)
            _wmoAreaTableLookup[Tuple.Create((short)entry.WmoID, (sbyte)entry.NameSetID, entry.WmoGroupID)] = entry;
    }

    public IDB2Storage GetStorage(uint type)
    {
        return Storage.LookupByKey(type);
    }

    public void LoadHotfixData()
    {
        var oldMSTime = Time.MSTime;

        var result = _hotfixDatabase.Query("SELECT Id, UniqueId, TableHash, RecordId, Status FROM hotfix_data ORDER BY Id");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 hotfix info entries.");

            return;
        }

        Dictionary<(uint tableHash, int recordId), bool> deletedRecords = new();

        uint count = 0;

        do
        {
            var id = result.Read<int>(0);
            var uniqueId = result.Read<uint>(1);
            var tableHash = result.Read<uint>(2);
            var recordId = result.Read<int>(3);
            var status = (HotfixRecord.Status)result.Read<byte>(4);

            if (status == HotfixRecord.Status.Valid && !Storage.ContainsKey(tableHash))
                if (!_hotfixBlob.Any(p => p.ContainsKey((tableHash, recordId))))
                {
                    Log.Logger.Error($"Table `hotfix_data` references unknown DB2 store by hash 0x{tableHash:X} and has no reference to `hotfix_blob` in hotfix id {id} with RecordID: {recordId}");

                    continue;
                }

            HotfixRecord hotfixRecord = new()
            {
                TableHash = tableHash,
                RecordID = recordId
            };

            hotfixRecord.ID.PushID = id;
            hotfixRecord.ID.UniqueID = uniqueId;
            hotfixRecord.HotfixStatus = status;

            _hotfixData.Add(id, hotfixRecord);
            deletedRecords[(tableHash, recordId)] = status == HotfixRecord.Status.RecordRemoved;

            ++count;
        } while (result.NextRow());

        foreach (var itr in deletedRecords)
            if (itr.Value)
            {
                var store = Storage.LookupByKey(itr.Key.tableHash);

                if (store != null)
                    store.EraseRecord((uint)itr.Key.recordId);
            }

        Log.Logger.Information("Loaded {0} hotfix info entries in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public void LoadHotfixBlob(BitSet availableDb2Locales)
    {
        var oldMSTime = Time.MSTime;

        var result = _hotfixDatabase.Query("SELECT TableHash, RecordId, locale, `Blob` FROM hotfix_blob ORDER BY TableHash");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 hotfix blob entries.");

            return;
        }

        uint hotfixBlobCount = 0;

        do
        {
            var tableHash = result.Read<uint>(0);
            var recordId = result.Read<int>(1);
            var localeName = result.Read<string>(2);

            var storeItr = Storage.LookupByKey(tableHash);

            if (storeItr != null)
            {
                Log.Logger.Warning($"Table hash 0x{tableHash:X}({tableHash}) points to a loaded DB2 store {storeItr.GetName()} {recordId}:{localeName}, fill related table instead of hotfix_blob");

                continue;
            }

            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale))
            {
                Log.Logger.Warning($"`hotfix_blob` contains invalid locale: {localeName} at TableHash: 0x{tableHash:X} and RecordID: {recordId}");

                continue;
            }

            if (!availableDb2Locales[(int)locale])
                continue;

            _hotfixBlob[(int)locale][(tableHash, recordId)] = result.Read<byte[]>(3);
            hotfixBlobCount++;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {hotfixBlobCount} hotfix blob records in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public void LoadHotfixOptionalData(BitSet availableDb2Locales)
    {
        // Register allowed optional data keys
        _allowedHotfixOptionalData.Add(_cliDB.BroadcastTextStorage.GetTableHash(), Tuple.Create(_cliDB.TactKeyStorage.GetTableHash(), (AllowedHotfixOptionalData)ValidateBroadcastTextTactKeyOptionalData));

        var oldMSTime = Time.MSTime;

        var result = _hotfixDatabase.Query("SELECT TableHash, RecordId, locale, `Key`, `Data` FROM hotfix_optional_data ORDER BY TableHash");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 hotfix optional data records.");

            return;
        }

        uint hotfixOptionalDataCount = 0;

        do
        {
            var tableHash = result.Read<uint>(0);
            var allowedHotfixes = _allowedHotfixOptionalData.LookupByKey(tableHash);

            if (allowedHotfixes.Empty())
            {
                Log.Logger.Error($"Table `hotfix_optional_data` references DB2 store by hash 0x{tableHash:X} that is not allowed to have optional data");

                continue;
            }

            var recordId = result.Read<uint>(1);
            var db2Storage = Storage.LookupByKey(tableHash);

            if (db2Storage == null)
            {
                Log.Logger.Error($"Table `hotfix_optional_data` references unknown DB2 store by hash 0x{tableHash:X} with RecordID: {recordId}");

                continue;
            }

            var localeName = result.Read<string>(2);
            var locale = localeName.ToEnum<Locale>();

            if (!SharedConst.IsValidLocale(locale))
            {
                Log.Logger.Error($"`hotfix_optional_data` contains invalid locale: {localeName} at TableHash: 0x{tableHash:X} and RecordID: {recordId}");

                continue;
            }

            if (!availableDb2Locales[(int)locale])
                continue;

            HotfixOptionalData optionalData = new()
            {
                Key = result.Read<uint>(3)
            };

            var allowedHotfixItr = allowedHotfixes.Find(v => { return v.Item1 == optionalData.Key; });

            if (allowedHotfixItr == null)
            {
                Log.Logger.Error($"Table `hotfix_optional_data` references non-allowed optional data key 0x{optionalData.Key:X} for DB2 store by hash 0x{tableHash:X} and RecordID: {recordId}");

                continue;
            }

            optionalData.Data = result.Read<byte[]>(4);

            if (!allowedHotfixItr.Item2(optionalData.Data))
            {
                Log.Logger.Error($"Table `hotfix_optional_data` contains invalid data for DB2 store 0x{tableHash:X}, RecordID: {recordId} and Key: 0x{optionalData.Key:X}");

                continue;
            }

            _hotfixOptionalData[(int)locale].Add((tableHash, (int)recordId), optionalData);
            hotfixOptionalDataCount++;
        } while (result.NextRow());

        Log.Logger.Information($"Loaded {hotfixOptionalDataCount} hotfix optional data records in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }

    public bool ValidateBroadcastTextTactKeyOptionalData(byte[] data)
    {
        return data.Length == 8 + 16;
    }

    public uint GetHotfixCount()
    {
        return (uint)_hotfixData.Count;
    }

    public MultiMap<int, HotfixRecord> GetHotfixData()
    {
        return _hotfixData;
    }

    public byte[] GetHotfixBlobData(uint tableHash, int recordId, Locale locale)
    {
        return _hotfixBlob[(int)locale].LookupByKey((tableHash, recordId));
    }

    public List<HotfixOptionalData> GetHotfixOptionalData(uint tableHash, uint recordId, Locale locale)
    {
        return _hotfixOptionalData[(int)locale].LookupByKey((tableHash, (int)recordId));
    }

    public uint GetEmptyAnimStateID()
    {
        return _cliDB.AnimationDataStorage.GetNumRows();
    }

    public List<uint> GetAreasForGroup(uint areaGroupId)
    {
        return _areaGroupMembers.LookupByKey(areaGroupId);
    }

    public List<AreaPOIRecord> GetAreaPoiID(uint areaId)
    {
        return _areaPOIRecords.LookupByKey(areaId);
    }

    public bool IsInArea(uint objectAreaId, uint areaId)
    {
        do
        {
            if (objectAreaId == areaId)
                return true;

            var objectArea = _cliDB.AreaTableStorage.LookupByKey(objectAreaId);

            if (objectArea == null)
                break;

            objectAreaId = objectArea.ParentAreaID;
        } while (objectAreaId != 0);

        return false;
    }

    public List<ArtifactPowerRecord> GetArtifactPowers(byte artifactId)
    {
        return _artifactPowers.LookupByKey(artifactId);
    }

    public List<uint> GetArtifactPowerLinks(uint artifactPowerId)
    {
        return _artifactPowerLinks.LookupByKey(artifactPowerId);
    }

    public ArtifactPowerRankRecord GetArtifactPowerRank(uint artifactPowerId, byte rank)
    {
        return _artifactPowerRanks.LookupByKey(Tuple.Create(artifactPowerId, rank));
    }

    public AzeriteEmpoweredItemRecord GetAzeriteEmpoweredItem(uint itemId)
    {
        return _azeriteEmpoweredItems.LookupByKey(itemId);
    }

    public bool IsAzeriteItem(uint itemId)
    {
        return _cliDB.AzeriteItemStorage.Any(pair => pair.Value.ItemID == itemId);
    }

    public AzeriteEssencePowerRecord GetAzeriteEssencePower(uint azeriteEssenceId, uint rank)
    {
        return _azeriteEssencePowersByIdAndRank.LookupByKey((azeriteEssenceId, rank));
    }

    public List<AzeriteItemMilestonePowerRecord> GetAzeriteItemMilestonePowers()
    {
        return _azeriteItemMilestonePowers;
    }

    public AzeriteItemMilestonePowerRecord GetAzeriteItemMilestonePower(int slot)
    {
        //ASSERT(slot < MAX_AZERITE_ESSENCE_SLOT, "Slot %u must be lower than MAX_AZERITE_ESSENCE_SLOT (%u)", uint32(slot), MAX_AZERITE_ESSENCE_SLOT);
        return _azeriteItemMilestonePowerByEssenceSlot[slot];
    }

    public List<AzeritePowerSetMemberRecord> GetAzeritePowers(uint itemId)
    {
        var azeriteEmpoweredItem = GetAzeriteEmpoweredItem(itemId);

        if (azeriteEmpoweredItem != null)
            return _azeritePowers.LookupByKey(azeriteEmpoweredItem.AzeritePowerSetID);

        return null;
    }

    public uint GetRequiredAzeriteLevelForAzeritePowerTier(uint azeriteUnlockSetId, ItemContext context, uint tier)
    {
        //ASSERT(tier < MAX_AZERITE_EMPOWERED_TIER);
        var levels = _azeriteTierUnlockLevels.LookupByKey((azeriteUnlockSetId, context));

        if (levels != null)
            return levels[tier];

        var azeriteTierUnlockSet = _cliDB.AzeriteTierUnlockSetStorage.LookupByKey(azeriteUnlockSetId);

        if (azeriteTierUnlockSet != null && azeriteTierUnlockSet.Flags.HasAnyFlag(AzeriteTierUnlockSetFlags.Default))
        {
            levels = _azeriteTierUnlockLevels.LookupByKey((azeriteUnlockSetId, ItemContext.None));

            if (levels != null)
                return levels[tier];
        }

        return _cliDB.AzeriteLevelInfoStorage.GetNumRows();
    }

    public string GetBroadcastTextValue(BroadcastTextRecord broadcastText, Locale locale = Locale.enUS, Gender gender = Gender.Male, bool forceGender = false)
    {
        if ((gender == Gender.Female || gender == Gender.None) && (forceGender || broadcastText.Text1.HasString()))
        {
            if (broadcastText.Text1.HasString(locale))
                return broadcastText.Text1[locale];

            return broadcastText.Text1[SharedConst.DefaultLocale];
        }

        if (broadcastText.Text.HasString(locale))
            return broadcastText.Text[locale];

        return broadcastText.Text[SharedConst.DefaultLocale];
    }

    public int GetBroadcastTextDuration(int broadcastTextId, Locale locale = Locale.enUS)
    {
        return _broadcastTextDurations.LookupByKey((broadcastTextId, SharedConst.WowLocaleToCascLocaleBit[(int)locale]));
    }

    public ChrClassUIDisplayRecord GetUiDisplayForClass(PlayerClass unitClass)
    {
        return _uiDisplayByClass[(int)unitClass];
    }

    public string GetClassName(PlayerClass playerClass, Locale locale = Locale.enUS)
    {
        var classEntry = _cliDB.ChrClassesStorage.LookupByKey((uint)playerClass);

        if (classEntry == null)
            return "";

        if (classEntry.Name[locale][0] != '\0')
            return classEntry.Name[locale];

        return classEntry.Name[Locale.enUS];
    }

    public uint GetPowerIndexByClass(PowerType powerType, PlayerClass classId)
    {
        return _powersByClass[(int)classId][(int)powerType];
    }

    public List<ChrCustomizationChoiceRecord> GetCustomiztionChoices(uint chrCustomizationOptionId)
    {
        return _chrCustomizationChoicesByOption.LookupByKey(chrCustomizationOptionId);
    }

    public List<ChrCustomizationOptionRecord> GetCustomiztionOptions(Race race, Gender gender)
    {
        return _chrCustomizationOptionsByRaceAndGender.LookupByKey(Tuple.Create((byte)race, (byte)gender));
    }

    public MultiMap<uint, uint> GetRequiredCustomizationChoices(uint chrCustomizationReqId)
    {
        return _chrCustomizationRequiredChoices.LookupByKey(chrCustomizationReqId);
    }

    public ChrModelRecord GetChrModel(Race race, Gender gender)
    {
        return _chrModelsByRaceAndGender.LookupByKey(Tuple.Create((byte)race, (byte)gender));
    }

    public string GetChrRaceName(Race race, Locale locale = Locale.enUS)
    {
        var raceEntry = _cliDB.ChrRacesStorage.LookupByKey((uint)race);

        if (raceEntry == null)
            return "";

        if (raceEntry.Name[locale][0] != '\0')
            return raceEntry.Name[locale];

        return raceEntry.Name[Locale.enUS];
    }

    public ChrSpecializationRecord GetChrSpecializationByIndex(PlayerClass playerClass, uint index)
    {
        return _chrSpecializationsByIndex[(int)playerClass][index];
    }

    public ChrSpecializationRecord GetDefaultChrSpecializationForClass(PlayerClass playerClass)
    {
        return GetChrSpecializationByIndex(playerClass, PlayerConst.InitialSpecializationIndex);
    }

    public ContentTuningLevels? GetContentTuningData(uint contentTuningId, uint replacementConditionMask, bool forItem = false)
    {
        var contentTuning = _cliDB.ContentTuningStorage.LookupByKey(contentTuningId);

        if (contentTuning == null)
            return null;

        if (forItem && contentTuning.GetFlags().HasFlag(ContentTuningFlag.DisabledForItem))
            return null;

        int GetLevelAdjustment(ContentTuningCalcType type) => type switch
        {
            ContentTuningCalcType.PlusOne                  => 1,
            ContentTuningCalcType.PlusMaxLevelForExpansion => (int)_gameObjectManager.GetMaxLevelForExpansion((Expansion)_configuration.GetDefaultValue("Expansion", (int)Expansion.Dragonflight)),
            _                                              => 0
        };

        ContentTuningLevels levels = new()
        {
            MinLevel = (short)(contentTuning.MinLevel + GetLevelAdjustment((ContentTuningCalcType)contentTuning.MinLevelType)),
            MaxLevel = (short)(contentTuning.MaxLevel + GetLevelAdjustment((ContentTuningCalcType)contentTuning.MaxLevelType))
        };

        levels.MinLevelWithDelta = (short)Math.Clamp(levels.MinLevel + contentTuning.TargetLevelDelta, 1, SharedConst.MaxLevel);
        levels.MaxLevelWithDelta = (short)Math.Clamp(levels.MaxLevel + contentTuning.TargetLevelMaxDelta, 1, SharedConst.MaxLevel);

        // clamp after calculating levels with delta (delta can bring "overflown" level back into correct range)
        levels.MinLevel = (short)Math.Clamp((int)levels.MinLevel, 1, SharedConst.MaxLevel);
        levels.MaxLevel = (short)Math.Clamp((int)levels.MaxLevel, 1, SharedConst.MaxLevel);

        if (contentTuning.TargetLevelMin != 0)
            levels.TargetLevelMin = (short)contentTuning.TargetLevelMin;
        else
            levels.TargetLevelMin = levels.MinLevelWithDelta;

        if (contentTuning.TargetLevelMax != 0)
            levels.TargetLevelMax = (short)contentTuning.TargetLevelMax;
        else
            levels.TargetLevelMax = levels.MaxLevelWithDelta;

        return levels;
    }

    public string GetCreatureFamilyPetName(CreatureFamily petfamily, Locale locale)
    {
        if (petfamily == CreatureFamily.None)
            return null;

        var petFamily = _cliDB.CreatureFamilyStorage.LookupByKey((uint)petfamily);

        if (petFamily == null)
            return "";

        return petFamily.Name[locale][0] != '\0' ? petFamily.Name[locale] : "";
    }

    public CurrencyContainerRecord GetCurrencyContainerForCurrencyQuantity(uint currencyId, int quantity)
    {
        foreach (var record in _currencyContainers.LookupByKey(currencyId))
            if (quantity >= record.MinAmount && (record.MaxAmount == 0 || quantity <= record.MaxAmount))
                return record;

        return null;
    }

    public Tuple<float, float> GetCurveXAxisRange(uint curveId)
    {
        var points = _curvePoints.LookupByKey(curveId);

        if (!points.Empty())
            return Tuple.Create(points.First().Pos.X, points.Last().Pos.X);

        return Tuple.Create(0.0f, 0.0f);
    }

    public float GetCurveValueAt(uint curveId, float x)
    {
        var points = _curvePoints.LookupByKey(curveId);

        if (points.Empty())
            return 0.0f;

        var curve = _cliDB.CurveStorage.LookupByKey(curveId);

        switch (DetermineCurveType(curve, points))
        {
            case CurveInterpolationMode.Linear:
            {
                var pointIndex = 0;

                while (pointIndex < points.Count && points[pointIndex].Pos.X <= x)
                    ++pointIndex;

                if (pointIndex == 0)
                    return points[0].Pos.Y;

                if (pointIndex >= points.Count)
                    return points.Last().Pos.Y;

                var xDiff = points[pointIndex].Pos.X - points[pointIndex - 1].Pos.X;

                if (xDiff == 0.0)
                    return points[pointIndex].Pos.Y;

                return (((x - points[pointIndex - 1].Pos.X) / xDiff) * (points[pointIndex].Pos.Y - points[pointIndex - 1].Pos.Y)) + points[pointIndex - 1].Pos.Y;
            }
            case CurveInterpolationMode.Cosine:
            {
                var pointIndex = 0;

                while (pointIndex < points.Count && points[pointIndex].Pos.X <= x)
                    ++pointIndex;

                if (pointIndex == 0)
                    return points[0].Pos.Y;

                if (pointIndex >= points.Count)
                    return points.Last().Pos.Y;

                var xDiff = points[pointIndex].Pos.X - points[pointIndex - 1].Pos.X;

                if (xDiff == 0.0)
                    return points[pointIndex].Pos.Y;

                return (float)((points[pointIndex].Pos.Y - points[pointIndex - 1].Pos.Y) * (1.0f - Math.Cos((x - points[pointIndex - 1].Pos.X) / xDiff * Math.PI)) * 0.5f) + points[pointIndex - 1].Pos.Y;
            }
            case CurveInterpolationMode.CatmullRom:
            {
                var pointIndex = 1;

                while (pointIndex < points.Count && points[pointIndex].Pos.X <= x)
                    ++pointIndex;

                if (pointIndex == 1)
                    return points[1].Pos.Y;

                if (pointIndex >= points.Count - 1)
                    return points[^2].Pos.Y;

                var xDiff = points[pointIndex].Pos.X - points[pointIndex - 1].Pos.X;

                if (xDiff == 0.0)
                    return points[pointIndex].Pos.Y;

                var mu = (x - points[pointIndex - 1].Pos.X) / xDiff;
                var a0 = -0.5f * points[pointIndex - 2].Pos.Y + 1.5f * points[pointIndex - 1].Pos.Y - 1.5f * points[pointIndex].Pos.Y + 0.5f * points[pointIndex + 1].Pos.Y;
                var a1 = points[pointIndex - 2].Pos.Y - 2.5f * points[pointIndex - 1].Pos.Y + 2.0f * points[pointIndex].Pos.Y - 0.5f * points[pointIndex + 1].Pos.Y;
                var a2 = -0.5f * points[pointIndex - 2].Pos.Y + 0.5f * points[pointIndex].Pos.Y;
                var a3 = points[pointIndex - 1].Pos.Y;

                return a0 * mu * mu * mu + a1 * mu * mu + a2 * mu + a3;
            }
            case CurveInterpolationMode.Bezier3:
            {
                var xDiff = points[2].Pos.X - points[0].Pos.X;

                if (xDiff == 0.0)
                    return points[1].Pos.Y;

                var mu = (x - points[0].Pos.X) / xDiff;

                return ((1.0f - mu) * (1.0f - mu) * points[0].Pos.Y) + (1.0f - mu) * 2.0f * mu * points[1].Pos.Y + mu * mu * points[2].Pos.Y;
            }
            case CurveInterpolationMode.Bezier4:
            {
                var xDiff = points[3].Pos.X - points[0].Pos.X;

                if (xDiff == 0.0)
                    return points[1].Pos.Y;

                var mu = (x - points[0].Pos.X) / xDiff;

                return (1.0f - mu) * (1.0f - mu) * (1.0f - mu) * points[0].Pos.Y + 3.0f * mu * (1.0f - mu) * (1.0f - mu) * points[1].Pos.Y + 3.0f * mu * mu * (1.0f - mu) * points[2].Pos.Y + mu * mu * mu * points[3].Pos.Y;
            }
            case CurveInterpolationMode.Bezier:
            {
                var xDiff = points.Last().Pos.X - points[0].Pos.X;

                if (xDiff == 0.0f)
                    return points.Last().Pos.Y;

                var tmp = new float[points.Count];

                for (var c = 0; c < points.Count; ++c)
                    tmp[c] = points[c].Pos.Y;

                var mu = (x - points[0].Pos.X) / xDiff;
                var i = points.Count - 1;

                while (i > 0)
                {
                    for (var k = 0; k < i; ++k)
                    {
                        var val = tmp[k] + mu * (tmp[k + 1] - tmp[k]);
                        tmp[k] = val;
                    }

                    --i;
                }

                return tmp[0];
            }
            case CurveInterpolationMode.Constant:
                return points[0].Pos.Y;
        }

        return 0.0f;
    }

    public EmotesTextSoundRecord GetTextSoundEmoteFor(uint emote, Race race, Gender gender, PlayerClass playerClass)
    {
        var emoteTextSound = _emoteTextSounds.LookupByKey(Tuple.Create(emote, (byte)race, (byte)gender, (byte)playerClass));

        if (emoteTextSound != null)
            return emoteTextSound;

        if (_emoteTextSounds.TryGetValue(Tuple.Create(emote, (byte)race, (byte)gender, (byte)0), out emoteTextSound))
            return emoteTextSound;

        return null;
    }

    public float EvaluateExpectedStat(ExpectedStatType stat, uint level, int expansion, uint contentTuningId, PlayerClass unitClass)
    {
        var expectedStatRecord = _expectedStatsByLevel.LookupByKey(Tuple.Create(level, expansion));

        if (expectedStatRecord == null)
            expectedStatRecord = _expectedStatsByLevel.LookupByKey(Tuple.Create(level, -2));

        if (expectedStatRecord == null)
            return 1.0f;

        ExpectedStatModRecord classMod = null;

        switch (unitClass)
        {
            case PlayerClass.Warrior:
                classMod = _cliDB.ExpectedStatModStorage.LookupByKey(4u);

                break;
            case PlayerClass.Paladin:
                classMod = _cliDB.ExpectedStatModStorage.LookupByKey(2u);

                break;
            case PlayerClass.Rogue:
                classMod = _cliDB.ExpectedStatModStorage.LookupByKey(3u);

                break;
            case PlayerClass.Mage:
                classMod = _cliDB.ExpectedStatModStorage.LookupByKey(1u);

                break;
        }

        var contentTuningMods = _expectedStatModsByContentTuning.LookupByKey(contentTuningId);
        var value = 0.0f;

        switch (stat)
        {
            case ExpectedStatType.CreatureHealth:
                value = expectedStatRecord.CreatureHealth;

                if (!contentTuningMods.Empty())
                    value *= contentTuningMods.Sum(expectedStatMod => ExpectedStatModReducer(1.0f, expectedStatMod, stat));

                if (classMod != null)
                    value *= classMod.CreatureHealthMod;

                break;
            case ExpectedStatType.PlayerHealth:
                value = expectedStatRecord.PlayerHealth;

                if (!contentTuningMods.Empty())
                    value *= contentTuningMods.Sum(expectedStatMod => ExpectedStatModReducer(1.0f, expectedStatMod, stat));

                if (classMod != null)
                    value *= classMod.PlayerHealthMod;

                break;
            case ExpectedStatType.CreatureAutoAttackDps:
                value = expectedStatRecord.CreatureAutoAttackDps;

                if (!contentTuningMods.Empty())
                    value *= contentTuningMods.Sum(expectedStatMod => ExpectedStatModReducer(1.0f, expectedStatMod, stat));

                if (classMod != null)
                    value *= classMod.CreatureAutoAttackDPSMod;

                break;
            case ExpectedStatType.CreatureArmor:
                value = expectedStatRecord.CreatureArmor;

                if (!contentTuningMods.Empty())
                    value *= contentTuningMods.Sum(expectedStatMod => ExpectedStatModReducer(1.0f, expectedStatMod, stat));

                if (classMod != null)
                    value *= classMod.CreatureArmorMod;

                break;
            case ExpectedStatType.PlayerMana:
                value = expectedStatRecord.PlayerMana;

                if (!contentTuningMods.Empty())
                    value *= contentTuningMods.Sum(expectedStatMod => ExpectedStatModReducer(1.0f, expectedStatMod, stat));

                if (classMod != null)
                    value *= classMod.PlayerManaMod;

                break;
            case ExpectedStatType.PlayerPrimaryStat:
                value = expectedStatRecord.PlayerPrimaryStat;

                if (!contentTuningMods.Empty())
                    value *= contentTuningMods.Sum(expectedStatMod => ExpectedStatModReducer(1.0f, expectedStatMod, stat));

                if (classMod != null)
                    value *= classMod.PlayerPrimaryStatMod;

                break;
            case ExpectedStatType.PlayerSecondaryStat:
                value = expectedStatRecord.PlayerSecondaryStat;

                if (!contentTuningMods.Empty())
                    value *= contentTuningMods.Sum(expectedStatMod => ExpectedStatModReducer(1.0f, expectedStatMod, stat));

                if (classMod != null)
                    value *= classMod.PlayerSecondaryStatMod;

                break;
            case ExpectedStatType.ArmorConstant:
                value = expectedStatRecord.ArmorConstant;

                if (!contentTuningMods.Empty())
                    value *= contentTuningMods.Sum(expectedStatMod => ExpectedStatModReducer(1.0f, expectedStatMod, stat));

                if (classMod != null)
                    value *= classMod.ArmorConstantMod;

                break;
            case ExpectedStatType.None:
                break;
            case ExpectedStatType.CreatureSpellDamage:
                value = expectedStatRecord.CreatureSpellDamage;

                if (!contentTuningMods.Empty())
                    value *= contentTuningMods.Sum(expectedStatMod => ExpectedStatModReducer(1.0f, expectedStatMod, stat));

                if (classMod != null)
                    value *= classMod.CreatureSpellDamageMod;

                break;
        }

        return value;
    }

    public List<uint> GetFactionTeamList(uint faction)
    {
        return _factionTeams.LookupByKey(faction);
    }

    public List<FriendshipRepReactionRecord> GetFriendshipRepReactions(uint friendshipRepID)
    {
        return _friendshipRepReactions.LookupByKey(friendshipRepID);
    }

    public uint GetGlobalCurveId(GlobalCurve globalCurveType)
    {
        foreach (var globalCurveEntry in _cliDB.GlobalCurveStorage.Values)
            if (globalCurveEntry.Type == globalCurveType)
                return globalCurveEntry.CurveID;

        return 0;
    }

    public List<uint> GetGlyphBindableSpells(uint glyphPropertiesId)
    {
        return _glyphBindableSpells.LookupByKey(glyphPropertiesId);
    }

    public List<uint> GetGlyphRequiredSpecs(uint glyphPropertiesId)
    {
        return _glyphRequiredSpecs.LookupByKey(glyphPropertiesId);
    }

    public HeirloomRecord GetHeirloomByItemId(uint itemId)
    {
        return _heirlooms.LookupByKey(itemId);
    }

    public List<ItemBonusRecord> GetItemBonusList(uint bonusListId)
    {
        return _itemBonusLists.LookupByKey(bonusListId);
    }

    public List<ItemBonusTreeNodeRecord> GetItemBonusSet(uint itemBonusTreeId)
    {
        return _itemBonusTrees.LookupByKey(itemBonusTreeId);
    }

    public uint GetItemBonusListForItemLevelDelta(short delta)
    {
        return _itemLevelDeltaToBonusListContainer.LookupByKey(delta);
    }

    public List<uint> GetDefaultItemBonusTree(uint itemId, ItemContext itemContext)
    {
        List<uint> bonusListIDs = new();

        var proto = _cliDB.ItemSparseStorage.LookupByKey(itemId);

        if (proto == null)
            return bonusListIDs;

        var itemIdRange = _itemToBonusTree.LookupByKey(itemId);

        if (itemIdRange == null)
            return bonusListIDs;

        ushort itemLevelSelectorId = 0;

        foreach (var itemBonusTreeId in itemIdRange)
        {
            uint matchingNodes = 0;

            VisitItemBonusTree(itemBonusTreeId,
                               false,
                               bonusTreeNode =>
                               {
                                   if ((ItemContext)bonusTreeNode.ItemContext == ItemContext.None || itemContext == (ItemContext)bonusTreeNode.ItemContext)
                                       ++matchingNodes;
                               });

            if (matchingNodes != 1)
                continue;

            VisitItemBonusTree(itemBonusTreeId,
                               true,
                               bonusTreeNode =>
                               {
                                   var requiredContext = (ItemContext)bonusTreeNode.ItemContext != ItemContext.ForceToNone ? (ItemContext)bonusTreeNode.ItemContext : ItemContext.None;

                                   if ((ItemContext)bonusTreeNode.ItemContext != ItemContext.None && itemContext != requiredContext)
                                       return;

                                   if (bonusTreeNode.ChildItemBonusListID != 0)
                                       bonusListIDs.Add(bonusTreeNode.ChildItemBonusListID);
                                   else if (bonusTreeNode.ChildItemLevelSelectorID != 0)
                                       itemLevelSelectorId = bonusTreeNode.ChildItemLevelSelectorID;
                               });
        }

        var selector = _cliDB.ItemLevelSelectorStorage.LookupByKey(itemLevelSelectorId);

        if (selector != null)
        {
            var delta = (short)(selector.MinItemLevel - proto.ItemLevel);

            var bonus = GetItemBonusListForItemLevelDelta(delta);

            if (bonus != 0)
                bonusListIDs.Add(bonus);

            var selectorQualitySet = _cliDB.ItemLevelSelectorQualitySetStorage.LookupByKey(selector.ItemLevelSelectorQualitySetID);

            if (selectorQualitySet != null)
            {
                var itemSelectorQualities = _itemLevelQualitySelectorQualities.LookupByKey(selector.ItemLevelSelectorQualitySetID);

                if (itemSelectorQualities != null)
                {
                    var quality = ItemQuality.Uncommon;

                    if (selector.MinItemLevel >= selectorQualitySet.IlvlEpic)
                        quality = ItemQuality.Epic;
                    else if (selector.MinItemLevel >= selectorQualitySet.IlvlRare)
                        quality = ItemQuality.Rare;

                    var itemSelectorQuality = itemSelectorQualities.Find(p => p.Quality < (sbyte)quality);

                    if (itemSelectorQuality != null)
                        bonusListIDs.Add(itemSelectorQuality.QualityItemBonusListID);
                }
            }

            var azeriteUnlockMapping = _azeriteUnlockMappings.LookupByKey((proto.Id, itemContext));

            if (azeriteUnlockMapping != null)
                switch (proto.inventoryType)
                {
                    case InventoryType.Head:
                        bonusListIDs.Add(azeriteUnlockMapping.ItemBonusListHead);

                        break;
                    case InventoryType.Shoulders:
                        bonusListIDs.Add(azeriteUnlockMapping.ItemBonusListShoulders);

                        break;
                    case InventoryType.Chest:
                    case InventoryType.Robe:
                        bonusListIDs.Add(azeriteUnlockMapping.ItemBonusListChest);

                        break;
                }
        }

        return bonusListIDs;
    }

    public List<uint> GetAllItemBonusTreeBonuses(uint itemBonusTreeId)
    {
        List<uint> bonusListIDs = new();

        VisitItemBonusTree(itemBonusTreeId,
                           true,
                           bonusTreeNode =>
                           {
                               if (bonusTreeNode.ChildItemBonusListID != 0)
                                   bonusListIDs.Add(bonusTreeNode.ChildItemBonusListID);
                           });

        return bonusListIDs;
    }

    public ItemChildEquipmentRecord GetItemChildEquipment(uint itemId)
    {
        return _itemChildEquipment.LookupByKey(itemId);
    }

    public ItemClassRecord GetItemClassByOldEnum(ItemClass itemClass)
    {
        return _itemClassByOldEnum[(int)itemClass];
    }

    public List<ItemLimitCategoryConditionRecord> GetItemLimitCategoryConditions(uint categoryId)
    {
        return _itemCategoryConditions.LookupByKey(categoryId);
    }

    public uint GetItemDisplayId(uint itemId, uint appearanceModId)
    {
        var modifiedAppearance = GetItemModifiedAppearance(itemId, appearanceModId);

        if (modifiedAppearance != null)
        {
            var itemAppearance = _cliDB.ItemAppearanceStorage.LookupByKey((uint)modifiedAppearance.ItemAppearanceID);

            if (itemAppearance != null)
                return itemAppearance.ItemDisplayInfoID;
        }

        return 0;
    }

    public ItemModifiedAppearanceRecord GetItemModifiedAppearance(uint itemId, uint appearanceModId)
    {
        var itemModifiedAppearance = _itemModifiedAppearancesByItem.LookupByKey(itemId | (appearanceModId << 24));

        if (itemModifiedAppearance != null)
            return itemModifiedAppearance;

        // Fall back to unmodified appearance
        if (appearanceModId != 0)
        {
            itemModifiedAppearance = _itemModifiedAppearancesByItem.LookupByKey(itemId);

            if (itemModifiedAppearance != null)
                return itemModifiedAppearance;
        }

        return null;
    }

    public ItemModifiedAppearanceRecord GetDefaultItemModifiedAppearance(uint itemId)
    {
        return _itemModifiedAppearancesByItem.LookupByKey(itemId);
    }

    public List<ItemSetSpellRecord> GetItemSetSpells(uint itemSetId)
    {
        return _itemSetSpells.LookupByKey(itemSetId);
    }

    public List<ItemSpecOverrideRecord> GetItemSpecOverrides(uint itemId)
    {
        return _itemSpecOverrides.LookupByKey(itemId);
    }

    public JournalTierRecord GetJournalTier(uint index)
    {
        if (index < _journalTiersByIndex.Count)
            return _journalTiersByIndex[(int)index];

        return null;
    }

    public LFGDungeonsRecord GetLfgDungeon(uint mapId, Difficulty difficulty)
    {
        foreach (var dungeon in _cliDB.LFGDungeonsStorage.Values)
            if (dungeon.MapID == mapId && dungeon.DifficultyID == difficulty)
                return dungeon;

        return null;
    }

    public uint GetDefaultMapLight(uint mapId)
    {
        foreach (var light in _cliDB.LightStorage.Values.Reverse())
            if (light.ContinentID == mapId && light.GameCoords.X == 0.0f && light.GameCoords is { Y: 0.0f, Z: 0.0f })
                return light.Id;

        return 0;
    }

    public uint GetLiquidFlags(uint liquidType)
    {
        var liq = _cliDB.LiquidTypeStorage.LookupByKey(liquidType);

        if (liq != null)
            return 1u << liq.SoundBank;

        return 0;
    }

    public MapDifficultyRecord GetDefaultMapDifficulty(uint mapId)
    {
        var notUsed = Difficulty.None;

        return GetDefaultMapDifficulty(mapId, ref notUsed);
    }

    public MapDifficultyRecord GetDefaultMapDifficulty(uint mapId, ref Difficulty difficulty)
    {
        var dicMapDiff = _mapDifficulties.LookupByKey(mapId);

        if (dicMapDiff == null)
            return null;

        if (dicMapDiff.Empty())
            return null;

        foreach (var pair in dicMapDiff)
        {
            var difficultyEntry = _cliDB.DifficultyStorage.LookupByKey(pair.Key);

            if (difficultyEntry == null)
                continue;

            if (difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.Default))
            {
                difficulty = (Difficulty)pair.Key;

                return pair.Value;
            }
        }

        difficulty = (Difficulty)dicMapDiff.First().Key;

        return dicMapDiff.First().Value;
    }

    public MapDifficultyRecord GetMapDifficultyData(uint mapId, Difficulty difficulty)
    {
        var dictionaryMapDiff = _mapDifficulties.LookupByKey(mapId);

        if (dictionaryMapDiff == null)
            return null;

        var mapDifficulty = dictionaryMapDiff.LookupByKey((uint)difficulty);

        if (mapDifficulty == null)
            return null;

        return mapDifficulty;
    }

    public MapDifficultyRecord GetDownscaledMapDifficultyData(uint mapId, ref Difficulty difficulty)
    {
        var diffEntry = _cliDB.DifficultyStorage.LookupByKey((uint)difficulty);

        if (diffEntry == null)
            return GetDefaultMapDifficulty(mapId, ref difficulty);

        var tmpDiff = difficulty;
        var mapDiff = GetMapDifficultyData(mapId, tmpDiff);

        while (mapDiff == null)
        {
            tmpDiff = (Difficulty)diffEntry.FallbackDifficultyID;
            diffEntry = _cliDB.DifficultyStorage.LookupByKey((uint)tmpDiff);

            if (diffEntry == null)
                return GetDefaultMapDifficulty(mapId, ref difficulty);

            // pull new data
            mapDiff = GetMapDifficultyData(mapId, tmpDiff); // we are 10 normal or 25 normal
        }

        difficulty = tmpDiff;

        return mapDiff;
    }

    public List<Tuple<uint, PlayerConditionRecord>> GetMapDifficultyConditions(uint mapDifficultyId)
    {
        return _mapDifficultyConditions.LookupByKey(mapDifficultyId);
    }

    public MountRecord GetMount(uint spellId)
    {
        return _mountsBySpellId.LookupByKey(spellId);
    }

    public MountRecord GetMountById(uint id)
    {
        return _cliDB.MountStorage.LookupByKey(id);
    }

    public List<MountTypeXCapabilityRecord> GetMountCapabilities(uint mountType)
    {
        return _mountCapabilitiesByType.LookupByKey(mountType);
    }

    public List<MountXDisplayRecord> GetMountDisplays(uint mountId)
    {
        return _mountDisplays.LookupByKey(mountId);
    }

    public string GetNameGenEntry(uint race, uint gender)
    {
        var listNameGen = _nameGenData.LookupByKey(race);

        if (listNameGen == null)
            return "";

        if (listNameGen[gender].Empty())
            return "";

        return listNameGen[gender].SelectRandom().Name;
    }

    public ResponseCodes ValidateName(string name, Locale locale)
    {
        foreach (var testName in _nameValidators[(int)locale])
            if (testName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return ResponseCodes.CharNameProfane;

        // regexes at TOTAL_LOCALES are loaded from NamesReserved which is not locale specific
        foreach (var testName in _nameValidators[(int)Locale.Total])
            if (testName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return ResponseCodes.CharNameReserved;

        return ResponseCodes.CharNameSuccess;
    }

    public uint GetNumTalentsAtLevel(uint level, PlayerClass playerClass)
    {
        var numTalentsAtLevel = _cliDB.NumTalentsAtLevelStorage.LookupByKey(level);

        if (numTalentsAtLevel == null)
            numTalentsAtLevel = _cliDB.NumTalentsAtLevelStorage.LastOrDefault().Value;

        if (numTalentsAtLevel != null)
            return playerClass switch
            {
                PlayerClass.Deathknight => numTalentsAtLevel.NumTalentsDeathKnight,
                PlayerClass.DemonHunter => numTalentsAtLevel.NumTalentsDemonHunter,
                _                       => numTalentsAtLevel.NumTalents,
            };

        return 0;
    }

    public ParagonReputationRecord GetParagonReputation(uint factionId)
    {
        return _paragonReputations.LookupByKey(factionId);
    }

    public PvpDifficultyRecord GetBattlegroundBracketByLevel(uint mapid, uint level)
    {
        PvpDifficultyRecord maxEntry = null; // used for level > max listed level case

        foreach (var entry in _cliDB.PvpDifficultyStorage.Values)
        {
            // skip unrelated and too-high brackets
            if (entry.MapID != mapid || entry.MinLevel > level)
                continue;

            // exactly fit
            if (entry.MaxLevel >= level)
                return entry;

            // remember for possible out-of-range case (search higher from existed)
            if (maxEntry == null || maxEntry.MaxLevel < entry.MaxLevel)
                maxEntry = entry;
        }

        return maxEntry;
    }

    public PvpDifficultyRecord GetBattlegroundBracketById(uint mapid, BattlegroundBracketId id)
    {
        foreach (var entry in _cliDB.PvpDifficultyStorage.Values)
            if (entry.MapID == mapid && entry.GetBracketId() == id)
                return entry;

        return null;
    }

    public uint GetRequiredLevelForPvpTalentSlot(byte slot, PlayerClass playerClass)
    {
        if (_pvpTalentSlotUnlock[slot] != null)
        {
            switch (playerClass)
            {
                case PlayerClass.Deathknight:
                    return _pvpTalentSlotUnlock[slot].DeathKnightLevelRequired;
                case PlayerClass.DemonHunter:
                    return _pvpTalentSlotUnlock[slot].DemonHunterLevelRequired;
            }

            return _pvpTalentSlotUnlock[slot].LevelRequired;
        }

        return 0;
    }

    public int GetPvpTalentNumSlotsAtLevel(uint level, PlayerClass playerClass)
    {
        var slots = 0;

        for (byte slot = 0; slot < PlayerConst.MaxPvpTalentSlots; ++slot)
            if (level >= GetRequiredLevelForPvpTalentSlot(slot, playerClass))
                ++slots;

        return slots;
    }

    public List<QuestLineXQuestRecord> GetQuestsForQuestLine(uint questLineId)
    {
        return _questsByQuestLine.LookupByKey(questLineId);
    }

    public bool TryGetQuestsForQuestLine(uint questLineId, out List<QuestLineXQuestRecord> questLineXQuestRecords)
    {
        return _questsByQuestLine.TryGetValue(questLineId, out questLineXQuestRecords);
    }

    public List<QuestPackageItemRecord> GetQuestPackageItems(uint questPackageID)
    {
        if (_questPackages.ContainsKey(questPackageID))
            return _questPackages[questPackageID].Item1;

        return null;
    }

    public List<QuestPackageItemRecord> GetQuestPackageItemsFallback(uint questPackageID)
    {
        return _questPackages.LookupByKey(questPackageID).Item2;
    }

    public uint GetQuestUniqueBitFlag(uint questId)
    {
        var v2 = _cliDB.QuestV2Storage.LookupByKey(questId);

        if (v2 == null)
            return 0;

        return v2.UniqueBitFlag;
    }

    public List<uint> GetPhasesForGroup(uint group)
    {
        return _phasesByGroup.LookupByKey(group);
    }

    public PowerTypeRecord GetPowerTypeEntry(PowerType power)
    {
        if (!_powerTypes.ContainsKey(power))
            return null;

        return _powerTypes[power];
    }

    public PowerTypeRecord GetPowerTypeByName(string name)
    {
        foreach (var powerType in _cliDB.PowerTypeStorage.Values)
        {
            var powerName = powerType.NameGlobalStringTag;

            if (powerName.ToLower() == name)
                return powerType;

            powerName = powerName.Replace("_", "");

            if (powerName == name)
                return powerType;
        }

        return null;
    }

    public byte GetPvpItemLevelBonus(uint itemId)
    {
        return _pvpItemBonus.LookupByKey(itemId);
    }

    public List<RewardPackXCurrencyTypeRecord> GetRewardPackCurrencyTypesByRewardID(uint rewardPackID)
    {
        return _rewardPackCurrencyTypes.LookupByKey(rewardPackID);
    }

    public List<RewardPackXItemRecord> GetRewardPackItemsByRewardID(uint rewardPackID)
    {
        return _rewardPackItems.LookupByKey(rewardPackID);
    }

    public ShapeshiftFormModelData GetShapeshiftFormModelData(Race race, Gender gender, ShapeShiftForm form)
    {
        return _chrCustomizationChoicesForShapeshifts.LookupByKey(Tuple.Create((byte)race, (byte)gender, (byte)form));
    }

    public List<SkillLineRecord> GetSkillLinesForParentSkill(uint parentSkillId)
    {
        return _skillLinesByParentSkillLine.LookupByKey(parentSkillId);
    }

    public List<SkillLineAbilityRecord> GetSkillLineAbilitiesBySkill(uint skillId)
    {
        return _skillLineAbilitiesBySkillupSkill.LookupByKey(skillId);
    }

    public SkillRaceClassInfoRecord GetSkillRaceClassInfo(uint skill, Race race, PlayerClass playerClass)
    {
        var bounds = _skillRaceClassInfoBySkill.LookupByKey(skill);

        foreach (var skllRaceClassInfo in bounds)
        {
            if (skllRaceClassInfo.RaceMask != 0 && !Convert.ToBoolean(skllRaceClassInfo.RaceMask & SharedConst.GetMaskForRace(race)))
                continue;

            if (skllRaceClassInfo.ClassMask != 0 && !Convert.ToBoolean(skllRaceClassInfo.ClassMask & (1 << ((byte)playerClass - 1))))
                continue;

            return skllRaceClassInfo;
        }

        return null;
    }

    public List<SkillRaceClassInfoRecord> GetSkillRaceClassInfo(uint skill)
    {
        return _skillRaceClassInfoBySkill.LookupByKey(skill);
    }

    public SoulbindConduitRankRecord GetSoulbindConduitRank(int soulbindConduitId, int rank)
    {
        return _soulbindConduitRanks.LookupByKey(Tuple.Create(soulbindConduitId, rank));
    }

    public List<SpecializationSpellsRecord> GetSpecializationSpells(uint specId)
    {
        return _specializationSpellsBySpec.LookupByKey(specId);
    }

    public bool IsSpecSetMember(int specSetId, uint specId)
    {
        return _specsBySpecSet.Contains(Tuple.Create(specSetId, specId));
    }

    public bool IsValidSpellFamiliyName(SpellFamilyNames family)
    {
        return _spellFamilyNames.Contains((byte)family);
    }

    public List<SpellProcsPerMinuteModRecord> GetSpellProcsPerMinuteMods(uint spellprocsPerMinuteId)
    {
        return _spellProcsPerMinuteMods.LookupByKey(spellprocsPerMinuteId);
    }

    public List<SpellVisualMissileRecord> GetSpellVisualMissiles(int spellVisualMissileSetId)
    {
        return _spellVisualMissilesBySet.LookupByKey(spellVisualMissileSetId);
    }

    public List<TalentRecord> GetTalentsByPosition(PlayerClass playerClass, uint tier, uint column)
    {
        return _talentsByPosition[(int)playerClass][tier][column];
    }

    public bool IsTotemCategoryCompatibleWith(uint itemTotemCategoryId, uint requiredTotemCategoryId)
    {
        if (requiredTotemCategoryId == 0)
            return true;

        if (itemTotemCategoryId == 0)
            return false;

        var itemEntry = _cliDB.TotemCategoryStorage.LookupByKey(itemTotemCategoryId);

        if (itemEntry == null)
            return false;

        var reqEntry = _cliDB.TotemCategoryStorage.LookupByKey(requiredTotemCategoryId);

        if (reqEntry == null)
            return false;

        if (itemEntry.TotemCategoryType != reqEntry.TotemCategoryType)
            return false;

        return (itemEntry.TotemCategoryMask & reqEntry.TotemCategoryMask) == reqEntry.TotemCategoryMask;
    }

    public bool IsToyItem(uint toy)
    {
        return _toys.Contains(toy);
    }

    public TransmogIllusionRecord GetTransmogIllusionForEnchantment(uint spellItemEnchantmentId)
    {
        return _transmogIllusionsByEnchantmentId.LookupByKey(spellItemEnchantmentId);
    }

    public List<TransmogSetRecord> GetTransmogSetsForItemModifiedAppearance(uint itemModifiedAppearanceId)
    {
        return _transmogSetsByItemModifiedAppearance.LookupByKey(itemModifiedAppearanceId);
    }

    public List<TransmogSetItemRecord> GetTransmogSetItems(uint transmogSetId)
    {
        return _transmogSetItemsByTransmogSet.LookupByKey(transmogSetId);
    }

    public bool GetUiMapPosition(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapSystem system, bool local, out Vector2 newPos)
    {
        return GetUiMapPosition(x, y, z, mapId, areaId, wmoDoodadPlacementId, wmoGroupId, system, local, out _, out newPos);
    }

    public bool GetUiMapPosition(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapSystem system, bool local, out int uiMapId)
    {
        return GetUiMapPosition(x, y, z, mapId, areaId, wmoDoodadPlacementId, wmoGroupId, system, local, out uiMapId, out _);
    }

    public bool GetUiMapPosition(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapSystem system, bool local, out int uiMapId, out Vector2 newPos)
    {
        uiMapId = -1;
        newPos = new Vector2();

        var uiMapAssignment = FindNearestMapAssignment(x, y, z, mapId, areaId, wmoDoodadPlacementId, wmoGroupId, system);

        if (uiMapAssignment == null)
            return false;

        uiMapId = uiMapAssignment.UiMapID;

        Vector2 relativePosition = new(0.5f, 0.5f);
        Vector2 regionSize = new(uiMapAssignment.Region[1].X - uiMapAssignment.Region[0].X, uiMapAssignment.Region[1].Y - uiMapAssignment.Region[0].Y);

        if (regionSize.X > 0.0f)
            relativePosition.X = (x - uiMapAssignment.Region[0].X) / regionSize.X;

        if (regionSize.Y > 0.0f)
            relativePosition.Y = (y - uiMapAssignment.Region[0].Y) / regionSize.Y;

        // x any y are swapped
        Vector2 uiPosition = new(((1.0f - (1.0f - relativePosition.Y)) * uiMapAssignment.UiMin.X) + ((1.0f - relativePosition.Y) * uiMapAssignment.UiMax.X), ((1.0f - (1.0f - relativePosition.X)) * uiMapAssignment.UiMin.Y) + ((1.0f - relativePosition.X) * uiMapAssignment.UiMax.Y));

        if (!local)
            uiPosition = CalculateGlobalUiMapPosition(uiMapAssignment.UiMapID, uiPosition);

        newPos = uiPosition;

        return true;
    }

    public bool Zone2MapCoordinates(uint areaId, ref float x, ref float y)
    {
        var areaEntry = _cliDB.AreaTableStorage.LookupByKey(areaId);

        if (areaEntry == null)
            return false;

        foreach (var assignment in _uiMapAssignmentByArea[(int)UiMapSystem.World].LookupByKey(areaId))
        {
            if (assignment.MapID >= 0 && assignment.MapID != areaEntry.ContinentID)
                continue;

            var tmpY = (y - assignment.UiMax.Y) / (assignment.UiMin.Y - assignment.UiMax.Y);
            var tmpX = (x - assignment.UiMax.X) / (assignment.UiMin.X - assignment.UiMax.X);
            x = assignment.Region[0].X + tmpY * (assignment.Region[1].X - assignment.Region[0].X);
            y = assignment.Region[0].Y + tmpX * (assignment.Region[1].Y - assignment.Region[0].Y);

            return true;
        }

        return false;
    }

    public void Map2ZoneCoordinates(int areaId, ref float x, ref float y)
    {
        if (!GetUiMapPosition(x, y, 0.0f, -1, areaId, 0, 0, UiMapSystem.World, true, out Vector2 zoneCoords))
            return;

        x = zoneCoords.Y * 100.0f;
        y = zoneCoords.X * 100.0f;
    }

    public bool IsUiMapPhase(int phaseId)
    {
        return _uiMapPhases.Contains(phaseId);
    }

    public WMOAreaTableRecord GetWmoAreaTable(int rootId, int adtId, int groupId)
    {
        return _wmoAreaTableLookup.LookupByKey(Tuple.Create((short)rootId, (sbyte)adtId, groupId));
    }

    public bool HasItemCurrencyCost(uint itemId)
    {
        return _itemsWithCurrencyCost.Contains(itemId);
    }

    public Dictionary<uint, Dictionary<uint, MapDifficultyRecord>> GetMapDifficulties()
    {
        return _mapDifficulties;
    }

    public void AddDB2<T>(uint tableHash, DB6Storage<T> store) where T : new()
    {
        lock (Storage)
        {
            Storage[tableHash] = store;
        }
    }

    private static CurveInterpolationMode DetermineCurveType(CurveRecord curve, List<CurvePointRecord> points)
    {
        switch (curve.Type)
        {
            case 1:
                return points.Count < 4 ? CurveInterpolationMode.Cosine : CurveInterpolationMode.CatmullRom;
            case 2:
            {
                switch (points.Count)
                {
                    case 1:
                        return CurveInterpolationMode.Constant;
                    case 2:
                        return CurveInterpolationMode.Linear;
                    case 3:
                        return CurveInterpolationMode.Bezier3;
                    case 4:
                        return CurveInterpolationMode.Bezier4;
                }

                return CurveInterpolationMode.Bezier;
            }
            case 3:
                return CurveInterpolationMode.Cosine;
        }

        return points.Count != 1 ? CurveInterpolationMode.Linear : CurveInterpolationMode.Constant;
    }

    private float ExpectedStatModReducer(float mod, ContentTuningXExpectedRecord contentTuningXExpected, ExpectedStatType stat)
    {
        if (contentTuningXExpected == null)
            return mod;

        //if (contentTuningXExpected->MinMythicPlusSeasonID)
        //    if (MythicPlusSeasonEntry const* mythicPlusSeason = sMythicPlusSeasonStore.LookupEntry(contentTuningXExpected->MinMythicPlusSeasonID))
        //        if (MythicPlusSubSeason < mythicPlusSeason->SubSeason)
        //            return mod;

        //if (contentTuningXExpected->MaxMythicPlusSeasonID)
        //    if (MythicPlusSeasonEntry const* mythicPlusSeason = sMythicPlusSeasonStore.LookupEntry(contentTuningXExpected->MaxMythicPlusSeasonID))
        //        if (MythicPlusSubSeason >= mythicPlusSeason->SubSeason)
        //            return mod;

        var expectedStatMod = _cliDB.ExpectedStatModStorage.LookupByKey((uint)contentTuningXExpected.ExpectedStatModID);

        switch (stat)
        {
            case ExpectedStatType.CreatureHealth:
                return mod * expectedStatMod.CreatureHealthMod;
            case ExpectedStatType.PlayerHealth:
                return mod * expectedStatMod.PlayerHealthMod;
            case ExpectedStatType.CreatureAutoAttackDps:
                return mod * expectedStatMod.CreatureAutoAttackDPSMod;
            case ExpectedStatType.CreatureArmor:
                return mod * expectedStatMod.CreatureArmorMod;
            case ExpectedStatType.PlayerMana:
                return mod * expectedStatMod.PlayerManaMod;
            case ExpectedStatType.PlayerPrimaryStat:
                return mod * expectedStatMod.PlayerPrimaryStatMod;
            case ExpectedStatType.PlayerSecondaryStat:
                return mod * expectedStatMod.PlayerSecondaryStatMod;
            case ExpectedStatType.ArmorConstant:
                return mod * expectedStatMod.ArmorConstantMod;
            case ExpectedStatType.CreatureSpellDamage:
                return mod * expectedStatMod.CreatureSpellDamageMod;
        }

        return mod;

        // int32 MythicPlusSubSeason = 0;
    }

    private void VisitItemBonusTree(uint itemBonusTreeId, bool visitChildren, Action<ItemBonusTreeNodeRecord> visitor)
    {
        var bonusTreeNodeList = _itemBonusTrees.LookupByKey(itemBonusTreeId);

        if (bonusTreeNodeList.Empty())
            return;

        foreach (var bonusTreeNode in bonusTreeNodeList)
        {
            visitor(bonusTreeNode);

            if (visitChildren && bonusTreeNode.ChildItemBonusTreeID != 0)
                VisitItemBonusTree(bonusTreeNode.ChildItemBonusTreeID, true, visitor);
        }
    }

    private void LoadAzeriteEmpoweredItemUnlockMappings(MultiMap<uint, AzeriteUnlockMappingRecord> azeriteUnlockMappingsBySet, uint itemId)
    {
        var itemIdRange = _itemToBonusTree.LookupByKey(itemId);

        if (itemIdRange == null)
            return;

        foreach (var itemTreeItr in itemIdRange)
            VisitItemBonusTree(itemTreeItr,
                               true,
                               bonusTreeNode =>
                               {
                                   if (bonusTreeNode.ChildItemBonusListID == 0 && bonusTreeNode.ChildItemLevelSelectorID != 0)
                                   {
                                       var selector = _cliDB.ItemLevelSelectorStorage.LookupByKey(bonusTreeNode.ChildItemLevelSelectorID);

                                       if (selector == null)
                                           return;

                                       var azeriteUnlockMappings = azeriteUnlockMappingsBySet.LookupByKey(selector.AzeriteUnlockMappingSet);

                                       if (azeriteUnlockMappings != null)
                                       {
                                           AzeriteUnlockMappingRecord selectedAzeriteUnlockMapping = null;

                                           foreach (var azeriteUnlockMapping in azeriteUnlockMappings)
                                           {
                                               if (azeriteUnlockMapping.ItemLevel > selector.MinItemLevel ||
                                                   (selectedAzeriteUnlockMapping != null && selectedAzeriteUnlockMapping.ItemLevel > azeriteUnlockMapping.ItemLevel))
                                                   continue;

                                               selectedAzeriteUnlockMapping = azeriteUnlockMapping;
                                           }

                                           if (selectedAzeriteUnlockMapping != null)
                                               _azeriteUnlockMappings[(itemId, (ItemContext)bonusTreeNode.ItemContext)] = selectedAzeriteUnlockMapping;
                                       }
                                   }
                               });
    }

    private bool CheckUiMapAssignmentStatus(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapAssignmentRecord uiMapAssignment, out UiMapAssignmentStatus status)
    {
        status = new UiMapAssignmentStatus
        {
            UiMapAssignment = uiMapAssignment
        };

        // x,y not in region
        if (x < uiMapAssignment.Region[0].X || x > uiMapAssignment.Region[1].X || y < uiMapAssignment.Region[0].Y || y > uiMapAssignment.Region[1].Y)
        {
            float xDiff, yDiff;

            if (x >= uiMapAssignment.Region[0].X)
            {
                xDiff = 0.0f;

                if (x > uiMapAssignment.Region[1].X)
                    xDiff = x - uiMapAssignment.Region[0].X;
            }
            else
            {
                xDiff = uiMapAssignment.Region[0].X - x;
            }

            if (y >= uiMapAssignment.Region[0].Y)
            {
                yDiff = 0.0f;

                if (y > uiMapAssignment.Region[1].Y)
                    yDiff = y - uiMapAssignment.Region[0].Y;
            }
            else
            {
                yDiff = uiMapAssignment.Region[0].Y - y;
            }

            status.Outside.DistanceToRegionEdgeSquared = xDiff * xDiff + yDiff * yDiff;
        }
        else
        {
            status.Inside.DistanceToRegionCenterSquared =
                (x - (uiMapAssignment.Region[0].X + uiMapAssignment.Region[1].X) * 0.5f) * (x - (uiMapAssignment.Region[0].X + uiMapAssignment.Region[1].X) * 0.5f) + (y - (uiMapAssignment.Region[0].Y + uiMapAssignment.Region[1].Y) * 0.5f) * (y - (uiMapAssignment.Region[0].Y + uiMapAssignment.Region[1].Y) * 0.5f);

            status.Outside.DistanceToRegionEdgeSquared = 0.0f;
        }

        // z not in region
        if (z < uiMapAssignment.Region[0].Z || z > uiMapAssignment.Region[1].Z)
        {
            if (z < uiMapAssignment.Region[1].Z)
            {
                if (z < uiMapAssignment.Region[0].Z)
                    status.Outside.DistanceToRegionBottom = Math.Min(uiMapAssignment.Region[0].Z - z, 10000.0f);
            }
            else
            {
                status.Outside.DistanceToRegionTop = Math.Min(z - uiMapAssignment.Region[1].Z, 10000.0f);
            }
        }
        else
        {
            status.Outside.DistanceToRegionTop = 0.0f;
            status.Outside.DistanceToRegionBottom = 0.0f;
            status.Inside.DistanceToRegionBottom = Math.Min(uiMapAssignment.Region[0].Z - z, 10000.0f);
        }

        if (areaId != 0 && uiMapAssignment.AreaID != 0)
        {
            sbyte areaPriority = 0;

            while (areaId != uiMapAssignment.AreaID)
            {
                var areaEntry = _cliDB.AreaTableStorage.LookupByKey((uint)areaId);

                if (areaEntry != null)
                {
                    areaId = areaEntry.ParentAreaID;
                    ++areaPriority;
                }
                else
                {
                    return false;
                }
            }

            status.AreaPriority = areaPriority;
        }

        if (mapId >= 0 && uiMapAssignment.MapID >= 0)
        {
            if (mapId != uiMapAssignment.MapID)
            {
                var mapEntry = _cliDB.MapStorage.LookupByKey((uint)mapId);

                if (mapEntry != null)
                {
                    if (mapEntry.ParentMapID == uiMapAssignment.MapID)
                        status.MapPriority = 1;
                    else if (mapEntry.CosmeticParentMapID == uiMapAssignment.MapID)
                        status.MapPriority = 2;
                    else
                        return false;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                status.MapPriority = 0;
            }
        }

        if (wmoGroupId != 0 || wmoDoodadPlacementId != 0)
            if (uiMapAssignment.WmoGroupID != 0 || uiMapAssignment.WmoDoodadPlacementID != 0)
            {
                var hasDoodadPlacement = false;

                if (wmoDoodadPlacementId != 0 && uiMapAssignment.WmoDoodadPlacementID != 0)
                {
                    if (wmoDoodadPlacementId != uiMapAssignment.WmoDoodadPlacementID)
                        return false;

                    hasDoodadPlacement = true;
                }

                if (wmoGroupId != 0 && uiMapAssignment.WmoGroupID != 0)
                {
                    if (wmoGroupId != uiMapAssignment.WmoGroupID)
                        return false;

                    if (hasDoodadPlacement)
                        status.WmoPriority = 0;
                    else
                        status.WmoPriority = 2;
                }
                else if (hasDoodadPlacement)
                {
                    status.WmoPriority = 1;
                }
            }

        return true;
    }

    private UiMapAssignmentRecord FindNearestMapAssignment(float x, float y, float z, int mapId, int areaId, int wmoDoodadPlacementId, int wmoGroupId, UiMapSystem system)
    {
        UiMapAssignmentStatus nearestMapAssignment = new();

        var iterateUiMapAssignments = new Action<MultiMap<int, UiMapAssignmentRecord>, int>((assignments, id) =>
        {
            foreach (var assignment in assignments.LookupByKey(id))
                if (CheckUiMapAssignmentStatus(x, y, z, mapId, areaId, wmoDoodadPlacementId, wmoGroupId, assignment, out var status))
                    if (status < nearestMapAssignment)
                        nearestMapAssignment = status;
        });

        iterateUiMapAssignments(_uiMapAssignmentByWmoGroup[(int)system], wmoGroupId);
        iterateUiMapAssignments(_uiMapAssignmentByWmoDoodadPlacement[(int)system], wmoDoodadPlacementId);

        var areaEntry = _cliDB.AreaTableStorage.LookupByKey((uint)areaId);

        while (areaEntry != null)
        {
            iterateUiMapAssignments(_uiMapAssignmentByArea[(int)system], (int)areaEntry.Id);
            areaEntry = _cliDB.AreaTableStorage.LookupByKey(areaEntry.ParentAreaID);
        }

        if (mapId > 0)
        {
            var mapEntry = _cliDB.MapStorage.LookupByKey((uint)mapId);

            if (mapEntry != null)
            {
                iterateUiMapAssignments(_uiMapAssignmentByMap[(int)system], (int)mapEntry.Id);

                if (mapEntry.ParentMapID >= 0)
                    iterateUiMapAssignments(_uiMapAssignmentByMap[(int)system], mapEntry.ParentMapID);

                if (mapEntry.CosmeticParentMapID >= 0)
                    iterateUiMapAssignments(_uiMapAssignmentByMap[(int)system], mapEntry.CosmeticParentMapID);
            }
        }

        return nearestMapAssignment.UiMapAssignment;
    }

    private Vector2 CalculateGlobalUiMapPosition(int uiMapID, Vector2 uiPosition)
    {
        var uiMap = _cliDB.UiMapStorage.LookupByKey((uint)uiMapID);

        while (uiMap != null)
        {
            if (uiMap.Type <= UiMapType.Continent)
                break;

            if (!_uiMapBounds.TryGetValue((int)uiMap.Id, out var bounds) || !bounds.IsUiAssignment)
                break;

            uiPosition.X = ((1.0f - uiPosition.X) * bounds.Bounds[1]) + (bounds.Bounds[3] * uiPosition.X);
            uiPosition.Y = ((1.0f - uiPosition.Y) * bounds.Bounds[0]) + (bounds.Bounds[2] * uiPosition.Y);

            uiMap = _cliDB.UiMapStorage.LookupByKey((uint)uiMap.ParentUiMapID);
        }

        return uiPosition;
    }

    private delegate bool AllowedHotfixOptionalData(byte[] data);
}