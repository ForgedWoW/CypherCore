﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.BattleGrounds.Zones;

class BgEyeofStorm : Battleground
{
	readonly uint[] m_HonorScoreTics = new uint[2];
	readonly uint[] m_TeamPointsCount = new uint[2];
	readonly uint[] m_Points_Trigger = new uint[EotSPoints.PointsMax];
	readonly TeamFaction[] m_PointOwnedByTeam = new TeamFaction[EotSPoints.PointsMax];
	readonly EotSPointState[] m_PointState = new EotSPointState[EotSPoints.PointsMax];
	readonly EotSProgressBarConsts[] m_PointBarStatus = new EotSProgressBarConsts[EotSPoints.PointsMax];
	readonly BattlegroundPointCaptureStatus[] m_LastPointCaptureStatus = new BattlegroundPointCaptureStatus[EotSPoints.PointsMax];
	readonly List<ObjectGuid>[] m_PlayersNearPoint = new List<ObjectGuid>[EotSPoints.PointsMax + 1];
	readonly byte[] m_CurrentPointPlayersCount = new byte[2 * EotSPoints.PointsMax];

	ObjectGuid m_FlagKeeper; // keepers guid
	ObjectGuid m_DroppedFlagGUID;
	uint m_FlagCapturedBgObjectType; // type that should be despawned when flag is captured
	EotSFlagState m_FlagState;       // for checking flag state
	int m_FlagsTimer;
	int m_TowerCapCheckTimer;

	int m_PointAddingTimer;
	uint m_HonorTics;

	public BgEyeofStorm(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
	{
		m_BuffChange = true;
		BgObjects = new ObjectGuid[EotSObjectTypes.Max];
		BgCreatures = new ObjectGuid[EotSCreaturesTypes.Max];
		m_Points_Trigger[EotSPoints.FelReaver] = EotSPointsTrigger.FelReaverBuff;
		m_Points_Trigger[EotSPoints.BloodElf] = EotSPointsTrigger.BloodElfBuff;
		m_Points_Trigger[EotSPoints.DraeneiRuins] = EotSPointsTrigger.DraeneiRuinsBuff;
		m_Points_Trigger[EotSPoints.MageTower] = EotSPointsTrigger.MageTowerBuff;
		m_HonorScoreTics[TeamIds.Alliance] = 0;
		m_HonorScoreTics[TeamIds.Horde] = 0;
		m_TeamPointsCount[TeamIds.Alliance] = 0;
		m_TeamPointsCount[TeamIds.Horde] = 0;
		m_FlagKeeper.Clear();
		m_DroppedFlagGUID.Clear();
		m_FlagCapturedBgObjectType = 0;
		m_FlagState = EotSFlagState.OnBase;
		m_FlagsTimer = 0;
		m_TowerCapCheckTimer = 0;
		m_PointAddingTimer = 0;
		m_HonorTics = 0;

		for (byte i = 0; i < EotSPoints.PointsMax; ++i)
		{
			m_PointOwnedByTeam[i] = TeamFaction.Other;
			m_PointState[i] = EotSPointState.Uncontrolled;
			m_PointBarStatus[i] = EotSProgressBarConsts.ProgressBarStateMiddle;
			m_LastPointCaptureStatus[i] = BattlegroundPointCaptureStatus.Neutral;
		}

		for (byte i = 0; i < EotSPoints.PointsMax + 1; ++i)
			m_PlayersNearPoint[i] = new List<ObjectGuid>();

		for (byte i = 0; i < 2 * EotSPoints.PointsMax; ++i)
			m_CurrentPointPlayersCount[i] = 0;
	}

	public override void PostUpdateImpl(uint diff)
	{
		if (GetStatus() == BattlegroundStatus.InProgress)
		{
			m_PointAddingTimer -= (int)diff;

			if (m_PointAddingTimer <= 0)
			{
				m_PointAddingTimer = EotSMisc.FPointsTickTime;

				if (m_TeamPointsCount[TeamIds.Alliance] > 0)
					AddPoints(TeamFaction.Alliance, EotSMisc.TickPoints[m_TeamPointsCount[TeamIds.Alliance] - 1]);

				if (m_TeamPointsCount[TeamIds.Horde] > 0)
					AddPoints(TeamFaction.Horde, EotSMisc.TickPoints[m_TeamPointsCount[TeamIds.Horde] - 1]);
			}

			if (m_FlagState == EotSFlagState.WaitRespawn || m_FlagState == EotSFlagState.OnGround)
			{
				m_FlagsTimer -= (int)diff;

				if (m_FlagsTimer < 0)
				{
					m_FlagsTimer = 0;

					if (m_FlagState == EotSFlagState.WaitRespawn)
						RespawnFlag(true);
					else
						RespawnFlagAfterDrop();
				}
			}

			m_TowerCapCheckTimer -= (int)diff;

			if (m_TowerCapCheckTimer <= 0)
			{
				//check if player joined point
				/*I used this order of calls, because although we will check if one player is in gameobject's distance 2 times
				but we can count of players on current point in CheckSomeoneLeftPoint
				*/
				CheckSomeoneJoinedPoint();
				//check if player left point
				CheckSomeoneLeftPo();
				UpdatePointStatuses();
				m_TowerCapCheckTimer = EotSMisc.FPointsTickTime;
			}
		}
	}

	public override void StartingEventCloseDoors()
	{
		SpawnBGObject(EotSObjectTypes.DoorA, BattlegroundConst.RespawnImmediately);
		SpawnBGObject(EotSObjectTypes.DoorH, BattlegroundConst.RespawnImmediately);

		for (var i = EotSObjectTypes.ABannerFelReaverCenter; i < EotSObjectTypes.Max; ++i)
			SpawnBGObject(i, BattlegroundConst.RespawnOneDay);
	}

	public override void StartingEventOpenDoors()
	{
		SpawnBGObject(EotSObjectTypes.DoorA, BattlegroundConst.RespawnOneDay);
		SpawnBGObject(EotSObjectTypes.DoorH, BattlegroundConst.RespawnOneDay);

		for (var i = EotSObjectTypes.NBannerFelReaverLeft; i <= EotSObjectTypes.FlagNetherstorm; ++i)
			SpawnBGObject(i, BattlegroundConst.RespawnImmediately);

		for (var i = 0; i < EotSPoints.PointsMax; ++i)
		{
			//randomly spawn buff
			var buff = (byte)RandomHelper.URand(0, 2);
			SpawnBGObject(EotSObjectTypes.SpeedbuffFelReaver + buff + i * 3, BattlegroundConst.RespawnImmediately);
		}

		// Achievement: Flurry
		TriggerGameEvent(EotSMisc.EventStartBattle);
	}

	public override void EndBattleground(TeamFaction winner)
	{
		// Win reward
		if (winner == TeamFaction.Alliance)
			RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Alliance);

		if (winner == TeamFaction.Horde)
			RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Horde);

