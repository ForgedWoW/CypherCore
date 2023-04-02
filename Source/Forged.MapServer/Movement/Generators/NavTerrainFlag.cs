// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Movement.Generators;

[Flags]
public enum NavTerrainFlag
{
    Empty = 0x00,
    Ground = 1 << (NavArea.MaxValue - NavArea.Ground),
    GroundSteep = 1 << (NavArea.MaxValue - NavArea.GroundSteep),
    Water = 1 << (NavArea.MaxValue - NavArea.Water),
    MagmaSlime = 1 << (NavArea.MaxValue - NavArea.MagmaSlime)
}