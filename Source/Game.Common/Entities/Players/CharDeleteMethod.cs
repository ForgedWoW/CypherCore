using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Entities.Players;

public enum CharDeleteMethod
{
	Remove = 0, // Completely remove from the database

	Unlink = 1 // The character gets unlinked from the account,
	// the name gets freed up and appears as deleted ingame
}