		// Complete map reward
		RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Alliance);
		RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Horde);

		base.EndBattleground(winner);
	}

	public override void AddPlayer(Player player)
	{
		var isInBattleground = IsPlayerInBattleground(player.GUID);
		base.AddPlayer(player);

		if (!isInBattleground)
			PlayerScores[player.GUID] = new BgEyeOfStormScore(player.GUID, player.GetBgTeam());

		m_PlayersNearPoint[EotSPoints.PointsMax].Add(player.GUID);
	}

	public override void RemovePlayer(Player player, ObjectGuid guid, TeamFaction team)
	{
		// sometimes flag aura not removed :(
		for (var j = EotSPoints.PointsMax; j >= 0; --j)
		{
			for (var i = 0; i < m_PlayersNearPoint[j].Count; ++i)
				if (m_PlayersNearPoint[j][i] == guid)
					m_PlayersNearPoint[j].RemoveAt(i);
		}

		if (IsFlagPickedup())
			if (m_FlagKeeper == guid)
			{
				if (player)
				{
					EventPlayerDroppedFlag(player);
				}
				else
				{
					SetFlagPicker(ObjectGuid.Empty);
					RespawnFlag(true);
				}
			}
	}

	public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
	{
		if (!player.IsAlive) //hack code, must be removed later
			return;

		switch (trigger)
		{
			case 4530: // Horde Start
			case 4531: // Alliance Start
				if (GetStatus() == BattlegroundStatus.WaitJoin && !entered)
					TeleportPlayerToExploitLocation(player);

				break;
			case EotSPointsTrigger.BloodElfPoint:
				if (m_PointState[EotSPoints.BloodElf] == EotSPointState.UnderControl && m_PointOwnedByTeam[EotSPoints.BloodElf] == GetPlayerTeam(player.GUID))
					if (m_FlagState != 0 && GetFlagPickerGUID() == player.GUID)
						EventPlayerCapturedFlag(player, EotSObjectTypes.FlagBloodElf);

				break;
			case EotSPointsTrigger.FelReaverPoint:
				if (m_PointState[EotSPoints.FelReaver] == EotSPointState.UnderControl && m_PointOwnedByTeam[EotSPoints.FelReaver] == GetPlayerTeam(player.GUID))
					if (m_FlagState != 0 && GetFlagPickerGUID() == player.GUID)
						EventPlayerCapturedFlag(player, EotSObjectTypes.FlagFelReaver);

				break;
			case EotSPointsTrigger.MageTowerPoint:
				if (m_PointState[EotSPoints.MageTower] == EotSPointState.UnderControl && m_PointOwnedByTeam[EotSPoints.MageTower] == GetPlayerTeam(player.GUID))
					if (m_FlagState != 0 && GetFlagPickerGUID() == player.GUID)
						EventPlayerCapturedFlag(player, EotSObjectTypes.FlagMageTower);

				break;
			case EotSPointsTrigger.DraeneiRuinsPoint:
				if (m_PointState[EotSPoints.DraeneiRuins] == EotSPointState.UnderControl && m_PointOwnedByTeam[EotSPoints.DraeneiRuins] == GetPlayerTeam(player.GUID))
					if (m_FlagState != 0 && GetFlagPickerGUID() == player.GUID)
						EventPlayerCapturedFlag(player, EotSObjectTypes.FlagDraeneiRuins);

				break;
			case 4512:
			case 4515:
			case 4517:
			case 4519:
			case 4568:
			case 4569:
			case 4570:
			case 4571:
			case 5866:
				break;
			default:
				base.HandleAreaTrigger(player, trigger, entered);

				break;
		}
	}

	public override bool SetupBattleground()
	{
		// doors
		if (!AddObject(EotSObjectTypes.DoorA, EotSObjectIds.ADoorEyEntry, 2527.59716796875f, 1596.90625f, 1238.4544677734375f, 3.159139871597290039f, 0.173641681671142578f, 0.001514434814453125f, -0.98476982116699218f, 0.008638577535748481f, BattlegroundConst.RespawnImmediately) ||
			!AddObject(EotSObjectTypes.DoorH, EotSObjectIds.HDoorEyEntry, 1803.2066650390625f, 1539.486083984375f, 1238.4544677734375f, 3.13898324966430664f, 0.173647880554199218f, 0.0f, 0.984807014465332031f, 0.001244877814315259f, BattlegroundConst.RespawnImmediately)
			// banners (alliance)
			||
			!AddObject(EotSObjectTypes.ABannerFelReaverCenter, EotSObjectIds.ABannerEyEntry, 2057.47265625f, 1735.109130859375f, 1188.065673828125f, 5.305802345275878906f, 0.0f, 0.0f, -0.46947097778320312f, 0.882947921752929687f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerFelReaverLeft, EotSObjectIds.ABannerEyEntry, 2032.248291015625f, 1729.546875f, 1191.2296142578125f, 1.797688722610473632f, 0.0f, 0.0f, 0.7826080322265625f, 0.622514784336090087f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerFelReaverRight, EotSObjectIds.ABannerEyEntry, 2092.338623046875f, 1775.4739990234375f, 1187.504150390625f, 5.811946868896484375f, 0.0f, 0.0f, -0.2334451675415039f, 0.972369968891143798f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerBloodElfCenter, EotSObjectIds.ABannerEyEntry, 2047.1910400390625f, 1349.1927490234375f, 1189.0032958984375f, 4.660029888153076171f, 0.0f, 0.0f, -0.72537422180175781f, 0.688354730606079101f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerBloodElfLeft, EotSObjectIds.ABannerEyEntry, 2074.319580078125f, 1385.779541015625f, 1194.7203369140625f, 0.488691210746765136f, 0.0f, 0.0f, 0.241921424865722656f, 0.970295846462249755f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerBloodElfRight, EotSObjectIds.ABannerEyEntry, 2025.125f, 1386.123291015625f, 1192.7354736328125f, 2.391098499298095703f, 0.0f, 0.0f, 0.930417060852050781f, 0.366502493619918823f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerDraeneiRuinsCenter, EotSObjectIds.ABannerEyEntry, 2276.796875f, 1400.407958984375f, 1196.333740234375f, 2.44346022605895996f, 0.0f, 0.0f, 0.939692497253417968f, 0.34202045202255249f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerDraeneiRuinsLeft, EotSObjectIds.ABannerEyEntry, 2305.776123046875f, 1404.5572509765625f, 1199.384765625f, 1.745326757431030273f, 0.0f, 0.0f, 0.766043663024902343f, 0.642788589000701904f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerDraeneiRuinsRight, EotSObjectIds.ABannerEyEntry, 2245.395751953125f, 1366.4132080078125f, 1195.27880859375f, 2.216565132141113281f, 0.0f, 0.0f, 0.894933700561523437f, 0.44619917869567871f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerMageTowerCenter, EotSObjectIds.ABannerEyEntry, 2270.8359375f, 1784.080322265625f, 1186.757080078125f, 2.426007747650146484f, 0.0f, 0.0f, 0.936672210693359375f, 0.350207358598709106f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerMageTowerLeft, EotSObjectIds.ABannerEyEntry, 2269.126708984375f, 1737.703125f, 1186.8145751953125f, 0.994837164878845214f, 0.0f, 0.0f, 0.477158546447753906f, 0.878817260265350341f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.ABannerMageTowerRight, EotSObjectIds.ABannerEyEntry, 2300.85595703125f, 1741.24658203125f, 1187.793212890625f, 5.497788906097412109f, 0.0f, 0.0f, -0.38268280029296875f, 0.923879802227020263f, BattlegroundConst.RespawnOneDay)
			// banners (horde)
			||
			!AddObject(EotSObjectTypes.HBannerFelReaverCenter, EotSObjectIds.HBannerEyEntry, 2057.45654296875f, 1735.07470703125f, 1187.9063720703125f, 5.35816192626953125f, 0.0f, 0.0f, -0.446197509765625f, 0.894934535026550292f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerFelReaverLeft, EotSObjectIds.HBannerEyEntry, 2032.251708984375f, 1729.532958984375f, 1190.3251953125f, 1.867502212524414062f, 0.0f, 0.0f, 0.803856849670410156f, 0.594822824001312255f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerFelReaverRight, EotSObjectIds.HBannerEyEntry, 2092.354248046875f, 1775.4583740234375f, 1187.079345703125f, 5.881760597229003906f, 0.0f, 0.0f, -0.19936752319335937f, 0.979924798011779785f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerBloodElfCenter, EotSObjectIds.HBannerEyEntry, 2047.1978759765625f, 1349.1875f, 1188.5650634765625f, 4.625123500823974609f, 0.0f, 0.0f, -0.73727703094482421f, 0.67559051513671875f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerBloodElfLeft, EotSObjectIds.HBannerEyEntry, 2074.3056640625f, 1385.7725830078125f, 1194.4686279296875f, 0.471238493919372558f, 0.0f, 0.0f, 0.233445167541503906f, 0.972369968891143798f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerBloodElfRight, EotSObjectIds.HBannerEyEntry, 2025.09375f, 1386.12158203125f, 1192.6536865234375f, 2.373644113540649414f, 0.0f, 0.0f, 0.927183151245117187f, 0.37460830807685852f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerDraeneiRuinsCenter, EotSObjectIds.HBannerEyEntry, 2276.798583984375f, 1400.4410400390625f, 1196.2200927734375f, 2.495818138122558593f, 0.0f, 0.0f, 0.948323249816894531f, 0.317305892705917358f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerDraeneiRuinsLeft, EotSObjectIds.HBannerEyEntry, 2305.763916015625f, 1404.5972900390625f, 1199.3333740234375f, 1.640606880187988281f, 0.0f, 0.0f, 0.731352806091308593f, 0.6819993257522583f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerDraeneiRuinsRight, EotSObjectIds.HBannerEyEntry, 2245.382080078125f, 1366.454833984375f, 1195.1815185546875f, 2.373644113540649414f, 0.0f, 0.0f, 0.927183151245117187f, 0.37460830807685852f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerMageTowerCenter, EotSObjectIds.HBannerEyEntry, 2270.869873046875f, 1784.0989990234375f, 1186.4384765625f, 2.356194972991943359f, 0.0f, 0.0f, 0.923879623413085937f, 0.382683247327804565f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerMageTowerLeft, EotSObjectIds.HBannerEyEntry, 2268.59716796875f, 1737.0191650390625f, 1186.75390625f, 0.942476630210876464f, 0.0f, 0.0f, 0.453989982604980468f, 0.891006767749786376f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.HBannerMageTowerRight, EotSObjectIds.HBannerEyEntry, 2301.01904296875f, 1741.4930419921875f, 1187.48974609375f, 5.375615119934082031f, 0.0f, 0.0f, -0.4383707046508789f, 0.898794233798980712f, BattlegroundConst.RespawnOneDay)
			// banners (natural)
			||
			!AddObject(EotSObjectTypes.NBannerFelReaverCenter, EotSObjectIds.NBannerEyEntry, 2057.4931640625f, 1735.111083984375f, 1187.675537109375f, 5.340708732604980468f, 0.0f, 0.0f, -0.45398998260498046f, 0.891006767749786376f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerFelReaverLeft, EotSObjectIds.NBannerEyEntry, 2032.2569580078125f, 1729.5572509765625f, 1191.0802001953125f, 1.797688722610473632f, 0.0f, 0.0f, 0.7826080322265625f, 0.622514784336090087f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerFelReaverRight, EotSObjectIds.NBannerEyEntry, 2092.395751953125f, 1775.451416015625f, 1186.965576171875f, 5.89921426773071289f, 0.0f, 0.0f, -0.19080829620361328f, 0.981627285480499267f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerBloodElfCenter, EotSObjectIds.NBannerEyEntry, 2047.1875f, 1349.1944580078125f, 1188.5731201171875f, 4.642575740814208984f, 0.0f, 0.0f, -0.731353759765625f, 0.681998312473297119f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerBloodElfLeft, EotSObjectIds.NBannerEyEntry, 2074.3212890625f, 1385.76220703125f, 1194.362060546875f, 0.488691210746765136f, 0.0f, 0.0f, 0.241921424865722656f, 0.970295846462249755f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerBloodElfRight, EotSObjectIds.NBannerEyEntry, 2025.13720703125f, 1386.1336669921875f, 1192.5482177734375f, 2.391098499298095703f, 0.0f, 0.0f, 0.930417060852050781f, 0.366502493619918823f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerDraeneiRuinsCenter, EotSObjectIds.NBannerEyEntry, 2276.833251953125f, 1400.4375f, 1196.146728515625f, 2.478367090225219726f, 0.0f, 0.0f, 0.94551849365234375f, 0.325568377971649169f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerDraeneiRuinsLeft, EotSObjectIds.NBannerEyEntry, 2305.77783203125f, 1404.5364990234375f, 1199.246337890625f, 1.570795774459838867f, 0.0f, 0.0f, 0.707106590270996093f, 0.707106947898864746f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerDraeneiRuinsRight, EotSObjectIds.NBannerEyEntry, 2245.40966796875f, 1366.4410400390625f, 1195.1107177734375f, 2.356194972991943359f, 0.0f, 0.0f, 0.923879623413085937f, 0.382683247327804565f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerMageTowerCenter, EotSObjectIds.NBannerEyEntry, 2270.84033203125f, 1784.1197509765625f, 1186.1473388671875f, 2.303830623626708984f, 0.0f, 0.0f, 0.913544654846191406f, 0.406738430261611938f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerMageTowerLeft, EotSObjectIds.NBannerEyEntry, 2268.46533203125f, 1736.8385009765625f, 1186.742919921875f, 0.942476630210876464f, 0.0f, 0.0f, 0.453989982604980468f, 0.891006767749786376f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.NBannerMageTowerRight, EotSObjectIds.NBannerEyEntry, 2300.9931640625f, 1741.5504150390625f, 1187.10693359375f, 5.375615119934082031f, 0.0f, 0.0f, -0.4383707046508789f, 0.898794233798980712f, BattlegroundConst.RespawnOneDay)
			// flags
			||
			!AddObject(EotSObjectTypes.FlagNetherstorm, EotSObjectIds.Flag2EyEntry, 2174.444580078125f, 1569.421875f, 1159.852783203125f, 4.625123500823974609f, 0.0f, 0.0f, -0.73727703094482421f, 0.67559051513671875f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.FlagFelReaver, EotSObjectIds.Flag1EyEntry, 2044.28f, 1729.68f, 1189.96f, -0.017453f, 0, 0, 0.008727f, -0.999962f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.FlagBloodElf, EotSObjectIds.Flag1EyEntry, 2048.83f, 1393.65f, 1194.49f, 0.20944f, 0, 0, 0.104528f, 0.994522f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.FlagDraeneiRuins, EotSObjectIds.Flag1EyEntry, 2286.56f, 1402.36f, 1197.11f, 3.72381f, 0, 0, 0.957926f, -0.287016f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.FlagMageTower, EotSObjectIds.Flag1EyEntry, 2284.48f, 1731.23f, 1189.99f, 2.89725f, 0, 0, 0.992546f, 0.121869f, BattlegroundConst.RespawnOneDay)
			// tower cap
			||
			!AddObject(EotSObjectTypes.TowerCapFelReaver, EotSObjectIds.FrTowerCapEyEntry, 2024.600708f, 1742.819580f, 1195.157715f, 2.443461f, 0, 0, 0.939693f, 0.342020f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.TowerCapBloodElf, EotSObjectIds.BeTowerCapEyEntry, 2050.493164f, 1372.235962f, 1194.563477f, 1.710423f, 0, 0, 0.754710f, 0.656059f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.TowerCapDraeneiRuins, EotSObjectIds.DrTowerCapEyEntry, 2301.010498f, 1386.931641f, 1197.183472f, 1.570796f, 0, 0, 0.707107f, 0.707107f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.TowerCapMageTower, EotSObjectIds.HuTowerCapEyEntry, 2282.121582f, 1760.006958f, 1189.707153f, 1.919862f, 0, 0, 0.819152f, 0.573576f, BattlegroundConst.RespawnOneDay)
			// buffs
			||
			!AddObject(EotSObjectTypes.SpeedbuffFelReaver, EotSObjectIds.SpeedBuffFelReaverEyEntry, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.RegenbuffFelReaver, EotSObjectIds.RestorationBuffFelReaverEyEntry, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.BerserkbuffFelReaver, EotSObjectIds.BerserkBuffFelReaverEyEntry, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.SpeedbuffBloodElf, EotSObjectIds.SpeedBuffBloodElfEyEntry, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.RegenbuffBloodElf, EotSObjectIds.RestorationBuffBloodElfEyEntry, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.BerserkbuffBloodElf, EotSObjectIds.BerserkBuffBloodElfEyEntry, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.SpeedbuffDraeneiRuins, EotSObjectIds.SpeedBuffDraeneiRuinsEyEntry, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.RegenbuffDraeneiRuins, EotSObjectIds.RestorationBuffDraeneiRuinsEyEntry, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.BerserkbuffDraeneiRuins, EotSObjectIds.BerserkBuffDraeneiRuinsEyEntry, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.SpeedbuffMageTower, EotSObjectIds.SpeedBuffMageTowerEyEntry, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.RegenbuffMageTower, EotSObjectIds.RestorationBuffMageTowerEyEntry, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RespawnOneDay) ||
			!AddObject(EotSObjectTypes.BerserkbuffMageTower, EotSObjectIds.BerserkBuffMageTowerEyEntry, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RespawnOneDay)
			)
		{
			Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn some objects. The battleground was not created.");

			return false;
		}

		var sg = Global.ObjectMgr.GetWorldSafeLoc(EotSGaveyardIds.MainAlliance);

		if (sg == null || !AddSpiritGuide(EotSCreaturesTypes.SpiritMainAlliance, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.124139f, TeamIds.Alliance))
		{
			Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn spirit guide. The battleground was not created.");

			return false;
		}

		sg = Global.ObjectMgr.GetWorldSafeLoc(EotSGaveyardIds.MainHorde);

		if (sg == null || !AddSpiritGuide(EotSCreaturesTypes.SpiritMainHorde, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.193953f, TeamIds.Horde))
		{
			Log.outError(LogFilter.Sql, "BatteGroundEY: Failed to spawn spirit guide. The battleground was not created.");

			return false;
		}

		return true;
	}

	public override void Reset()
	{
		//call parent's class reset
		base.Reset();

		m_TeamScores[TeamIds.Alliance] = 0;
		m_TeamScores[TeamIds.Horde] = 0;
		m_TeamPointsCount[TeamIds.Alliance] = 0;
		m_TeamPointsCount[TeamIds.Horde] = 0;
		m_HonorScoreTics[TeamIds.Alliance] = 0;
		m_HonorScoreTics[TeamIds.Horde] = 0;
		m_FlagState = EotSFlagState.OnBase;
		m_FlagCapturedBgObjectType = 0;
		m_FlagKeeper.Clear();
		m_DroppedFlagGUID.Clear();
		m_PointAddingTimer = 0;
		m_TowerCapCheckTimer = 0;
		var isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(GetTypeID());
		m_HonorTics = (isBGWeekend) ? EotSMisc.EYWeekendHonorTicks : EotSMisc.NotEYWeekendHonorTicks;

		for (byte i = 0; i < EotSPoints.PointsMax; ++i)
		{
			m_PointOwnedByTeam[i] = TeamFaction.Other;
			m_PointState[i] = EotSPointState.Uncontrolled;
			m_PointBarStatus[i] = EotSProgressBarConsts.ProgressBarStateMiddle;
			m_PlayersNearPoint[i].Clear();
		}

		m_PlayersNearPoint[EotSPoints.PlayersOutOfPoints].Clear();
	}

	public override void HandleKillPlayer(Player player, Player killer)
	{
		if (GetStatus() != BattlegroundStatus.InProgress)
			return;

		base.HandleKillPlayer(player, killer);
		EventPlayerDroppedFlag(player);
	}

	public override void EventPlayerDroppedFlag(Player player)
	{
		if (GetStatus() != BattlegroundStatus.InProgress)
		{
			// if not running, do not cast things at the dropper player, neither send unnecessary messages
			// just take off the aura
			if (IsFlagPickedup() && GetFlagPickerGUID() == player.GUID)
			{
				SetFlagPicker(ObjectGuid.Empty);
				player.RemoveAura(EotSMisc.SpellNetherstormFlag);
			}

			return;
		}

		if (!IsFlagPickedup())
			return;

		if (GetFlagPickerGUID() != player.GUID)
			return;

		SetFlagPicker(ObjectGuid.Empty);
		player.RemoveAura(EotSMisc.SpellNetherstormFlag);
		m_FlagState = EotSFlagState.OnGround;
		m_FlagsTimer = EotSMisc.FlagRespawnTime;
		player.CastSpell(player, BattlegroundConst.SpellRecentlyDroppedFlag, true);
		player.CastSpell(player, EotSMisc.SpellPlayerDroppedFlag, true);
		//this does not work correctly :((it should remove flag carrier name)
		UpdateWorldState(EotSWorldStateIds.NetherstormFlagStateHorde, (int)EotSFlagState.WaitRespawn);
		UpdateWorldState(EotSWorldStateIds.NetherstormFlagStateAlliance, (int)EotSFlagState.WaitRespawn);

		if (GetPlayerTeam(player.GUID) == TeamFaction.Alliance)
			SendBroadcastText(EotSBroadcastTexts.FlagDropped, ChatMsg.BgSystemAlliance, null);
		else
			SendBroadcastText(EotSBroadcastTexts.FlagDropped, ChatMsg.BgSystemHorde, null);
	}

	public override void EventPlayerClickedOnFlag(Player player, GameObject target_obj)
	{
		if (GetStatus() != BattlegroundStatus.InProgress || IsFlagPickedup() || !player.IsWithinDistInMap(target_obj, 10))
			return;

		if (GetPlayerTeam(player.GUID) == TeamFaction.Alliance)
		{
			UpdateWorldState(EotSWorldStateIds.NetherstormFlagStateAlliance, (int)EotSFlagState.OnPlayer);
			PlaySoundToAll(EotSSoundIds.FlagPickedUpAlliance);
		}
		else
		{
			UpdateWorldState(EotSWorldStateIds.NetherstormFlagStateHorde, (int)EotSFlagState.OnPlayer);
			PlaySoundToAll(EotSSoundIds.FlagPickedUpHorde);
		}

		if (m_FlagState == EotSFlagState.OnBase)
			UpdateWorldState(EotSWorldStateIds.NetherstormFlag, 0);

		m_FlagState = EotSFlagState.OnPlayer;

		SpawnBGObject(EotSObjectTypes.FlagNetherstorm, BattlegroundConst.RespawnOneDay);
		SetFlagPicker(player.GUID);
		//get flag aura on player
		player.CastSpell(player, EotSMisc.SpellNetherstormFlag, true);
		player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);

		if (GetPlayerTeam(player.GUID) == TeamFaction.Alliance)
			SendBroadcastText(EotSBroadcastTexts.TakenFlag, ChatMsg.BgSystemAlliance, player);
		else
			SendBroadcastText(EotSBroadcastTexts.TakenFlag, ChatMsg.BgSystemHorde, player);
	}

	public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
	{
		if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
			return false;

		switch (type)
		{
			case ScoreType.FlagCaptures:
				player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, EotSMisc.ObjectiveCaptureFlag);

				break;
			default:
				break;
		}

		return true;
	}

	public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
	{
		uint g_id;
		var team = GetPlayerTeam(player.GUID);

		switch (team)
		{
			case TeamFaction.Alliance:
				g_id = EotSGaveyardIds.MainAlliance;

				break;
			case TeamFaction.Horde:
				g_id = EotSGaveyardIds.MainHorde;

				break;
			default: return null;
		}

		var entry = Global.ObjectMgr.GetWorldSafeLoc(g_id);
		var nearestEntry = entry;

		if (entry == null)
		{
			Log.outError(LogFilter.Battleground, "BattlegroundEY: The main team graveyard could not be found. The graveyard system will not be operational!");

			return null;
		}

		var plr_x = player.Location.X;
		var plr_y = player.Location.Y;
		var plr_z = player.Location.Z;

		var distance = (entry.Loc.X - plr_x) * (entry.Loc.X - plr_x) + (entry.Loc.Y - plr_y) * (entry.Loc.Y - plr_y) + (entry.Loc.Z - plr_z) * (entry.Loc.Z - plr_z);
		var nearestDistance = distance;

		for (byte i = 0; i < EotSPoints.PointsMax; ++i)
			if (m_PointOwnedByTeam[i] == team && m_PointState[i] == EotSPointState.UnderControl)
			{
				entry = Global.ObjectMgr.GetWorldSafeLoc(EotSMisc.m_CapturingPointTypes[i].GraveYardId);

				if (entry == null)
				{
					Log.outError(LogFilter.Battleground, "BattlegroundEY: Graveyard {0} could not be found.", EotSMisc.m_CapturingPointTypes[i].GraveYardId);
				}
				else
				{
					distance = (entry.Loc.X - plr_x) * (entry.Loc.X - plr_x) + (entry.Loc.Y - plr_y) * (entry.Loc.Y - plr_y) + (entry.Loc.Z - plr_z) * (entry.Loc.Z - plr_z);

					if (distance < nearestDistance)
					{
						nearestDistance = distance;
						nearestEntry = entry;
					}
				}
			}

		return nearestEntry;
	}

	public override WorldSafeLocsEntry GetExploitTeleportLocation(TeamFaction team)
	{
		return Global.ObjectMgr.GetWorldSafeLoc(team == TeamFaction.Alliance ? EotSMisc.ExploitTeleportLocationAlliance : EotSMisc.ExploitTeleportLocationHorde);
	}

	public override TeamFaction GetPrematureWinner()
	{
		if (GetTeamScore(TeamIds.Alliance) > GetTeamScore(TeamIds.Horde))
			return TeamFaction.Alliance;
		else if (GetTeamScore(TeamIds.Horde) > GetTeamScore(TeamIds.Alliance))
			return TeamFaction.Horde;

		return base.GetPrematureWinner();
	}

	public override ObjectGuid GetFlagPickerGUID(int team = -1)
	{
		return m_FlagKeeper;
	}

	public override void SetDroppedFlagGUID(ObjectGuid guid, int TeamID = -1)
	{
		m_DroppedFlagGUID = guid;
	}

	void AddPoints(TeamFaction Team, uint Points)
	{
		var team_index = GetTeamIndexByTeamId(Team);
		m_TeamScores[team_index] += Points;
		m_HonorScoreTics[team_index] += Points;

		if (m_HonorScoreTics[team_index] >= m_HonorTics)
		{
			RewardHonorToTeam(GetBonusHonorFromKill(1), Team);
			m_HonorScoreTics[team_index] -= m_HonorTics;
		}

		UpdateTeamScore(team_index);
	}

	BattlegroundPointCaptureStatus GetPointCaptureStatus(uint point)
	{
		if (m_PointBarStatus[point] >= EotSProgressBarConsts.ProgressBarAliControlled)
			return BattlegroundPointCaptureStatus.AllianceControlled;

		if (m_PointBarStatus[point] <= EotSProgressBarConsts.ProgressBarHordeControlled)
			return BattlegroundPointCaptureStatus.HordeControlled;

		if (m_CurrentPointPlayersCount[2 * point] == m_CurrentPointPlayersCount[2 * point + 1])
			return BattlegroundPointCaptureStatus.Neutral;

		return m_CurrentPointPlayersCount[2 * point] > m_CurrentPointPlayersCount[2 * point + 1]
					? BattlegroundPointCaptureStatus.AllianceCapturing
					: BattlegroundPointCaptureStatus.HordeCapturing;
	}

	void CheckSomeoneJoinedPoint()
	{
		GameObject obj;

		for (byte i = 0; i < EotSPoints.PointsMax; ++i)
		{
			obj = GetBgMap().GetGameObject(BgObjects[EotSObjectTypes.TowerCapFelReaver + i]);

			if (obj)
			{
				byte j = 0;

				while (j < m_PlayersNearPoint[EotSPoints.PointsMax].Count)
				{
					var player = Global.ObjAccessor.FindPlayer(m_PlayersNearPoint[EotSPoints.PointsMax][j]);

					if (!player)
					{
						Log.outError(LogFilter.Battleground, "BattlegroundEY:CheckSomeoneJoinedPoint: Player ({0}) could not be found!", m_PlayersNearPoint[EotSPoints.PointsMax][j].ToString());
						++j;

						continue;
					}

					if (player.CanCaptureTowerPoint && player.IsWithinDistInMap(obj, (float)EotSProgressBarConsts.PointRadius))
					{
						//player joined point!
						//show progress bar
						player.SendUpdateWorldState(EotSWorldStateIds.ProgressBarPercentGrey, (uint)EotSProgressBarConsts.ProgressBarPercentGrey);
						player.SendUpdateWorldState(EotSWorldStateIds.ProgressBarStatus, (uint)m_PointBarStatus[i]);
						player.SendUpdateWorldState(EotSWorldStateIds.ProgressBarShow, (uint)EotSProgressBarConsts.ProgressBarShow);
						//add player to point
						m_PlayersNearPoint[i].Add(m_PlayersNearPoint[EotSPoints.PointsMax][j]);
						//remove player from "free space"
						m_PlayersNearPoint[EotSPoints.PointsMax].RemoveAt(j);
					}
					else
					{
						++j;
					}
				}
			}
		}
	}

	void CheckSomeoneLeftPo()
	{
		//reset current point counts
		for (byte i = 0; i < 2 * EotSPoints.PointsMax; ++i)
			m_CurrentPointPlayersCount[i] = 0;

		GameObject obj;

		for (byte i = 0; i < EotSPoints.PointsMax; ++i)
		{
			obj = GetBgMap().GetGameObject(BgObjects[EotSObjectTypes.TowerCapFelReaver + i]);

			if (obj)
			{
				byte j = 0;

				while (j < m_PlayersNearPoint[i].Count)
				{
					var player = Global.ObjAccessor.FindPlayer(m_PlayersNearPoint[i][j]);

					if (!player)
					{
						Log.outError(LogFilter.Battleground, "BattlegroundEY:CheckSomeoneLeftPoint Player ({0}) could not be found!", m_PlayersNearPoint[i][j].ToString());
						//move non-existing players to "free space" - this will cause many errors showing in log, but it is a very important bug
						m_PlayersNearPoint[EotSPoints.PointsMax].Add(m_PlayersNearPoint[i][j]);
						m_PlayersNearPoint[i].RemoveAt(j);

						continue;
					}

					if (!player.CanCaptureTowerPoint || !player.IsWithinDistInMap(obj, (float)EotSProgressBarConsts.PointRadius))
						//move player out of point (add him to players that are out of points
					{
						m_PlayersNearPoint[EotSPoints.PointsMax].Add(m_PlayersNearPoint[i][j]);
						m_PlayersNearPoint[i].RemoveAt(j);
						player.SendUpdateWorldState(EotSWorldStateIds.ProgressBarShow, (uint)EotSProgressBarConsts.ProgressBarDontShow);
					}
					else
					{
						//player is neat flag, so update count:
						m_CurrentPointPlayersCount[2 * i + GetTeamIndexByTeamId(GetPlayerTeam(player.GUID))]++;
						++j;
					}
				}
			}
		}
	}

	void UpdatePointStatuses()
	{
		for (byte point = 0; point < EotSPoints.PointsMax; ++point)
		{
			if (!m_PlayersNearPoint[point].Empty())
			{
				//count new point bar status:
				var pointDelta = (int)(m_CurrentPointPlayersCount[2 * point]) - (int)(m_CurrentPointPlayersCount[2 * point + 1]);
				MathFunctions.RoundToInterval(ref pointDelta, -(int)EotSProgressBarConsts.PointMaxCapturersCount, EotSProgressBarConsts.PointMaxCapturersCount);
				m_PointBarStatus[point] += pointDelta;

				if (m_PointBarStatus[point] > EotSProgressBarConsts.ProgressBarAliControlled)
					//point is fully alliance's
					m_PointBarStatus[point] = EotSProgressBarConsts.ProgressBarAliControlled;

				if (m_PointBarStatus[point] < EotSProgressBarConsts.ProgressBarHordeControlled)
					//point is fully horde's
					m_PointBarStatus[point] = EotSProgressBarConsts.ProgressBarHordeControlled;

				uint pointOwnerTeamId;

				//find which team should own this point
				if (m_PointBarStatus[point] <= EotSProgressBarConsts.ProgressBarNeutralLow)
					pointOwnerTeamId = (uint)TeamFaction.Horde;
				else if (m_PointBarStatus[point] >= EotSProgressBarConsts.ProgressBarNeutralHigh)
					pointOwnerTeamId = (uint)TeamFaction.Alliance;
				else
					pointOwnerTeamId = (uint)EotSPointState.NoOwner;

				for (byte i = 0; i < m_PlayersNearPoint[point].Count; ++i)
				{
					var player = Global.ObjAccessor.FindPlayer(m_PlayersNearPoint[point][i]);

					if (player)
					{
						player.SendUpdateWorldState(EotSWorldStateIds.ProgressBarStatus, (uint)m_PointBarStatus[point]);
						var team = GetPlayerTeam(player.GUID);

						//if point owner changed we must evoke event!
						if (pointOwnerTeamId != (uint)m_PointOwnedByTeam[point])
						{
							//point was uncontrolled and player is from team which captured point
							if (m_PointState[point] == EotSPointState.Uncontrolled && (uint)team == pointOwnerTeamId)
								EventTeamCapturedPoint(player, point);

							//point was under control and player isn't from team which controlled it
							if (m_PointState[point] == EotSPointState.UnderControl && team != m_PointOwnedByTeam[point])
								EventTeamLostPoint(player, point);
						}

						// @workaround The original AreaTrigger is covered by a bigger one and not triggered on client side.
						if (point == EotSPoints.FelReaver && m_PointOwnedByTeam[point] == team)
							if (m_FlagState != 0 && GetFlagPickerGUID() == player.GUID)
								if (player.GetDistance(2044.0f, 1729.729f, 1190.03f) < 3.0f)
									EventPlayerCapturedFlag(player, EotSObjectTypes.FlagFelReaver);
					}
				}
			}

			var captureStatus = GetPointCaptureStatus(point);

			if (m_LastPointCaptureStatus[point] != captureStatus)
			{
				UpdateWorldState(EotSMisc.m_PointsIconStruct[point].WorldStateAllianceStatusBarIcon, (int)(captureStatus == BattlegroundPointCaptureStatus.AllianceControlled ? 2 : (captureStatus == BattlegroundPointCaptureStatus.AllianceCapturing ? 1 : 0)));
				UpdateWorldState(EotSMisc.m_PointsIconStruct[point].WorldStateHordeStatusBarIcon, (int)(captureStatus == BattlegroundPointCaptureStatus.HordeControlled ? 2 : (captureStatus == BattlegroundPointCaptureStatus.HordeCapturing ? 1 : 0)));
				m_LastPointCaptureStatus[point] = captureStatus;
			}
		}
	}

	void UpdateTeamScore(int team)
	{
		var score = GetTeamScore(team);

		if (score >= EotSScoreIds.MaxTeamScore)
		{
			score = EotSScoreIds.MaxTeamScore;

			if (team == TeamIds.Alliance)
				EndBattleground(TeamFaction.Alliance);
			else
				EndBattleground(TeamFaction.Horde);
		}

		if (team == TeamIds.Alliance)
			UpdateWorldState(EotSWorldStateIds.AllianceResources, (int)score);
		else
			UpdateWorldState(EotSWorldStateIds.HordeResources, (int)score);
	}

	void UpdatePointsCount(TeamFaction team)
	{
		if (team == TeamFaction.Alliance)
			UpdateWorldState(EotSWorldStateIds.AllianceBase, (int)m_TeamPointsCount[TeamIds.Alliance]);
		else
			UpdateWorldState(EotSWorldStateIds.HordeBase, (int)m_TeamPointsCount[TeamIds.Horde]);
	}

	void UpdatePointsIcons(TeamFaction team, int Point)
	{
		//we MUST firstly send 0, after that we can send 1!!!
		if (m_PointState[Point] == EotSPointState.UnderControl)
		{
			UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateControlIndex, 0);

			if (team == TeamFaction.Alliance)
				UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateAllianceControlledIndex, 1);
			else
				UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateHordeControlledIndex, 1);
		}
		else
		{
			if (team == TeamFaction.Alliance)
				UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateAllianceControlledIndex, 0);
			else
				UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateHordeControlledIndex, 0);

			UpdateWorldState(EotSMisc.m_PointsIconStruct[Point].WorldStateControlIndex, 1);
		}
	}

	void RespawnFlag(bool send_message)
	{
		if (m_FlagCapturedBgObjectType > 0)
			SpawnBGObject((int)m_FlagCapturedBgObjectType, BattlegroundConst.RespawnOneDay);

		m_FlagCapturedBgObjectType = 0;
		m_FlagState = EotSFlagState.OnBase;
		SpawnBGObject(EotSObjectTypes.FlagNetherstorm, BattlegroundConst.RespawnImmediately);

		if (send_message)
		{
			SendBroadcastText(EotSBroadcastTexts.FlagReset, ChatMsg.BgSystemNeutral);
			PlaySoundToAll(EotSSoundIds.FlagReset); // flags respawned sound...
		}

		UpdateWorldState(EotSWorldStateIds.NetherstormFlag, 1);
	}

	void RespawnFlagAfterDrop()
	{
		RespawnFlag(true);

		var obj = GetBgMap().GetGameObject(GetDroppedFlagGUID());

		if (obj)
			obj.Delete();
		else
			Log.outError(LogFilter.Battleground, "BattlegroundEY: Unknown dropped flag ({0}).", GetDroppedFlagGUID().ToString());

		SetDroppedFlagGUID(ObjectGuid.Empty);
	}

	void EventTeamLostPoint(Player player, int Point)
	{
		if (GetStatus() != BattlegroundStatus.InProgress)
			return;

		//Natural point
		var Team = m_PointOwnedByTeam[Point];

		if (Team == 0)
			return;

		if (Team == TeamFaction.Alliance)
		{
			m_TeamPointsCount[TeamIds.Alliance]--;
			SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeAlliance, BattlegroundConst.RespawnOneDay);
			SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeAlliance + 1, BattlegroundConst.RespawnOneDay);
			SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeAlliance + 2, BattlegroundConst.RespawnOneDay);
		}
		else
		{
			m_TeamPointsCount[TeamIds.Horde]--;
			SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeHorde, BattlegroundConst.RespawnOneDay);
			SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeHorde + 1, BattlegroundConst.RespawnOneDay);
			SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].DespawnObjectTypeHorde + 2, BattlegroundConst.RespawnOneDay);
		}

		SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].SpawnNeutralObjectType, BattlegroundConst.RespawnImmediately);
		SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].SpawnNeutralObjectType + 1, BattlegroundConst.RespawnImmediately);
		SpawnBGObject(EotSMisc.m_LosingPointTypes[Point].SpawnNeutralObjectType + 2, BattlegroundConst.RespawnImmediately);

		//buff isn't despawned

		m_PointOwnedByTeam[Point] = TeamFaction.Other;
		m_PointState[Point] = EotSPointState.NoOwner;

		if (Team == TeamFaction.Alliance)
			SendBroadcastText(EotSMisc.m_LosingPointTypes[Point].MessageIdAlliance, ChatMsg.BgSystemAlliance, player);
		else
			SendBroadcastText(EotSMisc.m_LosingPointTypes[Point].MessageIdHorde, ChatMsg.BgSystemHorde, player);

		UpdatePointsIcons(Team, Point);
		UpdatePointsCount(Team);

		//remove bonus honor aura trigger creature when node is lost
		if (Point < EotSPoints.PointsMax)
			DelCreature(Point + 6); //null checks are in DelCreature! 0-5 spirit guides
	}

	void EventTeamCapturedPoint(Player player, int Point)
	{
		if (GetStatus() != BattlegroundStatus.InProgress)
			return;

		var Team = GetPlayerTeam(player.GUID);

		SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].DespawnNeutralObjectType, BattlegroundConst.RespawnOneDay);
		SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].DespawnNeutralObjectType + 1, BattlegroundConst.RespawnOneDay);
		SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].DespawnNeutralObjectType + 2, BattlegroundConst.RespawnOneDay);

		if (Team == TeamFaction.Alliance)
		{
			m_TeamPointsCount[TeamIds.Alliance]++;
			SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeAlliance, BattlegroundConst.RespawnImmediately);
			SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeAlliance + 1, BattlegroundConst.RespawnImmediately);
			SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeAlliance + 2, BattlegroundConst.RespawnImmediately);
		}
		else
		{
			m_TeamPointsCount[TeamIds.Horde]++;
			SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeHorde, BattlegroundConst.RespawnImmediately);
			SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeHorde + 1, BattlegroundConst.RespawnImmediately);
			SpawnBGObject(EotSMisc.m_CapturingPointTypes[Point].SpawnObjectTypeHorde + 2, BattlegroundConst.RespawnImmediately);
		}

		//buff isn't respawned

		m_PointOwnedByTeam[Point] = Team;
		m_PointState[Point] = EotSPointState.UnderControl;

		if (Team == TeamFaction.Alliance)
			SendBroadcastText(EotSMisc.m_CapturingPointTypes[Point].MessageIdAlliance, ChatMsg.BgSystemAlliance, player);
		else
			SendBroadcastText(EotSMisc.m_CapturingPointTypes[Point].MessageIdHorde, ChatMsg.BgSystemHorde, player);

		if (!BgCreatures[Point].IsEmpty)
			DelCreature(Point);

		var sg = Global.ObjectMgr.GetWorldSafeLoc(EotSMisc.m_CapturingPointTypes[Point].GraveYardId);

		if (sg == null || !AddSpiritGuide(Point, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.124139f, GetTeamIndexByTeamId(Team)))
			Log.outError(LogFilter.Battleground,
						"BatteGroundEY: Failed to spawn spirit guide. point: {0}, team: {1}, graveyard_id: {2}",
						Point,
						Team,
						EotSMisc.m_CapturingPointTypes[Point].GraveYardId);

		//    SpawnBGCreature(Point, RESPAWN_IMMEDIATELY);

		UpdatePointsIcons(Team, Point);
		UpdatePointsCount(Team);

		if (Point >= EotSPoints.PointsMax)
			return;

		var trigger = GetBGCreature(Point + 6); //0-5 spirit guides

		if (!trigger)
			trigger = AddCreature(SharedConst.WorldTrigger, Point + 6, EotSMisc.TriggerPositions[Point], GetTeamIndexByTeamId(Team));

		//add bonus honor aura trigger creature when node is accupied
		//cast bonus aura (+50% honor in 25yards)
		//aura should only apply to players who have accupied the node, set correct faction for trigger
		if (trigger)
		{
			trigger.Faction = Team == TeamFaction.Alliance ? 84u : 83;
			trigger.CastSpell(trigger, BattlegroundConst.SpellHonorableDefender25y, false);
		}
	}

	void EventPlayerCapturedFlag(Player player, uint BgObjectType)
	{
		if (GetStatus() != BattlegroundStatus.InProgress || GetFlagPickerGUID() != player.GUID)
			return;

		SetFlagPicker(ObjectGuid.Empty);
		m_FlagState = EotSFlagState.WaitRespawn;
		player.RemoveAura(EotSMisc.SpellNetherstormFlag);

		player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);

		var team = GetPlayerTeam(player.GUID);

		if (team == TeamFaction.Alliance)
		{
			SendBroadcastText(EotSBroadcastTexts.AllianceCapturedFlag, ChatMsg.BgSystemAlliance, player);
			PlaySoundToAll(EotSSoundIds.FlagCapturedAlliance);
		}
		else
		{
			SendBroadcastText(EotSBroadcastTexts.HordeCapturedFlag, ChatMsg.BgSystemHorde, player);
			PlaySoundToAll(EotSSoundIds.FlagCapturedHorde);
		}

		SpawnBGObject((int)BgObjectType, BattlegroundConst.RespawnImmediately);

		m_FlagsTimer = EotSMisc.FlagRespawnTime;
		m_FlagCapturedBgObjectType = BgObjectType;

		var team_id = GetTeamIndexByTeamId(team);

		if (m_TeamPointsCount[team_id] > 0)
			AddPoints(team, EotSMisc.FlagPoints[m_TeamPointsCount[team_id] - 1]);

		UpdateWorldState(EotSWorldStateIds.NetherstormFlagStateHorde, (int)EotSFlagState.OnBase);
		UpdateWorldState(EotSWorldStateIds.NetherstormFlagStateAlliance, (int)EotSFlagState.OnBase);

		UpdatePlayerScore(player, ScoreType.FlagCaptures, 1);
	}

	void SetFlagPicker(ObjectGuid guid)
	{
		m_FlagKeeper = guid;
	}

	bool IsFlagPickedup()
	{
		return !m_FlagKeeper.IsEmpty;
	}

	ObjectGuid GetDroppedFlagGUID()
	{
		return m_DroppedFlagGUID;
	}
}

