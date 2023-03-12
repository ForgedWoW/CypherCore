// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum FriendshipReputationFlags : int
{
	NoFXOnReactionChange = 0x01,
	NoLogTextOnRepGain = 0x02,
	NoLogTextOnReactionChange = 0x04,
	ShowRepGainandReactionChangeForHiddenFaction = 0x08,
	NoRepGainModifiers = 0x10
}