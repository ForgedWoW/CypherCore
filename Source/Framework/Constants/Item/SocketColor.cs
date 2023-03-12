// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SocketColor
{
	Meta = 0x00001,
	Red = 0x00002,
	Yellow = 0x00004,
	Blue = 0x00008,
	Hydraulic = 0x00010, // Not Used
	Cogwheel = 0x00020,
	Prismatic = 0x0000e,
	RelicIron = 0x00040,
	RelicBlood = 0x00080,
	RelicShadow = 0x00100,
	RelicFel = 0x00200,
	RelicArcane = 0x00400,
	RelicFrost = 0x00800,
	RelicFire = 0x01000,
	RelicWater = 0x02000,
	RelicLife = 0x04000,
	RelicWind = 0x08000,
	RelicHoly = 0x10000,

	Standard = (Red | Yellow | Blue)
}