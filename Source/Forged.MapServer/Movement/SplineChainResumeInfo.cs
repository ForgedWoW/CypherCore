// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Movement;

public class SplineChainResumeInfo
{
    public SplineChainResumeInfo() { }

    public SplineChainResumeInfo(uint id, List<SplineChainLink> chain, bool walk, byte splineIndex, byte wpIndex, uint msToNext)
    {
        PointID = id;
        Chain = chain;
        IsWalkMode = walk;
        SplineIndex = splineIndex;
        PointIndex = wpIndex;
        TimeToNext = msToNext;
    }

    public List<SplineChainLink> Chain { get; set; } = new();
    public bool IsWalkMode { get; set; }
    public uint PointID { get; set; }
    public byte PointIndex { get; set; }
    public byte SplineIndex { get; set; }
    public uint TimeToNext { get; set; }

    public void Clear()
    {
        Chain.Clear();
    }

    public bool Empty()
    {
        return Chain.Empty();
    }
}