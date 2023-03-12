// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CreatureLinkedRespawnType
{
	CreatureToCreature = 0,
	CreatureToGO = 1, // Creature is dependant on GO
	GOToGO = 2,
	GOToCreature = 3 // GO is dependant on creature
}