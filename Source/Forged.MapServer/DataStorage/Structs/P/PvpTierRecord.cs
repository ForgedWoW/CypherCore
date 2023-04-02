// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.P;

public sealed class PvpTierRecord
{
    public sbyte BracketID;
    public uint Id;
    public short MaxRating;
    public short MinRating;
    public LocalizedString Name;
    public int NextTier;
    public int PrevTier;
    public sbyte Rank;
    public int RankIconFileDataID;
}