class BgEyeOfStormScore : BattlegroundScore
{
	uint FlagCaptures;
	public BgEyeOfStormScore(ObjectGuid playerGuid, TeamFaction team) : base(playerGuid, team) { }

	public override void UpdateScore(ScoreType type, uint value)
	{
		switch (type)
		{
			case ScoreType.FlagCaptures: // Flags captured
				FlagCaptures += value;

				break;
			default:
				base.UpdateScore(type, value);

				break;
		}
	}

	public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
	{
		base.BuildPvPLogPlayerDataPacket(out playerData);

		playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)EotSMisc.ObjectiveCaptureFlag, FlagCaptures));
	}

	public override uint GetAttr1()
	{
		return FlagCaptures;
	}
}

struct BattlegroundEYPointIconsStruct
{
	public BattlegroundEYPointIconsStruct(uint worldStateControlIndex, uint worldStateAllianceControlledIndex, uint worldStateHordeControlledIndex, uint worldStateAllianceStatusBarIcon, uint worldStateHordeStatusBarIcon)
	{
		WorldStateControlIndex = worldStateControlIndex;
		WorldStateAllianceControlledIndex = worldStateAllianceControlledIndex;
		WorldStateHordeControlledIndex = worldStateHordeControlledIndex;
		WorldStateAllianceStatusBarIcon = worldStateAllianceStatusBarIcon;
		WorldStateHordeStatusBarIcon = worldStateHordeStatusBarIcon;
	}

