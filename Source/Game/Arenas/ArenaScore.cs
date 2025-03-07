﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Arenas;

class ArenaScore : BattlegroundScore
{
	readonly uint PreMatchRating;
	readonly uint PreMatchMMR;
	readonly uint PostMatchRating;
	readonly uint PostMatchMMR;

	public ArenaScore(ObjectGuid playerGuid, TeamFaction team) : base(playerGuid, team)
	{
		TeamId = (int)(team == TeamFaction.Alliance ? PvPTeamId.Alliance : PvPTeamId.Horde);
	}

	public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
	{
		base.BuildPvPLogPlayerDataPacket(out playerData);

		if (PreMatchRating != 0)
			playerData.PreMatchRating = PreMatchRating;

		if (PostMatchRating != PreMatchRating)
			playerData.RatingChange = (int)(PostMatchRating - PreMatchRating);

		if (PreMatchMMR != 0)
			playerData.PreMatchMMR = PreMatchMMR;

		if (PostMatchMMR != PreMatchMMR)
			playerData.MmrChange = (int)(PostMatchMMR - PreMatchMMR);
	}

	// For Logging purpose
	public override string ToString()
	{
		return $"Damage done: {DamageDone} Healing done: {HealingDone} Killing blows: {KillingBlows} PreMatchRating: {PreMatchRating} " +
				$"PreMatchMMR: {PreMatchMMR} PostMatchRating: {PostMatchRating} PostMatchMMR: {PostMatchMMR}";
	}
}

public class ArenaTeamScore
{
	public uint PreMatchRating;
	public uint PostMatchRating;
	public uint PreMatchMMR;
	public uint PostMatchMMR;

	public void Assign(uint preMatchRating, uint postMatchRating, uint preMatchMMR, uint postMatchMMR)
	{
		PreMatchRating = preMatchRating;
		PostMatchRating = postMatchRating;
		PreMatchMMR = preMatchMMR;
		PostMatchMMR = postMatchMMR;
	}
}