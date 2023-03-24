// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Entities.Players;

public enum SocialFlag
{
	Friend = 0x01,
	Ignored = 0x02,
	Muted = 0x04, // guessed
	Unk = 0x08,   // Unknown - does not appear to be RaF
	All = Friend | Ignored | Muted
}