	public uint WorldStateControlIndex;
	public uint WorldStateAllianceControlledIndex;
	public uint WorldStateHordeControlledIndex;
	public uint WorldStateAllianceStatusBarIcon;
	public uint WorldStateHordeStatusBarIcon;
}

struct BattlegroundEYLosingPointStruct
{
	public BattlegroundEYLosingPointStruct(int _SpawnNeutralObjectType, int _DespawnObjectTypeAlliance, uint _MessageIdAlliance, int _DespawnObjectTypeHorde, uint _MessageIdHorde)
	{
		SpawnNeutralObjectType = _SpawnNeutralObjectType;
		DespawnObjectTypeAlliance = _DespawnObjectTypeAlliance;
		MessageIdAlliance = _MessageIdAlliance;
		DespawnObjectTypeHorde = _DespawnObjectTypeHorde;
		MessageIdHorde = _MessageIdHorde;
	}

	public int SpawnNeutralObjectType;
	public int DespawnObjectTypeAlliance;
	public uint MessageIdAlliance;
	public int DespawnObjectTypeHorde;
	public uint MessageIdHorde;
}

struct BattlegroundEYCapturingPointStruct
{
	public BattlegroundEYCapturingPointStruct(int _DespawnNeutralObjectType, int _SpawnObjectTypeAlliance, uint _MessageIdAlliance, int _SpawnObjectTypeHorde, uint _MessageIdHorde, uint _GraveYardId)
	{
		DespawnNeutralObjectType = _DespawnNeutralObjectType;
		SpawnObjectTypeAlliance = _SpawnObjectTypeAlliance;
		MessageIdAlliance = _MessageIdAlliance;
		SpawnObjectTypeHorde = _SpawnObjectTypeHorde;
		MessageIdHorde = _MessageIdHorde;
		GraveYardId = _GraveYardId;
	}

