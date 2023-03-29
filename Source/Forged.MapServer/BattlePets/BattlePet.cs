// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.BattlePet;
using Framework.Constants;

namespace Forged.MapServer.BattlePets;

public class BattlePet
{
    public BattlePetStruct PacketInfo;
    public long NameTimestamp;
    public DeclinedName DeclinedName;
    public BattlePetSaveInfo SaveInfo;

    public void CalculateStats()
    {
        // get base breed stats
        var breedState = BattlePetMgr.BattlePetBreedStates.LookupByKey(PacketInfo.Breed);

        if (breedState == null) // non existing breed id
            return;

        float health = breedState[BattlePetState.StatStamina];
        float power = breedState[BattlePetState.StatPower];
        float speed = breedState[BattlePetState.StatSpeed];

        // modify stats depending on species - not all pets have this
        var speciesState = BattlePetMgr.BattlePetSpeciesStates.LookupByKey(PacketInfo.Species);

        if (speciesState != null)
        {
            health += speciesState[BattlePetState.StatStamina];
            power += speciesState[BattlePetState.StatPower];
            speed += speciesState[BattlePetState.StatSpeed];
        }

        // modify stats by quality
        foreach (var battlePetBreedQuality in CliDB.BattlePetBreedQualityStorage.Values)
            if (battlePetBreedQuality.QualityEnum == PacketInfo.Quality)
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