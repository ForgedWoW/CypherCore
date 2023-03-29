// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Inspect;

public struct PVPBracketData
{
    public void Write(WorldPacket data)
    {
        data.WriteUInt8(Bracket);
        data.WriteInt32(Unused3);
        data.WriteInt32(Rating);
        data.WriteInt32(Rank);
        data.WriteInt32(WeeklyPlayed);
        data.WriteInt32(WeeklyWon);
        data.WriteInt32(SeasonPlayed);
        data.WriteInt32(SeasonWon);
        data.WriteInt32(WeeklyBestRating);
        data.WriteInt32(SeasonBestRating);
        data.WriteInt32(PvpTierID);
        data.WriteInt32(WeeklyBestWinPvpTierID);
        data.WriteInt32(Unused1);
        data.WriteInt32(Unused2);
        data.WriteInt32(RoundsSeasonPlayed);
        data.WriteInt32(RoundsSeasonWon);
        data.WriteInt32(RoundsWeeklyPlayed);
        data.WriteInt32(RoundsWeeklyWon);
        data.WriteBit(Disqualified);
        data.FlushBits();
    }

    public int Rating;
    public int Rank;
    public int WeeklyPlayed;
    public int WeeklyWon;
    public int SeasonPlayed;
    public int SeasonWon;
    public int WeeklyBestRating;
    public int SeasonBestRating;
    public int PvpTierID;
    public int WeeklyBestWinPvpTierID;
    public int Unused1;
    public int Unused2;
    public int Unused3;
    public int RoundsSeasonPlayed;
    public int RoundsSeasonWon;
    public int RoundsWeeklyPlayed;
    public int RoundsWeeklyWon;
    public byte Bracket;
    public bool Disqualified;
}