	public int DespawnNeutralObjectType;
	public int SpawnObjectTypeAlliance;
	public uint MessageIdAlliance;
	public int SpawnObjectTypeHorde;
	public uint MessageIdHorde;
	public uint GraveYardId;
}

struct EotSMisc
{
	public const uint EventStartBattle = 13180; // Achievement: Flurry
	public const int FlagRespawnTime = (8 * Time.InMilliseconds);
	public const int FPointsTickTime = (2 * Time.InMilliseconds);

	public const uint NotEYWeekendHonorTicks = 260;
	public const uint EYWeekendHonorTicks = 160;

	public const uint ObjectiveCaptureFlag = 183;

	public const uint SpellNetherstormFlag = 34976;
	public const uint SpellPlayerDroppedFlag = 34991;

	public const uint ExploitTeleportLocationAlliance = 3773;
	public const uint ExploitTeleportLocationHorde = 3772;

	public static Position[] TriggerPositions =
	{
		new(2044.28f, 1729.68f, 1189.96f, 0.017453f), // FEL_REAVER center
		new(2048.83f, 1393.65f, 1194.49f, 0.20944f),  // BLOOD_ELF center
		new(2286.56f, 1402.36f, 1197.11f, 3.72381f),  // DRAENEI_RUINS center
		new(2284.48f, 1731.23f, 1189.99f, 2.89725f)   // MAGE_TOWER center
	};

