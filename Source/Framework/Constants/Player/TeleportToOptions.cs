// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum TeleportToOptions
{
	GMMode = 0x01,
	NotLeaveTransport = 0x02,
	NotLeaveCombat = 0x04,
	NotUnSummonPet = 0x08,
	Spell = 0x10,
	ReviveAtTeleport = 0x40,
	Seamless = 0x80
}