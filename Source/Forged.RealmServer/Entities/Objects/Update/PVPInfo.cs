// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Forged.RealmServer.Entities;

public class PVPInfo : BaseUpdateData<Player>
{
	public UpdateField<bool> Disqualified = new(0, 1);
	public UpdateField<sbyte> Bracket = new(0, 2);
	public UpdateField<uint> PvpRatingID = new(0, 3);
	public UpdateField<uint> WeeklyPlayed = new(0, 4);
	public UpdateField<uint> WeeklyWon = new(0, 5);
	public UpdateField<uint> SeasonPlayed = new(0, 6);
	public UpdateField<uint> SeasonWon = new(0, 7);
	public UpdateField<uint> Rating = new(0, 8);
	public UpdateField<uint> WeeklyBestRating = new(0, 9);
	public UpdateField<uint> SeasonBestRating = new(0, 10);
	public UpdateField<uint> PvpTierID = new(0, 11);
	public UpdateField<uint> WeeklyBestWinPvpTierID = new(0, 12);
	public UpdateField<uint> Field_28 = new(0, 13);
	public UpdateField<uint> Field_2C = new(0, 14);
	public UpdateField<uint> WeeklyRoundsPlayed = new(0, 15);
	public UpdateField<uint> WeeklyRoundsWon = new(0, 16);
	public UpdateField<uint> SeasonRoundsPlayed = new(0, 17);
	public UpdateField<uint> SeasonRoundsWon = new(0, 18);

	public PVPInfo() : base(19) { }

	public void WriteCreate(WorldPacket data, Player owner, Player receiver)
	{
		data.WriteInt8(Bracket);
		data.WriteUInt32(PvpRatingID);
		data.WriteUInt32(WeeklyPlayed);
		data.WriteUInt32(WeeklyWon);
		data.WriteUInt32(SeasonPlayed);
		data.WriteUInt32(SeasonWon);
		data.WriteUInt32(Rating);
		data.WriteUInt32(WeeklyBestRating);
		data.WriteUInt32(SeasonBestRating);
		data.WriteUInt32(PvpTierID);
		data.WriteUInt32(WeeklyBestWinPvpTierID);
		data.WriteUInt32(Field_28);
		data.WriteUInt32(Field_2C);
		data.WriteUInt32(WeeklyRoundsPlayed);
		data.WriteUInt32(WeeklyRoundsWon);
		data.WriteUInt32(SeasonRoundsPlayed);
		data.WriteUInt32(SeasonRoundsWon);
		data.WriteBit(Disqualified);
		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
	{
		var changesMask = ChangesMask;

		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlock(0), 19);

		if (changesMask[0])
			if (changesMask[1])
				data.WriteBit(Disqualified);

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[2])
				data.WriteInt8(Bracket);

			if (changesMask[3])
				data.WriteUInt32(PvpRatingID);

			if (changesMask[4])
				data.WriteUInt32(WeeklyPlayed);

			if (changesMask[5])
				data.WriteUInt32(WeeklyWon);

			if (changesMask[6])
				data.WriteUInt32(SeasonPlayed);

			if (changesMask[7])
				data.WriteUInt32(SeasonWon);

			if (changesMask[8])
				data.WriteUInt32(Rating);

			if (changesMask[9])
				data.WriteUInt32(WeeklyBestRating);

			if (changesMask[10])
				data.WriteUInt32(SeasonBestRating);

			if (changesMask[11])
				data.WriteUInt32(PvpTierID);

			if (changesMask[12])
				data.WriteUInt32(WeeklyBestWinPvpTierID);

			if (changesMask[13])
				data.WriteUInt32(Field_28);

			if (changesMask[14])
				data.WriteUInt32(Field_2C);

			if (changesMask[15])
				data.WriteUInt32(WeeklyRoundsPlayed);

			if (changesMask[16])
				data.WriteUInt32(WeeklyRoundsWon);

			if (changesMask[17])
				data.WriteUInt32(SeasonRoundsPlayed);

			if (changesMask[18])
				data.WriteUInt32(SeasonRoundsWon);
		}

		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Disqualified);
		ClearChangesMask(Bracket);
		ClearChangesMask(PvpRatingID);
		ClearChangesMask(WeeklyPlayed);
		ClearChangesMask(WeeklyWon);
		ClearChangesMask(SeasonPlayed);
		ClearChangesMask(SeasonWon);
		ClearChangesMask(Rating);
		ClearChangesMask(WeeklyBestRating);
		ClearChangesMask(SeasonBestRating);
		ClearChangesMask(PvpTierID);
		ClearChangesMask(WeeklyBestWinPvpTierID);
		ClearChangesMask(Field_28);
		ClearChangesMask(Field_2C);
		ClearChangesMask(WeeklyRoundsPlayed);
		ClearChangesMask(WeeklyRoundsWon);
		ClearChangesMask(SeasonRoundsPlayed);
		ClearChangesMask(SeasonRoundsWon);
		ChangesMask.ResetAll();
	}
}