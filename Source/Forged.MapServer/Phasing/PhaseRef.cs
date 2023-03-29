// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Conditions;

namespace Forged.MapServer.Phasing;

public class PhaseRef
{
    public PhaseFlags Flags;
    public int References;
    public List<Condition> AreaConditions;

    public PhaseRef(PhaseFlags flags, List<Condition> conditions)
    {
        Flags = flags;
        References = 0;
        AreaConditions = conditions;
    }

    public bool IsPersonal()
    {
        return Flags.HasFlag(PhaseFlags.Personal);
    }
}