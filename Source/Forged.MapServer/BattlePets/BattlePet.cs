// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chat;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.BattlePet;
using Framework.Constants;

namespace Forged.MapServer.BattlePets;

public class BattlePet
{
    private readonly BattlePetMgrData _battlePetMgr;
    private readonly DB6Storage<BattlePetBreedQualityRecord> _battlePetBreedQualityRecords;
    public DeclinedName DeclinedName;
    public long NameTimestamp;
    public BattlePetStruct PacketInfo;
    public BattlePetSaveInfo SaveInfo;

    public BattlePet(BattlePetMgrData battlePetMgr, DB6Storage<BattlePetBreedQualityRecord> battlePetBreedQualityRecords)
    {
        _battlePetMgr = battlePetMgr;
        _battlePetBreedQualityRecords = battlePetBreedQualityRecords;
    }

    public void CalculateStats()
    {
        // get base breed stats
        if (!_battlePetMgr.BattlePetBreedStates.TryGetValue(PacketInfo.Breed, out var breedState)) // non existing breed id
            return;

        float health = breedState[BattlePetState.StatStamina];
        float power = breedState[BattlePetState.StatPower];
        float speed = breedState[BattlePetState.StatSpeed];

        // modify stats depending on species - not all pets have this
        if (_battlePetMgr.BattlePetSpeciesStates.TryGetValue(PacketInfo.Species, out var speciesState))
        {
            health += speciesState[BattlePetState.StatStamina];
            power += speciesState[BattlePetState.StatPower];
            speed += speciesState[BattlePetState.StatSpeed];
        }

        // modify stats by quality
        foreach (var battlePetBreedQuality in _battlePetBreedQualityRecords.Values.Where(battlePetBreedQuality => battlePetBreedQuality.QualityEnum == PacketInfo.Quality))
        {
            health *= battlePetBreedQuality.StateMultiplier;
            power *= battlePetBreedQuality.StateMultiplier;
            speed *= battlePetBreedQuality.StateMultiplier;

            break;
        }

        // TOOD: add check if pet has existing quality
        // scale stats depending on level
        health *= PacketInfo.Level;
        power *= PacketInfo.Level;
        speed *= PacketInfo.Level;

        // set stats
        // round, ceil or floor? verify this
        PacketInfo.MaxHealth = (uint)((Math.Round(health / 20) + 100));
        PacketInfo.Power = (uint)(Math.Round(power / 100));
        PacketInfo.Speed = (uint)(Math.Round(speed / 100));
    }
}