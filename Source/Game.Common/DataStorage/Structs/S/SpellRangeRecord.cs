// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.DataStorage;

public sealed class SpellRangeRecord
{
	public uint Id;
	public string DisplayName;
	public string DisplayNameShort;
	public SpellRangeFlag Flags;
	public float[] RangeMin = new float[2];
	public float[] RangeMax = new float[2];
}