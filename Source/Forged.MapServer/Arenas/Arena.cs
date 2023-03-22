﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.BattleGrounds;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.Arenas;

public class Arena : Battleground
{
	public ArenaTeamScore[] _arenaTeamScores = new ArenaTeamScore[SharedConst.PvpTeamsCount];
	protected TaskScheduler taskScheduler = new();

	public Arena(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
	{
		StartDelayTimes[BattlegroundConst.EventIdFirst] = BattlegroundStartTimeIntervals.Delay1m;
		StartDelayTimes[BattlegroundConst.EventIdSecond] = BattlegroundStartTimeIntervals.Delay30s;
		StartDelayTimes[BattlegroundConst.EventIdThird] = BattlegroundStartTimeIntervals.Delay15s;
		StartDelayTimes[BattlegroundConst.EventIdFourth] = BattlegroundStartTimeIntervals.None;

		StartMessageIds[BattlegroundConst.EventIdFirst] = ArenaBroadcastTexts.OneMinute;
		StartMessageIds[BattlegroundConst.EventIdSecond] = ArenaBroadcastTexts.ThirtySeconds;
		StartMessageIds[BattlegroundConst.EventIdThird] = ArenaBroadcastTexts.FifteenSeconds;
		StartMessageIds[BattlegroundConst.EventIdFourth] = ArenaBroadcastTexts.HasBegun;
	}

	public override void AddPlayer(Player player)
	{
		var isInBattleground = IsPlayerInBattleground(player.GUID);
		base.AddPlayer(player);

		if (!isInBattleground)
			PlayerScores[player.GUID] = new ArenaScore(player.GUID, player.GetBgTeam());

		if (player.GetBgTeam() == TeamFaction.Alliance) // gold
		{
			if (player.EffectiveTeam == TeamFaction.Horde)
				player.CastSpell(player, ArenaSpellIds.HordeGoldFlag, true);
			else
				player.CastSpell(player, ArenaSpellIds.AllianceGoldFlag, true);
		}
		else // green
		{
			if (player.EffectiveTeam == TeamFaction.Horde)
				player.CastSpell(player, ArenaSpellIds.HordeGreenFlag, true);
			else
				player.CastSpell(player, ArenaSpellIds.AllianceGreenFlag, true);
		}

		UpdateArenaWorldState();
	}

	public override void RemovePlayer(Player player, ObjectGuid guid, TeamFaction team)
	{
		if (GetStatus() == BattlegroundStatus.WaitLeave)
			return;

		UpdateArenaWorldState();
		CheckWinConditions();
	}

	public override void HandleKillPlayer(Player victim, Player killer)
	{
		if (GetStatus() != BattlegroundStatus.InProgress)
			return;

		base.HandleKillPlayer(victim, killer);

		UpdateArenaWorldState();
		CheckWinConditions();
	}

	public override void BuildPvPLogDataPacket(out PVPMatchStatistics pvpLogData)
	{
		base.BuildPvPLogDataPacket(out pvpLogData);

		if (IsRated())
		{
			pvpLogData.Ratings = new PVPMatchStatistics.RatingData();

			for (byte i = 0; i < SharedConst.PvpTeamsCount; ++i)
			{
				pvpLogData.Ratings.Postmatch[i] = _arenaTeamScores[i].PostMatchRating;
				pvpLogData.Ratings.Prematch[i] = _arenaTeamScores[i].PreMatchRating;
				pvpLogData.Ratings.PrematchMMR[i] = _arenaTeamScores[i].PreMatchMMR;
			}
		}
	}

	public override void RemovePlayerAtLeave(ObjectGuid guid, bool Transport, bool SendPacket)
	{
		if (IsRated() && GetStatus() == BattlegroundStatus.InProgress)
		{
			var bgPlayer = GetPlayers().LookupByKey(guid);

			if (bgPlayer != null) // check if the player was a participant of the match, or only entered through gm command (appear)
			{
				// if the player was a match participant, calculate rating

				var winnerArenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(GetArenaTeamIdForTeam(GetOtherTeam(bgPlayer.Team)));
				var loserArenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(GetArenaTeamIdForTeam(bgPlayer.Team));

				// left a rated match while the encounter was in progress, consider as loser
				if (winnerArenaTeam != null && loserArenaTeam != null && winnerArenaTeam != loserArenaTeam)
				{
					var player = _GetPlayer(guid, bgPlayer.OfflineRemoveTime != 0, "Arena.RemovePlayerAtLeave");

					if (player)
						loserArenaTeam.MemberLost(player, GetArenaMatchmakerRating(GetOtherTeam(bgPlayer.Team)));
					else
						loserArenaTeam.OfflineMemberLost(guid, GetArenaMatchmakerRating(GetOtherTeam(bgPlayer.Team)));
				}
			}
		}

		// remove player
		base.RemovePlayerAtLeave(guid, Transport, SendPacket);
	}

	public override void CheckWinConditions()
	{
		if (GetAlivePlayersCountByTeam(TeamFaction.Alliance) == 0 && GetPlayersCountByTeam(TeamFaction.Horde) != 0)
			EndBattleground(TeamFaction.Horde);
		else if (GetPlayersCountByTeam(TeamFaction.Alliance) != 0 && GetAlivePlayersCountByTeam(TeamFaction.Horde) == 0)
			EndBattleground(TeamFaction.Alliance);
	}

	public override void EndBattleground(TeamFaction winner)
	{
		// arena rating calculation
		if (IsRated())
		{
			uint loserTeamRating;
			uint loserMatchmakerRating;
			var loserChange = 0;
			var loserMatchmakerChange = 0;
			uint winnerTeamRating;
			uint winnerMatchmakerRating;
			var winnerChange = 0;
			var winnerMatchmakerChange = 0;
			var guildAwarded = false;

			// In case of arena draw, follow this logic:
			// winnerArenaTeam => ALLIANCE, loserArenaTeam => HORDE
			var winnerArenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(GetArenaTeamIdForTeam(winner == 0 ? TeamFaction.Alliance : winner));
			var loserArenaTeam = Global.ArenaTeamMgr.GetArenaTeamById(GetArenaTeamIdForTeam(winner == 0 ? TeamFaction.Horde : GetOtherTeam(winner)));

			if (winnerArenaTeam != null && loserArenaTeam != null && winnerArenaTeam != loserArenaTeam)
			{
				// In case of arena draw, follow this logic:
				// winnerMatchmakerRating => ALLIANCE, loserMatchmakerRating => HORDE
				loserTeamRating = loserArenaTeam.GetRating();
				loserMatchmakerRating = GetArenaMatchmakerRating(winner == 0 ? TeamFaction.Horde : GetOtherTeam(winner));
				winnerTeamRating = winnerArenaTeam.GetRating();
				winnerMatchmakerRating = GetArenaMatchmakerRating(winner == 0 ? TeamFaction.Alliance : winner);

				if (winner != 0)
				{
					winnerMatchmakerChange = winnerArenaTeam.WonAgainst(winnerMatchmakerRating, loserMatchmakerRating, ref winnerChange);
					loserMatchmakerChange = loserArenaTeam.LostAgainst(loserMatchmakerRating, winnerMatchmakerRating, ref loserChange);

					Log.outDebug(LogFilter.Arena,
								"match Type: {0} --- Winner: old rating: {1}, rating gain: {2}, old MMR: {3}, MMR gain: {4} --- Loser: old rating: {5}, " +
								"rating loss: {6}, old MMR: {7}, MMR loss: {8} ---",
								GetArenaType(),
								winnerTeamRating,
								winnerChange,
								winnerMatchmakerRating,
								winnerMatchmakerChange,
								loserTeamRating,
								loserChange,
								loserMatchmakerRating,
								loserMatchmakerChange);

					SetArenaMatchmakerRating(winner, (uint)(winnerMatchmakerRating + winnerMatchmakerChange));
					SetArenaMatchmakerRating(GetOtherTeam(winner), (uint)(loserMatchmakerRating + loserMatchmakerChange));

					// bg team that the client expects is different to TeamId
					// alliance 1, horde 0
					var winnerTeam = (byte)(winner == TeamFaction.Alliance ? PvPTeamId.Alliance : PvPTeamId.Horde);
					var loserTeam = (byte)(winner == TeamFaction.Alliance ? PvPTeamId.Horde : PvPTeamId.Alliance);

					_arenaTeamScores[winnerTeam].Assign(winnerTeamRating, (uint)(winnerTeamRating + winnerChange), winnerMatchmakerRating, GetArenaMatchmakerRating(winner));
					_arenaTeamScores[loserTeam].Assign(loserTeamRating, (uint)(loserTeamRating + loserChange), loserMatchmakerRating, GetArenaMatchmakerRating(GetOtherTeam(winner)));

					Log.outDebug(LogFilter.Arena,
								"Arena match Type: {0} for Team1Id: {1} - Team2Id: {2} ended. WinnerTeamId: {3}. Winner rating: +{4}, Loser rating: {5}",
								GetArenaType(),
								GetArenaTeamIdByIndex(TeamIds.Alliance),
								GetArenaTeamIdByIndex(TeamIds.Horde),
								winnerArenaTeam.GetId(),
								winnerChange,
								loserChange);

					if (WorldConfig.GetBoolValue(WorldCfg.ArenaLogExtendedInfo))
						foreach (var score in PlayerScores)
						{
							var player = Global.ObjAccessor.FindPlayer(score.Key);

							if (player)
								Log.outDebug(LogFilter.Arena,
											"Statistics match Type: {0} for {1} (GUID: {2}, Team: {3}, IP: {4}): {5}",
											GetArenaType(),
											player.GetName(),
											score.Key,
											player.GetArenaTeamId((byte)(GetArenaType() == ArenaTypes.Team5v5 ? 2 : (GetArenaType() == ArenaTypes.Team3v3 ? 1 : 0))),
											player.Session.RemoteAddress,
											score.Value.ToString());
						}
				}
				// Deduct 16 points from each teams arena-rating if there are no winners after 45+2 minutes
				else
				{
					_arenaTeamScores[(int)PvPTeamId.Alliance].Assign(winnerTeamRating, (uint)(winnerTeamRating + SharedConst.ArenaTimeLimitPointsLoss), winnerMatchmakerRating, GetArenaMatchmakerRating(TeamFaction.Alliance));
					_arenaTeamScores[(int)PvPTeamId.Horde].Assign(loserTeamRating, (uint)(loserTeamRating + SharedConst.ArenaTimeLimitPointsLoss), loserMatchmakerRating, GetArenaMatchmakerRating(TeamFaction.Horde));

					winnerArenaTeam.FinishGame(SharedConst.ArenaTimeLimitPointsLoss);
					loserArenaTeam.FinishGame(SharedConst.ArenaTimeLimitPointsLoss);
				}

				var aliveWinners = GetAlivePlayersCountByTeam(winner);

				foreach (var pair in GetPlayers())
				{
					var team = pair.Value.Team;

					if (pair.Value.OfflineRemoveTime != 0)
					{
						// if rated arena match - make member lost!
						if (team == winner)
						{
							winnerArenaTeam.OfflineMemberLost(pair.Key, loserMatchmakerRating, winnerMatchmakerChange);
						}
						else
						{
							if (winner == 0)
								winnerArenaTeam.OfflineMemberLost(pair.Key, loserMatchmakerRating, winnerMatchmakerChange);

							loserArenaTeam.OfflineMemberLost(pair.Key, winnerMatchmakerRating, loserMatchmakerChange);
						}

						continue;
					}

					var player = _GetPlayer(pair.Key, pair.Value.OfflineRemoveTime != 0, "Arena.EndBattleground");

					if (!player)
						continue;

					// per player calculation
					if (team == winner)
					{
						// update achievement BEFORE personal rating update
						var rating = player.GetArenaPersonalRating(winnerArenaTeam.GetSlot());
						player.UpdateCriteria(CriteriaType.WinAnyRankedArena, rating != 0 ? rating : 1);
						player.UpdateCriteria(CriteriaType.WinArena, GetMapId());

						// Last standing - Rated 5v5 arena & be solely alive player
						if (GetArenaType() == ArenaTypes.Team5v5 && aliveWinners == 1 && player.IsAlive)
							player.CastSpell(player, ArenaSpellIds.LastManStanding, true);

						if (!guildAwarded)
						{
							guildAwarded = true;
							ulong guildId = GetBgMap().GetOwnerGuildId(player.GetBgTeam());

							if (guildId != 0)
							{
								var guild = Global.GuildMgr.GetGuildById(guildId);

								if (guild)
									guild.UpdateCriteria(CriteriaType.WinAnyRankedArena, Math.Max(winnerArenaTeam.GetRating(), 1), 0, 0, null, player);
							}
						}

						winnerArenaTeam.MemberWon(player, loserMatchmakerRating, winnerMatchmakerChange);
					}
					else
					{
						if (winner == 0)
							winnerArenaTeam.MemberLost(player, loserMatchmakerRating, winnerMatchmakerChange);

						loserArenaTeam.MemberLost(player, winnerMatchmakerRating, loserMatchmakerChange);

						// Arena lost => reset the win_rated_arena having the "no_lose" condition
						player.ResetCriteria(CriteriaFailEvent.LoseRankedArenaMatchWithTeamSize, 0);
					}
				}

				// save the stat changes
				winnerArenaTeam.SaveToDB();
				loserArenaTeam.SaveToDB();
				// send updated arena team stats to players
				// this way all arena team members will get notified, not only the ones who participated in this match
				winnerArenaTeam.NotifyStatsChanged();
				loserArenaTeam.NotifyStatsChanged();
			}
		}

		// end Battleground
		base.EndBattleground(winner);
	}

	void UpdateArenaWorldState()
	{
		UpdateWorldState(ArenaWorldStates.AlivePlayersGreen, (int)GetAlivePlayersCountByTeam(TeamFaction.Horde));
		UpdateWorldState(ArenaWorldStates.AlivePlayersGold, (int)GetAlivePlayersCountByTeam(TeamFaction.Alliance));
	}
}

struct ArenaWorldStates
{
	public const int AlivePlayersGreen = 3600;
	public const int AlivePlayersGold = 3601;
	public const int ShowAlivePlayers = 3610;
	public const int TimeRemaining = 8529;
	public const int ShowTimeRemaining = 8524;
	public const int GreenTeamExtraLives = 15480;
	public const int GoldTeamExtraLives = 15481;
	public const int ShowExtraLives = 13401;
	public const int SoloShuffleRound = 21427;
	public const int ShowSoloShuffleRound = 21322;
}