	public static byte[] TickPoints =
	{
		1, 2, 5, 10
	};

	public static uint[] FlagPoints =
	{
		75, 85, 100, 500
	};

	public static BattlegroundEYPointIconsStruct[] m_PointsIconStruct =
	{
		new(EotSWorldStateIds.FelReaverUncontrol, EotSWorldStateIds.FelReaverAllianceControl, EotSWorldStateIds.FelReaverHordeControl, EotSWorldStateIds.FelReaverAllianceControlState, EotSWorldStateIds.FelReaverHordeControlState), new(EotSWorldStateIds.BloodElfUncontrol, EotSWorldStateIds.BloodElfAllianceControl, EotSWorldStateIds.BloodElfHordeControl, EotSWorldStateIds.BloodElfAllianceControlState, EotSWorldStateIds.BloodElfHordeControlState), new(EotSWorldStateIds.DraeneiRuinsUncontrol, EotSWorldStateIds.DraeneiRuinsAllianceControl, EotSWorldStateIds.DraeneiRuinsHordeControl, EotSWorldStateIds.DraeneiRuinsAllianceControlState, EotSWorldStateIds.DraeneiRuinsHordeControlState), new(EotSWorldStateIds.MageTowerUncontrol, EotSWorldStateIds.MageTowerAllianceControl, EotSWorldStateIds.MageTowerHordeControl, EotSWorldStateIds.MageTowerAllianceControlState, EotSWorldStateIds.MageTowerHordeControlState)
	};

