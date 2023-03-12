// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PetitionSigns
{
	Ok = 0,
	AlreadySigned = 1,
	AlreadyInGuild = 2,
	CantSignOwn = 3,
	NotServer = 5,
	Full = 8,
	AlreadySignedOther = 10,
	RestrictedAccountTrial = 11,
	HasRestriction = 13
}