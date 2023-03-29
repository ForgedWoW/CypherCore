// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ProcFlagsSpellPhase
{
    None = 0x0,
    Cast = 0x1,
    Hit = 0x2,
    Finish = 0x4,
    MaskAll = Cast | Hit | Finish
}