// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Entities;

public class PetStable
{
    public PetInfo[] ActivePets = new PetInfo[SharedConst.MaxActivePets];

    public uint? CurrentPetIndex; // index into ActivePets or UnslottedPets if highest bit is set

    // PET_SAVE_FIRST_ACTIVE_SLOT - PET_SAVE_LAST_ACTIVE_SLOT
    public PetInfo[] StabledPets = new PetInfo[SharedConst.MaxPetStables]; // PET_SAVE_FIRST_STABLE_SLOT - PET_SAVE_LAST_STABLE_SLOT
    public List<PetInfo> UnslottedPets = new();                            // PET_SAVE_NOT_IN_SLOT
    private const uint UnslottedPetIndexMask = 0x80000000;

    public uint? CurrentActivePetIndex => CurrentPetIndex.HasValue && (CurrentPetIndex & UnslottedPetIndexMask) == 0 ? CurrentPetIndex : null;

    public PetInfo GetCurrentPet()
    {
        if (!CurrentPetIndex.HasValue)
            return null;

        var activePetIndex = CurrentActivePetIndex;

        if (activePetIndex.HasValue)
            return ActivePets[activePetIndex.Value] != null ? ActivePets[activePetIndex.Value] : null;

        var unslottedPetIndex = CurrentUnslottedPetIndex;

        if (unslottedPetIndex.HasValue)
            return unslottedPetIndex < UnslottedPets.Count ? UnslottedPets[(int)unslottedPetIndex.Value] : null;

        return null;
    }

    public void SetCurrentActivePetIndex(uint index)
    {
        CurrentPetIndex = index;
    }

    public void SetCurrentUnslottedPetIndex(uint index)
    {
        CurrentPetIndex = index | UnslottedPetIndexMask;
    }

    private uint? CurrentUnslottedPetIndex => CurrentPetIndex.HasValue && (CurrentPetIndex & UnslottedPetIndexMask) != 0 ? CurrentPetIndex & ~UnslottedPetIndexMask : null;

    public class PetInfo
    {
        public string ActionBar { get; set; }
        public uint CreatedBySpellId { get; set; }
        public uint CreatureId { get; set; }
        public uint DisplayId { get; set; }
        public uint Experience { get; set; }
        public uint Health { get; set; }
        public uint LastSaveTime { get; set; }
        public byte Level { get; set; }
        public uint Mana { get; set; }
        public string Name { get; set; }
        public uint PetNumber { get; set; }
        public ReactStates ReactState { get; set; }
        public ushort SpecializationId { get; set; }
        public PetType Type { get; set; } = PetType.Max;
        public bool WasRenamed { get; set; }
    }
}