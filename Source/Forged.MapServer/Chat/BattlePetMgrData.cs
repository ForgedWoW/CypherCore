// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.Globals;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Chat;

public class BattlePetMgrData
{
    public Dictionary<uint, Dictionary<BattlePetState, int>> BattlePetBreedStates = new();
    public Dictionary<uint, Dictionary<BattlePetState, int>> BattlePetSpeciesStates = new();
    private readonly WorldDatabase _worldDatabase;
    private readonly CliDB _cliDB;
    private readonly GameObjectManager _objectManager;
    private readonly Dictionary<uint, BattlePetSpeciesRecord> _battlePetSpeciesByCreature = new();
    private readonly Dictionary<uint, BattlePetSpeciesRecord> _battlePetSpeciesBySpell = new();
    private readonly MultiMap<uint, byte> _availableBreedsPerSpecies = new();
    private readonly Dictionary<uint, BattlePetBreedQuality> _defaultQualityPerSpecies = new();

    public BattlePetMgrData(LoginDatabase loginDatabase, WorldDatabase worldDatabase, CliDB cliDB, GameObjectManager objectManager)
    {
        _worldDatabase = worldDatabase;
        _cliDB = cliDB;
        _objectManager = objectManager;
        var result = loginDatabase.Query("SELECT MAX(guid) FROM battle_pets");

        if (!result.IsEmpty())
            _objectManager.GetGenerator(HighGuid.BattlePet).Set(result.Read<ulong>(0) + 1);

        foreach (var battlePetSpecies in _cliDB.BattlePetSpeciesStorage.Values)
        {
            var creatureId = battlePetSpecies.CreatureID;

            if (creatureId != 0)
                _battlePetSpeciesByCreature[creatureId] = battlePetSpecies;
        }

        foreach (var battlePetBreedState in _cliDB.BattlePetBreedStateStorage.Values)
        {
            if (!BattlePetBreedStates.ContainsKey(battlePetBreedState.BattlePetBreedID))
                BattlePetBreedStates[battlePetBreedState.BattlePetBreedID] = new Dictionary<BattlePetState, int>();

            BattlePetBreedStates[battlePetBreedState.BattlePetBreedID][(BattlePetState)battlePetBreedState.BattlePetStateID] = battlePetBreedState.Value;
        }

        foreach (var battlePetSpeciesState in _cliDB.BattlePetSpeciesStateStorage.Values)
        {
            if (!BattlePetSpeciesStates.ContainsKey(battlePetSpeciesState.BattlePetSpeciesID))
                BattlePetSpeciesStates[battlePetSpeciesState.BattlePetSpeciesID] = new Dictionary<BattlePetState, int>();

            BattlePetSpeciesStates[battlePetSpeciesState.BattlePetSpeciesID][(BattlePetState)battlePetSpeciesState.BattlePetStateID] = battlePetSpeciesState.Value;
        }

        LoadAvailablePetBreeds();
        LoadDefaultPetQualities();
    }

    public void AddBattlePetSpeciesBySpell(uint spellId, BattlePetSpeciesRecord speciesEntry)
    {
        _battlePetSpeciesBySpell[spellId] = speciesEntry;
    }

    public BattlePetSpeciesRecord GetBattlePetSpeciesByCreature(uint creatureId)
    {
        return _battlePetSpeciesByCreature.LookupByKey(creatureId);
    }

    public BattlePetSpeciesRecord GetBattlePetSpeciesBySpell(uint spellId)
    {
        return _battlePetSpeciesBySpell.LookupByKey(spellId);
    }

    public ushort RollPetBreed(uint species)
    {
        var list = _availableBreedsPerSpecies.LookupByKey(species);

        if (list.Empty())
            return 3; // default B/B

        return list.SelectRandom();
    }

    public BattlePetBreedQuality GetDefaultPetQuality(uint species)
    {
        if (!_defaultQualityPerSpecies.ContainsKey(species))
            return BattlePetBreedQuality.Poor; // Default

        return _defaultQualityPerSpecies[species];
    }

    public uint SelectPetDisplay(BattlePetSpeciesRecord speciesEntry)
    {
        var creatureTemplate = _objectManager.GetCreatureTemplate(speciesEntry.CreatureID);

        if (creatureTemplate != null)
            if (!speciesEntry.GetFlags().HasFlag(BattlePetSpeciesFlags.RandomDisplay))
            {
                var creatureModel = creatureTemplate.GetRandomValidModel();

                if (creatureModel != null)
                    return creatureModel.CreatureDisplayId;
            }

        return 0;
    }


    private void LoadAvailablePetBreeds()
    {
        var result = _worldDatabase.Query("SELECT speciesId, breedId FROM battle_pet_breeds");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 battle pet breeds. DB table `battle_pet_breeds` is empty.");

            return;
        }

        uint count = 0;

        do
        {
            var speciesId = result.Read<uint>(0);
            var breedId = result.Read<ushort>(1);

            if (!_cliDB.BattlePetSpeciesStorage.ContainsKey(speciesId))
            {
                Log.Logger.Error("Non-existing BattlePetSpecies.db2 entry {0} was referenced in `battle_pet_breeds` by row ({1}, {2}).", speciesId, speciesId, breedId);

                continue;
            }

            // TODO: verify breed id (3 - 12 (male) or 3 - 22 (male and female)) if needed

            _availableBreedsPerSpecies.Add(speciesId, (byte)breedId);
            ++count;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} battle pet breeds.", count);
    }

    private void LoadDefaultPetQualities()
    {
        var result = _worldDatabase.Query("SELECT speciesId, quality FROM battle_pet_quality");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 battle pet qualities. DB table `battle_pet_quality` is empty.");

            return;
        }

        do
        {
            var speciesId = result.Read<uint>(0);
            var quality = (BattlePetBreedQuality)result.Read<byte>(1);

            var battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(speciesId);

            if (battlePetSpecies == null)
            {
                Log.Logger.Error($"Non-existing BattlePetSpecies.db2 entry {speciesId} was referenced in `battle_pet_quality` by row ({speciesId}, {quality}).");

                continue;
            }

            if (quality >= BattlePetBreedQuality.Max)
            {
                Log.Logger.Error($"BattlePetSpecies.db2 entry {speciesId} was referenced in `battle_pet_quality` with non-existing quality {quality}).");

                continue;
            }

            if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.WellKnown) && quality > BattlePetBreedQuality.Rare)
            {
                Log.Logger.Error($"Learnable BattlePetSpecies.db2 entry {speciesId} was referenced in `battle_pet_quality` with invalid quality {quality}. Maximum allowed quality is BattlePetBreedQuality::Rare.");

                continue;
            }

            _defaultQualityPerSpecies[speciesId] = quality;
        } while (result.NextRow());

        Log.Logger.Information("Loaded {0} battle pet qualities.", _defaultQualityPerSpecies.Count);
    }
}