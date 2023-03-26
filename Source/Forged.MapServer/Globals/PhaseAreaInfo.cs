using System.Collections.Generic;
using Forged.MapServer.Conditions;

namespace Forged.MapServer.Globals;

public class PhaseAreaInfo
{
    public PhaseInfoStruct PhaseInfo;
    public List<uint> SubAreaExclusions = new();
    public List<Condition> Conditions = new();

    public PhaseAreaInfo(PhaseInfoStruct phaseInfo)
    {
        PhaseInfo = phaseInfo;
    }
}