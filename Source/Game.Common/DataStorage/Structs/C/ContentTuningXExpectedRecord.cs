using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.C;

public sealed class ContentTuningXExpectedRecord
{
	public uint Id;
	public int ExpectedStatModID;
	public int MinMythicPlusSeasonID;
	public int MaxMythicPlusSeasonID;
	public uint ContentTuningID;
}
