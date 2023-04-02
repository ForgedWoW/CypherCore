// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Entities.Objects;

public class SmoothPhasing
{
    private readonly Dictionary<ObjectGuid, SmoothPhasingInfo> _smoothPhasingInfoViewerDependent = new();
    private SmoothPhasingInfo _smoothPhasingInfoSingle;

    public void ClearViewerDependentInfo(ObjectGuid seer)
    {
        _smoothPhasingInfoViewerDependent.Remove(seer);
    }

    public void DisableReplacementForSeer(ObjectGuid seer)
    {
        var smoothPhasingInfo = _smoothPhasingInfoViewerDependent.LookupByKey(seer);

        if (smoothPhasingInfo != null)
            smoothPhasingInfo.Disabled = true;
    }

    public SmoothPhasingInfo GetInfoForSeer(ObjectGuid seer)
    {
        if (_smoothPhasingInfoViewerDependent.TryGetValue(seer, out var value))
            return value;

        return _smoothPhasingInfoSingle;
    }

    public bool IsBeingReplacedForSeer(ObjectGuid seer)
    {
        var smoothPhasingInfo = _smoothPhasingInfoViewerDependent.LookupByKey(seer);

        if (smoothPhasingInfo != null)
            return !smoothPhasingInfo.Disabled;

        return false;
    }

    public bool IsReplacing(ObjectGuid guid)
    {
        return _smoothPhasingInfoSingle != null && _smoothPhasingInfoSingle.ReplaceObject == guid;
    }

    public void SetSingleInfo(SmoothPhasingInfo info)
    {
        _smoothPhasingInfoSingle = info;
    }

    public void SetViewerDependentInfo(ObjectGuid seer, SmoothPhasingInfo info)
    {
        _smoothPhasingInfoViewerDependent[seer] = info;
    }
}