using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities;

public class PetStable
{
	public class PetInfo
	{
		public string Name;
		public string ActionBar;
		public uint PetNumber;
		public uint CreatureId;
		public uint DisplayId;
		public uint Experience;
		public uint Health;
		public uint Mana;
		public uint LastSaveTime;
		public uint CreatedBySpellId;
		public ushort SpecializationId;
		public byte Level = 0;
		public ReactStates ReactState;
		public PetType Type = PetType.Max;
		public bool WasRenamed;
	}

	public uint? CurrentPetIndex;                                          // index into ActivePets or UnslottedPets if highest bit is set
	public PetInfo[] ActivePets = new PetInfo[SharedConst.MaxActivePets];  // PET_SAVE_FIRST_ACTIVE_SLOT - PET_SAVE_LAST_ACTIVE_SLOT
	public PetInfo[] StabledPets = new PetInfo[SharedConst.MaxPetStables]; // PET_SAVE_FIRST_STABLE_SLOT - PET_SAVE_LAST_STABLE_SLOT
	public List<PetInfo> UnslottedPets = new();                            // PET_SAVE_NOT_IN_SLOT
	static readonly uint UnslottedPetIndexMask = 0x80000000;

	public PetInfo GetCurrentPet()
	{
		if (!CurrentPetIndex.HasValue)
			return null;

		var activePetIndex = GetCurrentActivePetIndex();

		if (activePetIndex.HasValue)
			return ActivePets[activePetIndex.Value] != null ? ActivePets[activePetIndex.Value] : null;

		var unslottedPetIndex = GetCurrentUnslottedPetIndex();

		if (unslottedPetIndex.HasValue)
			return unslottedPetIndex < UnslottedPets.Count ? UnslottedPets[(int)unslottedPetIndex.Value] : null;

		return null;
	}

	public uint? GetCurrentActivePetIndex()
	{
		return CurrentPetIndex.HasValue && ((CurrentPetIndex & UnslottedPetIndexMask) == 0) ? CurrentPetIndex : null;
	}

	public void SetCurrentActivePetIndex(uint index)
	{
		CurrentPetIndex = index;
	}

	public void SetCurrentUnslottedPetIndex(uint index)
	{
		CurrentPetIndex = index | UnslottedPetIndexMask;
	}

	uint? GetCurrentUnslottedPetIndex()
	{
		return CurrentPetIndex.HasValue && ((CurrentPetIndex & UnslottedPetIndexMask) != 0) ? (CurrentPetIndex & ~UnslottedPetIndexMask) : null;
	}
}