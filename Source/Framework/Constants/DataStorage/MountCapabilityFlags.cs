// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum MountCapabilityFlags : byte
{
	Ground = 0x1,
	Flying = 0x2,
	Float = 0x4,
	Underwater = 0x8,
	IgnoreRestrictions = 0x20
}