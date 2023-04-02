// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;

namespace Forged.MapServer.Globals;

public class PhaseAreaInfo
{
    public List<Condition> Conditions = new();
    public PhaseInfoStruct PhaseInfo;
    public List<uint> SubAreaExclusions = new();
    public PhaseAreaInfo(PhaseInfoStruct phaseInfo)
    {
        PhaseInfo = phaseInfo;
    }
}