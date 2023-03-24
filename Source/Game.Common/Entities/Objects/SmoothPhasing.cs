// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Entities.Objects;

public class SmoothPhasing
{
	readonly Dictionary<ObjectGuid, SmoothPhasingInfo> _smoothPhasingInfoViewerDependent = new();
	SmoothPhasingInfo _smoothPhasingInfoSingle;

	public void SetViewerDependentInfo(ObjectGuid seer, SmoothPhasingInfo info)
	{
		_smoothPhasingInfoViewerDependent[seer] = info;
	}

	public void ClearViewerDependentInfo(ObjectGuid seer)
	{
		_smoothPhasingInfoViewerDependent.Remove(seer);
	}

	public void SetSingleInfo(SmoothPhasingInfo info)
	{
		_smoothPhasingInfoSingle = info;
	}

	public bool IsReplacing(ObjectGuid guid)
	{
		return _smoothPhasingInfoSingle != null && _smoothPhasingInfoSingle.ReplaceObject == guid;
	}

	public bool IsBeingReplacedForSeer(ObjectGuid seer)
	{
		var smoothPhasingInfo = _smoothPhasingInfoViewerDependent.LookupByKey(seer);

		if (smoothPhasingInfo != null)
			return !smoothPhasingInfo.Disabled;

		return false;
	}

	public SmoothPhasingInfo GetInfoForSeer(ObjectGuid seer)
	{
		if (_smoothPhasingInfoViewerDependent.TryGetValue(seer, out var value))
			return value;

		return _smoothPhasingInfoSingle;
	}

	public void DisableReplacementForSeer(ObjectGuid seer)
	{
		var smoothPhasingInfo = _smoothPhasingInfoViewerDependent.LookupByKey(seer);

		if (smoothPhasingInfo != null)
			smoothPhasingInfo.Disabled = true;
	}
}
