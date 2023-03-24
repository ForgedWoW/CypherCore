// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.BattleGround;

struct BracketInfo
{
	public int PersonalRating;
	public int Ranking;
	public int SeasonPlayed;
	public int SeasonWon;
	public int Unused1;
	public int Unused2;
	public int WeeklyPlayed;
	public int WeeklyWon;
	public int RoundsSeasonPlayed;
	public int RoundsSeasonWon;
	public int RoundsWeeklyPlayed;
	public int RoundsWeeklyWon;
	public int BestWeeklyRating;
	public int LastWeeksBestRating;
	public int BestSeasonRating;
	public int PvpTierID;
	public int Unused3;
	public int Unused4;
	public int Rank;
	public bool Disqualified;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(PersonalRating);
		data.WriteInt32(Ranking);
		data.WriteInt32(SeasonPlayed);
		data.WriteInt32(SeasonWon);
		data.WriteInt32(Unused1);
		data.WriteInt32(Unused2);
		data.WriteInt32(WeeklyPlayed);
		data.WriteInt32(WeeklyWon);
		data.WriteInt32(RoundsSeasonPlayed);
		data.WriteInt32(RoundsSeasonWon);
		data.WriteInt32(RoundsWeeklyPlayed);
		data.WriteInt32(RoundsWeeklyWon);
		data.WriteInt32(BestWeeklyRating);
		data.WriteInt32(LastWeeksBestRating);
		data.WriteInt32(BestSeasonRating);
		data.WriteInt32(PvpTierID);
		data.WriteInt32(Unused3);
		data.WriteInt32(Unused4);
		data.WriteInt32(Rank);
		data.WriteBit(Disqualified);
		data.FlushBits();
	}
}