	public static BattlegroundEYLosingPointStruct[] m_LosingPointTypes =
	{
		new(EotSObjectTypes.NBannerFelReaverCenter, EotSObjectTypes.ABannerFelReaverCenter, EotSBroadcastTexts.AllianceLostFelReaverRuins, EotSObjectTypes.HBannerFelReaverCenter, EotSBroadcastTexts.HordeLostFelReaverRuins), new(EotSObjectTypes.NBannerBloodElfCenter, EotSObjectTypes.ABannerBloodElfCenter, EotSBroadcastTexts.AllianceLostBloodElfTower, EotSObjectTypes.HBannerBloodElfCenter, EotSBroadcastTexts.HordeLostBloodElfTower), new(EotSObjectTypes.NBannerDraeneiRuinsCenter, EotSObjectTypes.ABannerDraeneiRuinsCenter, EotSBroadcastTexts.AllianceLostDraeneiRuins, EotSObjectTypes.HBannerDraeneiRuinsCenter, EotSBroadcastTexts.HordeLostDraeneiRuins), new(EotSObjectTypes.NBannerMageTowerCenter, EotSObjectTypes.ABannerMageTowerCenter, EotSBroadcastTexts.AllianceLostMageTower, EotSObjectTypes.HBannerMageTowerCenter, EotSBroadcastTexts.HordeLostMageTower)
	};

	public static BattlegroundEYCapturingPointStruct[] m_CapturingPointTypes =
	{
		new(EotSObjectTypes.NBannerFelReaverCenter, EotSObjectTypes.ABannerFelReaverCenter, EotSBroadcastTexts.AllianceTakenFelReaverRuins, EotSObjectTypes.HBannerFelReaverCenter, EotSBroadcastTexts.HordeTakenFelReaverRuins, EotSGaveyardIds.FelReaver), new(EotSObjectTypes.NBannerBloodElfCenter, EotSObjectTypes.ABannerBloodElfCenter, EotSBroadcastTexts.AllianceTakenBloodElfTower, EotSObjectTypes.HBannerBloodElfCenter, EotSBroadcastTexts.HordeTakenBloodElfTower, EotSGaveyardIds.BloodElf), new(EotSObjectTypes.NBannerDraeneiRuinsCenter, EotSObjectTypes.ABannerDraeneiRuinsCenter, EotSBroadcastTexts.AllianceTakenDraeneiRuins, EotSObjectTypes.HBannerDraeneiRuinsCenter, EotSBroadcastTexts.HordeTakenDraeneiRuins, EotSGaveyardIds.DraeneiRuins), new(EotSObjectTypes.NBannerMageTowerCenter, EotSObjectTypes.ABannerMageTowerCenter, EotSBroadcastTexts.AllianceTakenMageTower, EotSObjectTypes.HBannerMageTowerCenter, EotSBroadcastTexts.HordeTakenMageTower, EotSGaveyardIds.MageTower)
	};
}

struct EotSBroadcastTexts
{
	public const uint AllianceTakenFelReaverRuins = 17828;
	public const uint HordeTakenFelReaverRuins = 17829;
	public const uint AllianceLostFelReaverRuins = 17835;
	public const uint HordeLostFelReaverRuins = 17836;

	public const uint AllianceTakenBloodElfTower = 17819;
	public const uint HordeTakenBloodElfTower = 17823;
	public const uint AllianceLostBloodElfTower = 17831;
	public const uint HordeLostBloodElfTower = 17832;

	public const uint AllianceTakenDraeneiRuins = 17827;
	public const uint HordeTakenDraeneiRuins = 17826;
	public const uint AllianceLostDraeneiRuins = 17833;
	public const uint HordeLostDraeneiRuins = 17834;

	public const uint AllianceTakenMageTower = 17824;
	public const uint HordeTakenMageTower = 17825;
	public const uint AllianceLostMageTower = 17837;
	public const uint HordeLostMageTower = 17838;

	public const uint TakenFlag = 18359;
	public const uint FlagDropped = 18361;
	public const uint FlagReset = 18364;
	public const uint AllianceCapturedFlag = 18375;
	public const uint HordeCapturedFlag = 18384;
}

