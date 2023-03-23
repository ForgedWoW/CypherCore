// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs.C;

public sealed class ContentTuningRecord
{
	public uint Id;
	public int Flags;
	public int ExpansionID;
	public int MinLevel;
	public int MaxLevel;
	public int MinLevelType;
	public int MaxLevelType;
	public int TargetLevelDelta;
	public int TargetLevelMaxDelta;
	public int TargetLevelMin;
	public int TargetLevelMax;
	public int MinItemLevel;

	public ContentTuningFlag GetFlags()
	{
		return (ContentTuningFlag)Flags;
	}

	public int GetScalingFactionGroup()
	{
		var flags = GetFlags();

		if (flags.HasFlag(ContentTuningFlag.Horde))
			return 5;

		if (flags.HasFlag(ContentTuningFlag.Alliance))
			return 3;

		return 0;
	}
}
