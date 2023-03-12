// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum InstanceSpawnGroupFlags
{
	ActivateSpawn = 0x01,
	BlockSpawn = 0x02,
	AllianceOnly = 0x04,
	HordeOnly = 0x08,

	All = ActivateSpawn | BlockSpawn | AllianceOnly | HordeOnly
}