struct EotSWorldStateIds
{
	public const uint AllianceResources = 1776;
	public const uint HordeResources = 1777;
	public const uint MaxResources = 1780;
	public const uint AllianceBase = 2752;
	public const uint HordeBase = 2753;
	public const uint DraeneiRuinsHordeControl = 2733;
	public const uint DraeneiRuinsAllianceControl = 2732;
	public const uint DraeneiRuinsUncontrol = 2731;
	public const uint MageTowerAllianceControl = 2730;
	public const uint MageTowerHordeControl = 2729;
	public const uint MageTowerUncontrol = 2728;
	public const uint FelReaverHordeControl = 2727;
	public const uint FelReaverAllianceControl = 2726;
	public const uint FelReaverUncontrol = 2725;
	public const uint BloodElfHordeControl = 2724;
	public const uint BloodElfAllianceControl = 2723;
	public const uint BloodElfUncontrol = 2722;
	public const uint ProgressBarPercentGrey = 2720; //100 = Empty (Only Grey); 0 = Blue|Red (No Grey)
	public const uint ProgressBarStatus = 2719;      //50 Init!; 48 ... Hordak Bere .. 33 .. 0 = Full 100% Hordacky; 100 = Full Alliance
	public const uint ProgressBarShow = 2718;        //1 Init; 0 Druhy Send - Bez Messagu; 1 = Controlled Aliance

	public const uint NetherstormFlag = 8863;

	//Set To 2 When Flag Is Picked Up; And To 1 If It Is Dropped
	public const uint NetherstormFlagStateAlliance = 9808;
	public const uint NetherstormFlagStateHorde = 9809;

	public const uint DraeneiRuinsHordeControlState = 17362;
	public const uint DraeneiRuinsAllianceControlState = 17366;
	public const uint MageTowerHordeControlState = 17361;
	public const uint MageTowerAllianceControlState = 17368;
	public const uint FelReaverHordeControlState = 17364;
	public const uint FelReaverAllianceControlState = 17367;
	public const uint BloodElfHordeControlState = 17363;
	public const uint BloodElfAllianceControlState = 17365;
}

enum EotSProgressBarConsts
{
	PointMaxCapturersCount = 5,
	PointRadius = 70,
	ProgressBarDontShow = 0,
	ProgressBarShow = 1,
	ProgressBarPercentGrey = 40,
	ProgressBarStateMiddle = 50,
	ProgressBarHordeControlled = 0,
	ProgressBarNeutralLow = 30,
	ProgressBarNeutralHigh = 70,
	ProgressBarAliControlled = 100
}

struct EotSSoundIds
{
	//strange ids, but sure about them
	public const uint FlagPickedUpAlliance = 8212;
	public const uint FlagCapturedHorde = 8213;
	public const uint FlagPickedUpHorde = 8174;
	public const uint FlagCapturedAlliance = 8173;
	public const uint FlagReset = 8192;
}

struct EotSObjectIds
{
	public const uint ADoorEyEntry = 184719;      //Alliance Door
	public const uint HDoorEyEntry = 184720;      //Horde Door
	public const uint Flag1EyEntry = 184493;      //Netherstorm Flag (Generic)
	public const uint Flag2EyEntry = 208977;      //Netherstorm Flag (Flagstand)
	public const uint ABannerEyEntry = 184381;    //Visual Banner (Alliance)
	public const uint HBannerEyEntry = 184380;    //Visual Banner (Horde)
	public const uint NBannerEyEntry = 184382;    //Visual Banner (Neutral)
	public const uint BeTowerCapEyEntry = 184080; //Be Tower Cap Pt
	public const uint FrTowerCapEyEntry = 184081; //Fel Reaver Cap Pt
	public const uint HuTowerCapEyEntry = 184082; //Human Tower Cap Pt
	public const uint DrTowerCapEyEntry = 184083; //Draenei Tower Cap Pt
	public const uint SpeedBuffFelReaverEyEntry = 184970;
	public const uint RestorationBuffFelReaverEyEntry = 184971;
	public const uint BerserkBuffFelReaverEyEntry = 184972;
	public const uint SpeedBuffBloodElfEyEntry = 184964;
	public const uint RestorationBuffBloodElfEyEntry = 184965;
	public const uint BerserkBuffBloodElfEyEntry = 184966;
	public const uint SpeedBuffDraeneiRuinsEyEntry = 184976;
	public const uint RestorationBuffDraeneiRuinsEyEntry = 184977;
	public const uint BerserkBuffDraeneiRuinsEyEntry = 184978;
	public const uint SpeedBuffMageTowerEyEntry = 184973;
	public const uint RestorationBuffMageTowerEyEntry = 184974;
	public const uint BerserkBuffMageTowerEyEntry = 184975;
}

struct EotSPointsTrigger
{
	public const uint BloodElfPoint = 4476;
	public const uint FelReaverPoint = 4514;
	public const uint MageTowerPoint = 4516;
	public const uint DraeneiRuinsPoint = 4518;
	public const uint BloodElfBuff = 4568;
	public const uint FelReaverBuff = 4569;
	public const uint MageTowerBuff = 4570;
	public const uint DraeneiRuinsBuff = 4571;
}

struct EotSGaveyardIds
{
	public const int MainAlliance = 1103;
	public const uint MainHorde = 1104;
	public const uint FelReaver = 1105;
	public const uint BloodElf = 1106;
	public const uint DraeneiRuins = 1107;
	public const uint MageTower = 1108;
}

struct EotSPoints
{
	public const int FelReaver = 0;
	public const int BloodElf = 1;
	public const int DraeneiRuins = 2;
	public const int MageTower = 3;

	public const int PlayersOutOfPoints = 4;
	public const int PointsMax = 4;
}

struct EotSCreaturesTypes
{
	public const uint SpiritFelReaver = 0;
	public const uint SpiritBloodElf = 1;
	public const uint SpiritDraeneiRuins = 2;
	public const uint SpiritMageTower = 3;
	public const int SpiritMainAlliance = 4;
	public const int SpiritMainHorde = 5;

	public const uint TriggerFelReaver = 6;
	public const uint TriggerBloodElf = 7;
	public const uint TriggerDraeneiRuins = 8;
	public const uint TriggerMageTower = 9;

	public const uint Max = 10;
}

struct EotSObjectTypes
{
	public const int DoorA = 0;
	public const int DoorH = 1;
	public const int ABannerFelReaverCenter = 2;
	public const int ABannerFelReaverLeft = 3;
	public const int ABannerFelReaverRight = 4;
	public const int ABannerBloodElfCenter = 5;
	public const int ABannerBloodElfLeft = 6;
	public const int ABannerBloodElfRight = 7;
	public const int ABannerDraeneiRuinsCenter = 8;
	public const int ABannerDraeneiRuinsLeft = 9;
	public const int ABannerDraeneiRuinsRight = 10;
	public const int ABannerMageTowerCenter = 11;
	public const int ABannerMageTowerLeft = 12;
	public const int ABannerMageTowerRight = 13;
	public const int HBannerFelReaverCenter = 14;
	public const int HBannerFelReaverLeft = 15;
	public const int HBannerFelReaverRight = 16;
	public const int HBannerBloodElfCenter = 17;
	public const int HBannerBloodElfLeft = 18;
	public const int HBannerBloodElfRight = 19;
	public const int HBannerDraeneiRuinsCenter = 20;
	public const int HBannerDraeneiRuinsLeft = 21;
	public const int HBannerDraeneiRuinsRight = 22;
	public const int HBannerMageTowerCenter = 23;
	public const int HBannerMageTowerLeft = 24;
	public const int HBannerMageTowerRight = 25;
	public const int NBannerFelReaverCenter = 26;
	public const int NBannerFelReaverLeft = 27;
	public const int NBannerFelReaverRight = 28;
	public const int NBannerBloodElfCenter = 29;
	public const int NBannerBloodElfLeft = 30;
	public const int NBannerBloodElfRight = 31;
	public const int NBannerDraeneiRuinsCenter = 32;
	public const int NBannerDraeneiRuinsLeft = 33;
	public const int NBannerDraeneiRuinsRight = 34;
	public const int NBannerMageTowerCenter = 35;
	public const int NBannerMageTowerLeft = 36;
	public const int NBannerMageTowerRight = 37;
	public const int TowerCapFelReaver = 38;
	public const int TowerCapBloodElf = 39;
	public const int TowerCapDraeneiRuins = 40;
	public const int TowerCapMageTower = 41;
	public const int FlagNetherstorm = 42;
	public const int FlagFelReaver = 43;
	public const int FlagBloodElf = 44;
	public const int FlagDraeneiRuins = 45;

	public const int FlagMageTower = 46;

	//Buffs
	public const int SpeedbuffFelReaver = 47;
	public const int RegenbuffFelReaver = 48;
	public const int BerserkbuffFelReaver = 49;
	public const int SpeedbuffBloodElf = 50;
	public const int RegenbuffBloodElf = 51;
	public const int BerserkbuffBloodElf = 52;
	public const int SpeedbuffDraeneiRuins = 53;
	public const int RegenbuffDraeneiRuins = 54;
	public const int BerserkbuffDraeneiRuins = 55;
	public const int SpeedbuffMageTower = 56;
	public const int RegenbuffMageTower = 57;
	public const int BerserkbuffMageTower = 58;
	public const int Max = 59;
}

struct EotSScoreIds
{
	public const uint WarningNearVictoryScore = 1400;
	public const uint MaxTeamScore = 1500;
}

enum EotSFlagState
{
	OnBase = 0,
	WaitRespawn = 1,
	OnPlayer = 2,
	OnGround = 3
}

enum EotSPointState
{
	NoOwner = 0,
	Uncontrolled = 0,
	UnderControl = 3
}