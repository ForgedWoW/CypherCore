// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;

namespace Forged.MapServer.Globals;

public class PhaseAreaInfo
{
    public PhaseAreaInfo(PhaseInfoStruct phaseInfo)
    {
        PhaseInfo = phaseInfo;
    }

    public List<Condition> Conditions { get; set; } = new();
    public PhaseInfoStruct PhaseInfo { get; set; }
    public List<uint> SubAreaExclusions { get; set; } = new();
}