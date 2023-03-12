// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum HeightHeaderFlags : uint
{
	None = 0x0000,
	NoHeight = 0x0001,
	HeightAsInt16 = 0x0002,
	HeightAsInt8 = 0x0004,
	HasFlightBounds = 0x0008
}