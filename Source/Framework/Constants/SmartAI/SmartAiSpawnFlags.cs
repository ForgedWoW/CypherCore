// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SmartAiSpawnFlags
{
    None = 0x00,
    IgnoreRespawn = 0x01,
    ForceSpawn = 0x02,
    NosaveRespawn = 0x04,
}