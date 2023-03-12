// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum ActivateTaxiReply
{
	Ok = 0,
	UnspecifiedServerError = 1,
	NoSuchPath = 2,
	NotEnoughMoney = 3,
	TooFarAway = 4,
	NoVendorNearby = 5,
	NotVisited = 6,
	PlayerBusy = 7,
	PlayerAlreadyMounted = 8,
	PlayerShapeshifted = 9,
	PlayerMoving = 10,
	SameNode = 11,
	NotStanding = 12,
}