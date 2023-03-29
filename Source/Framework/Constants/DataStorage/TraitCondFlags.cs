// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum TraitCondFlags
{
    None = 0x0,
    IsGate = 0x1,
    IsAlwaysMet = 0x2,
    IsSufficient = 0x4,
}