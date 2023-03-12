// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum BattlePetError
{
	CantHaveMorePetsOfThatType = 3, // You can't have any more pets of that type.
	CantHaveMorePets = 4,           // You can't have any more pets.
	TooHighLevelToUncage = 7,       // This pet is too high level for you to uncage.
}

// taken from BattlePetState.db2 - it seems to store some initial values for battle pets
// there are only values used in BattlePetSpeciesState.db2 and BattlePetBreedState.db2
// TODO: expand this enum if needed