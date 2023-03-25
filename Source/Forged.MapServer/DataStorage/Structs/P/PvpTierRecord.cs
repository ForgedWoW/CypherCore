// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed class PvpTierRecord
{
	public LocalizedString Name;
	public uint Id;
	public short MinRating;
	public short MaxRating;
	public int PrevTier;
	public int NextTier;
	public sbyte BracketID;
	public sbyte Rank;
	public int RankIconFileDataID;
}