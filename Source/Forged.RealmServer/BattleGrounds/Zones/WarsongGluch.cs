// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer.BattleGrounds.Zones;

class BgWarsongGluch : Battleground
{
	const uint ExploitTeleportLocationAlliance = 3784;
	const uint ExploitTeleportLocationHorde = 3785;

	readonly ObjectGuid[] m_FlagKeepers = new ObjectGuid[2]; // 0 - alliance, 1 - horde
	readonly ObjectGuid[] m_DroppedFlagGUID = new ObjectGuid[2];
	readonly WSGFlagState[] _flagState = new WSGFlagState[2]; // for checking flag state
	readonly int[] _flagsTimer = new int[2];
	readonly int[] _flagsDropTimer = new int[2];

	readonly uint[][] Honor =
	{
		new uint[]
		{
			20, 40, 40
		}, // normal honor
		new uint[]
		{
			60, 40, 80
		} // holiday
	};

	uint _lastFlagCaptureTeam; // Winner is based on this if score is equal

	uint m_ReputationCapture;
	uint m_HonorWinKills;
	uint m_HonorEndKills;
	int _flagSpellForceTimer;
	bool _bothFlagsKept;
	byte _flagDebuffState; // 0 - no debuffs, 1 - focused assault, 2 - brutal assault

	public BgWarsongGluch(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
	{
		BgObjects = new ObjectGuid[WSGObjectTypes.Max];
		BgCreatures = new ObjectGuid[WSGCreatureTypes.Max];

		StartMessageIds[BattlegroundConst.EventIdSecond] = WSGBroadcastTexts.StartOneMinute;
		StartMessageIds[BattlegroundConst.EventIdThird] = WSGBroadcastTexts.StartHalfMinute;
		StartMessageIds[BattlegroundConst.EventIdFourth] = WSGBroadcastTexts.BattleHasBegun;
	}

	public override void PostUpdateImpl(uint diff)
	{
		if (GetStatus() == BattlegroundStatus.InProgress)
		{
			if (GetElapsedTime() >= 17 * Time.Minute * Time.InMilliseconds)
			{
				if (GetTeamScore(TeamIds.Alliance) == 0)
				{
					if (GetTeamScore(TeamIds.Horde) == 0) // No one scored - result is tie
						EndBattleground(TeamFaction.Other);
					else // Horde has more points and thus wins
						EndBattleground(TeamFaction.Horde);
				}
				else if (GetTeamScore(TeamIds.Horde) == 0)
				{
					EndBattleground(TeamFaction.Alliance); // Alliance has > 0, Horde has 0, alliance wins
				}
				else if (GetTeamScore(TeamIds.Horde) == GetTeamScore(TeamIds.Alliance)) // Team score equal, winner is team that scored the last flag
				{
					EndBattleground((TeamFaction)_lastFlagCaptureTeam);
				}
				else if (GetTeamScore(TeamIds.Horde) > GetTeamScore(TeamIds.Alliance)) // Last but not least, check who has the higher score
				{
					EndBattleground(TeamFaction.Horde);
				}
				else
				{
					EndBattleground(TeamFaction.Alliance);
				}
			}

			if (_flagState[TeamIds.Alliance] == WSGFlagState.WaitRespawn)
			{
				_flagsTimer[TeamIds.Alliance] -= (int)diff;

				if (_flagsTimer[TeamIds.Alliance] < 0)
				{
					_flagsTimer[TeamIds.Alliance] = 0;
					RespawnFlag(TeamFaction.Alliance, true);
				}
			}

			if (_flagState[TeamIds.Alliance] == WSGFlagState.OnGround)
			{
				_flagsDropTimer[TeamIds.Alliance] -= (int)diff;

				if (_flagsDropTimer[TeamIds.Alliance] < 0)
				{
					_flagsDropTimer[TeamIds.Alliance] = 0;
					RespawnFlagAfterDrop(TeamFaction.Alliance);
					_bothFlagsKept = false;
				}
			}

			if (_flagState[TeamIds.Horde] == WSGFlagState.WaitRespawn)
			{
				_flagsTimer[TeamIds.Horde] -= (int)diff;

				if (_flagsTimer[TeamIds.Horde] < 0)
				{
					_flagsTimer[TeamIds.Horde] = 0;
					RespawnFlag(TeamFaction.Horde, true);
				}
			}

			if (_flagState[TeamIds.Horde] == WSGFlagState.OnGround)
			{
				_flagsDropTimer[TeamIds.Horde] -= (int)diff;

				if (_flagsDropTimer[TeamIds.Horde] < 0)
				{
					_flagsDropTimer[TeamIds.Horde] = 0;
					RespawnFlagAfterDrop(TeamFaction.Horde);
					_bothFlagsKept = false;
				}
			}

			if (_bothFlagsKept)
			{
				_flagSpellForceTimer += (int)diff;

				if (_flagDebuffState == 0 && _flagSpellForceTimer >= 10 * Time.Minute * Time.InMilliseconds) //10 minutes
				{
					// Apply Stage 1 (Focused Assault)
					var player = _objectAccessor.FindPlayer(m_FlagKeepers[0]);

					if (player)
						player.CastSpell(player, WSGSpellId.FocusedAssault, true);

					player = _objectAccessor.FindPlayer(m_FlagKeepers[1]);

					if (player)
						player.CastSpell(player, WSGSpellId.FocusedAssault, true);

					_flagDebuffState = 1;
				}
				else if (_flagDebuffState == 1 && _flagSpellForceTimer >= 900000) //15 minutes
				{
					// Apply Stage 2 (Brutal Assault)
					var player = _objectAccessor.FindPlayer(m_FlagKeepers[0]);

					if (player)
					{
						player.RemoveAura(WSGSpellId.FocusedAssault);
						player.CastSpell(player, WSGSpellId.BrutalAssault, true);
					}

					player = _objectAccessor.FindPlayer(m_FlagKeepers[1]);

					if (player)
					{
						player.RemoveAura(WSGSpellId.FocusedAssault);
						player.CastSpell(player, WSGSpellId.BrutalAssault, true);
					}

					_flagDebuffState = 2;
				}
			}
			else if ((_flagState[TeamIds.Alliance] == WSGFlagState.OnBase || _flagState[TeamIds.Alliance] == WSGFlagState.WaitRespawn) &&
					(_flagState[TeamIds.Horde] == WSGFlagState.OnBase || _flagState[TeamIds.Horde] == WSGFlagState.WaitRespawn))
			{
				// Both flags are in base or awaiting respawn.
				// Remove assault debuffs, reset timers

				var player = _objectAccessor.FindPlayer(m_FlagKeepers[0]);

				if (player)
				{
					player.RemoveAura(WSGSpellId.FocusedAssault);
					player.RemoveAura(WSGSpellId.BrutalAssault);
				}

				player = _objectAccessor.FindPlayer(m_FlagKeepers[1]);

				if (player)
				{
					player.RemoveAura(WSGSpellId.FocusedAssault);
					player.RemoveAura(WSGSpellId.BrutalAssault);
				}

				_flagSpellForceTimer = 0; //reset timer.
				_flagDebuffState = 0;
			}
		}
	}

	public override void StartingEventCloseDoors()
	{
		for (var i = WSGObjectTypes.DoorA1; i <= WSGObjectTypes.DoorH4; ++i)
		{
			DoorClose(i);
			SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
		}

		for (var i = WSGObjectTypes.AFlag; i <= WSGObjectTypes.Berserkbuff2; ++i)
			SpawnBGObject(i, BattlegroundConst.RespawnOneDay);
	}

	public override void StartingEventOpenDoors()
	{
		for (var i = WSGObjectTypes.DoorA1; i <= WSGObjectTypes.DoorA6; ++i)
			DoorOpen(i);

		for (var i = WSGObjectTypes.DoorH1; i <= WSGObjectTypes.DoorH4; ++i)
			DoorOpen(i);

		for (var i = WSGObjectTypes.AFlag; i <= WSGObjectTypes.Berserkbuff2; ++i)
			SpawnBGObject(i, BattlegroundConst.RespawnImmediately);

		SpawnBGObject(WSGObjectTypes.DoorA5, BattlegroundConst.RespawnOneDay);
		SpawnBGObject(WSGObjectTypes.DoorA6, BattlegroundConst.RespawnOneDay);
		SpawnBGObject(WSGObjectTypes.DoorH3, BattlegroundConst.RespawnOneDay);
		SpawnBGObject(WSGObjectTypes.DoorH4, BattlegroundConst.RespawnOneDay);

		UpdateWorldState(WSGWorldStates.StateTimerActive, 1);
		UpdateWorldState(WSGWorldStates.StateTimer, (int)(_gameTime.CurrentGameTime + 15 * Time.Minute));

		// players joining later are not eligibles
		TriggerGameEvent(8563);
	}

	public override void AddPlayer(Player player)
	{
		var isInBattleground = IsPlayerInBattleground(player.GUID);
		base.AddPlayer(player);

		if (!isInBattleground)
			PlayerScores[player.GUID] = new BattlegroundWGScore(player.GUID, player.GetBgTeam());
	}

	public override void EventPlayerDroppedFlag(Player player)
	{
		var team = GetPlayerTeam(player.GUID);

		if (GetStatus() != BattlegroundStatus.InProgress)
		{
			// if not running, do not cast things at the dropper player (prevent spawning the "dropped" flag), neither send unnecessary messages
			// just take off the aura
			if (team == TeamFaction.Alliance)
			{
				if (!IsHordeFlagPickedup())
					return;

				if (GetFlagPickerGUID(TeamIds.Horde) == player.GUID)
				{
					SetHordeFlagPicker(ObjectGuid.Empty);
					player.RemoveAura(WSGSpellId.WarsongFlag);
				}
			}
			else
			{
				if (!IsAllianceFlagPickedup())
					return;

				if (GetFlagPickerGUID(TeamIds.Alliance) == player.GUID)
				{
					SetAllianceFlagPicker(ObjectGuid.Empty);
					player.RemoveAura(WSGSpellId.SilverwingFlag);
				}
			}

			return;
		}

		var set = false;

		if (team == TeamFaction.Alliance)
		{
			if (!IsHordeFlagPickedup())
				return;

			if (GetFlagPickerGUID(TeamIds.Horde) == player.GUID)
			{
				SetHordeFlagPicker(ObjectGuid.Empty);
				player.RemoveAura(WSGSpellId.WarsongFlag);

				if (_flagDebuffState == 1)
					player.RemoveAura(WSGSpellId.FocusedAssault);
				else if (_flagDebuffState == 2)
					player.RemoveAura(WSGSpellId.BrutalAssault);

				_flagState[TeamIds.Horde] = WSGFlagState.OnGround;
				player.CastSpell(player, WSGSpellId.WarsongFlagDropped, true);
				set = true;
			}
		}
		else
		{
			if (!IsAllianceFlagPickedup())
				return;

			if (GetFlagPickerGUID(TeamIds.Alliance) == player.GUID)
			{
				SetAllianceFlagPicker(ObjectGuid.Empty);
				player.RemoveAura(WSGSpellId.SilverwingFlag);

				if (_flagDebuffState == 1)
					player.RemoveAura(WSGSpellId.FocusedAssault);
				else if (_flagDebuffState == 2)
					player.RemoveAura(WSGSpellId.BrutalAssault);

				_flagState[TeamIds.Alliance] = WSGFlagState.OnGround;
				player.CastSpell(player, WSGSpellId.SilverwingFlagDropped, true);
				set = true;
			}
		}

		if (set)
		{
			player.CastSpell(player, BattlegroundConst.SpellRecentlyDroppedFlag, true);
			UpdateFlagState(team, WSGFlagState.OnGround);

			if (team == TeamFaction.Alliance)
				SendBroadcastText(WSGBroadcastTexts.HordeFlagDropped, ChatMsg.BgSystemHorde, player);
			else
				SendBroadcastText(WSGBroadcastTexts.AllianceFlagDropped, ChatMsg.BgSystemAlliance, player);

			_flagsDropTimer[GetTeamIndexByTeamId(GetOtherTeam(team))] = WSGTimerOrScore.FlagDropTime;
		}
	}

	public override void EventPlayerClickedOnFlag(Player player, GameObject target_obj)
	{
		if (GetStatus() != BattlegroundStatus.InProgress)
			return;

		var team = GetPlayerTeam(player.GUID);

		//alliance flag picked up from base
		if (team == TeamFaction.Horde && GetFlagState(TeamFaction.Alliance) == WSGFlagState.OnBase && BgObjects[WSGObjectTypes.AFlag] == target_obj.GUID)
		{
			SendBroadcastText(WSGBroadcastTexts.AllianceFlagPickedUp, ChatMsg.BgSystemHorde, player);
			PlaySoundToAll(WSGSound.AllianceFlagPickedUp);
			SpawnBGObject(WSGObjectTypes.AFlag, BattlegroundConst.RespawnOneDay);
			SetAllianceFlagPicker(player.GUID);
			_flagState[TeamIds.Alliance] = WSGFlagState.OnPlayer;
			//update world state to show correct flag carrier
			UpdateFlagState(TeamFaction.Horde, WSGFlagState.OnPlayer);
			player.CastSpell(player, WSGSpellId.SilverwingFlag, true);
			player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, WSGSpellId.SilverwingFlagPicked);

			if (_flagState[1] == WSGFlagState.OnPlayer)
				_bothFlagsKept = true;

			if (_flagDebuffState == 1)
				player.CastSpell(player, WSGSpellId.FocusedAssault, true);
			else if (_flagDebuffState == 2)
				player.CastSpell(player, WSGSpellId.BrutalAssault, true);
		}

		//horde flag picked up from base
		if (team == TeamFaction.Alliance && GetFlagState(TeamFaction.Horde) == WSGFlagState.OnBase && BgObjects[WSGObjectTypes.HFlag] == target_obj.GUID)
		{
			SendBroadcastText(WSGBroadcastTexts.HordeFlagPickedUp, ChatMsg.BgSystemAlliance, player);
			PlaySoundToAll(WSGSound.HordeFlagPickedUp);
			SpawnBGObject(WSGObjectTypes.HFlag, BattlegroundConst.RespawnOneDay);
			SetHordeFlagPicker(player.GUID);
			_flagState[TeamIds.Horde] = WSGFlagState.OnPlayer;
			//update world state to show correct flag carrier
			UpdateFlagState(TeamFaction.Alliance, WSGFlagState.OnPlayer);
			player.CastSpell(player, WSGSpellId.WarsongFlag, true);
			player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, WSGSpellId.WarsongFlagPicked);

			if (_flagState[0] == WSGFlagState.OnPlayer)
				_bothFlagsKept = true;

			if (_flagDebuffState == 1)
				player.CastSpell(player, WSGSpellId.FocusedAssault, true);
			else if (_flagDebuffState == 2)
				player.CastSpell(player, WSGSpellId.BrutalAssault, true);
		}

		//Alliance flag on ground(not in base) (returned or picked up again from ground!)
		if (GetFlagState(TeamFaction.Alliance) == WSGFlagState.OnGround && player.IsWithinDistInMap(target_obj, 10) && target_obj.Template.entry == WSGObjectEntry.AFlagGround)
		{
			if (team == TeamFaction.Alliance)
			{
				SendBroadcastText(WSGBroadcastTexts.AllianceFlagReturned, ChatMsg.BgSystemAlliance, player);
				UpdateFlagState(TeamFaction.Horde, WSGFlagState.WaitRespawn);
				RespawnFlag(TeamFaction.Alliance, false);
				SpawnBGObject(WSGObjectTypes.AFlag, BattlegroundConst.RespawnImmediately);
				PlaySoundToAll(WSGSound.FlagReturned);
				UpdatePlayerScore(player, ScoreType.FlagReturns, 1);
				_bothFlagsKept = false;

				HandleFlagRoomCapturePoint(TeamIds.Horde); // Check Horde flag if it is in capture zone; if so, capture it
			}
			else
			{
				SendBroadcastText(WSGBroadcastTexts.AllianceFlagPickedUp, ChatMsg.BgSystemHorde, player);
				PlaySoundToAll(WSGSound.AllianceFlagPickedUp);
				SpawnBGObject(WSGObjectTypes.AFlag, BattlegroundConst.RespawnOneDay);
				SetAllianceFlagPicker(player.GUID);
				player.CastSpell(player, WSGSpellId.SilverwingFlag, true);
				_flagState[TeamIds.Alliance] = WSGFlagState.OnPlayer;
				UpdateFlagState(TeamFaction.Horde, WSGFlagState.OnPlayer);

				if (_flagDebuffState == 1)
					player.CastSpell(player, WSGSpellId.FocusedAssault, true);
				else if (_flagDebuffState == 2)
					player.CastSpell(player, WSGSpellId.BrutalAssault, true);
			}
			//called in HandleGameObjectUseOpcode:
			//target_obj.Delete();
		}

		//Horde flag on ground(not in base) (returned or picked up again)
		if (GetFlagState(TeamFaction.Horde) == WSGFlagState.OnGround && player.IsWithinDistInMap(target_obj, 10) && target_obj.Template.entry == WSGObjectEntry.HFlagGround)
		{
			if (team == TeamFaction.Horde)
			{
				SendBroadcastText(WSGBroadcastTexts.HordeFlagReturned, ChatMsg.BgSystemHorde, player);
				UpdateFlagState(TeamFaction.Alliance, WSGFlagState.WaitRespawn);
				RespawnFlag(TeamFaction.Horde, false);
				SpawnBGObject(WSGObjectTypes.HFlag, BattlegroundConst.RespawnImmediately);
				PlaySoundToAll(WSGSound.FlagReturned);
				UpdatePlayerScore(player, ScoreType.FlagReturns, 1);
				_bothFlagsKept = false;

				HandleFlagRoomCapturePoint(TeamIds.Alliance); // Check Alliance flag if it is in capture zone; if so, capture it
			}
			else
			{
				SendBroadcastText(WSGBroadcastTexts.HordeFlagPickedUp, ChatMsg.BgSystemAlliance, player);
				PlaySoundToAll(WSGSound.HordeFlagPickedUp);
				SpawnBGObject(WSGObjectTypes.HFlag, BattlegroundConst.RespawnOneDay);
				SetHordeFlagPicker(player.GUID);
				player.CastSpell(player, WSGSpellId.WarsongFlag, true);
				_flagState[TeamIds.Horde] = WSGFlagState.OnPlayer;
				UpdateFlagState(TeamFaction.Alliance, WSGFlagState.OnPlayer);

				if (_flagDebuffState == 1)
					player.CastSpell(player, WSGSpellId.FocusedAssault, true);
				else if (_flagDebuffState == 2)
					player.CastSpell(player, WSGSpellId.BrutalAssault, true);
			}
			//called in HandleGameObjectUseOpcode:
			//target_obj.Delete();
		}

		player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
	}

	public override void RemovePlayer(Player player, ObjectGuid guid, TeamFaction team)
	{
		// sometimes flag aura not removed :(
		if (IsAllianceFlagPickedup() && m_FlagKeepers[TeamIds.Alliance] == guid)
		{
			if (!player)
			{
				Log.Logger.Error("BattlegroundWS: Removing offline player who has the FLAG!!");
				SetAllianceFlagPicker(ObjectGuid.Empty);
				RespawnFlag(TeamFaction.Alliance, false);
			}
			else
			{
				EventPlayerDroppedFlag(player);
			}
		}

		if (IsHordeFlagPickedup() && m_FlagKeepers[TeamIds.Horde] == guid)
		{
			if (!player)
			{
				Log.Logger.Error("BattlegroundWS: Removing offline player who has the FLAG!!");
				SetHordeFlagPicker(ObjectGuid.Empty);
				RespawnFlag(TeamFaction.Horde, false);
			}
			else
			{
				EventPlayerDroppedFlag(player);
			}
		}
	}

	public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
	{
		//uint SpellId = 0;
		//uint64 buff_guid = 0;
		switch (trigger)
		{
			case 8965: // Horde Start
			case 8966: // Alliance Start
				if (GetStatus() == BattlegroundStatus.WaitJoin && !entered)
					TeleportPlayerToExploitLocation(player);

				break;
			case 3686: // Alliance elixir of speed spawn. Trigger not working, because located inside other areatrigger, can be replaced by IsWithinDist(object, dist) in Battleground.Update().
				//buff_guid = BgObjects[BG_WS_OBJECT_SPEEDBUFF_1];
				break;
			case 3687: // Horde elixir of speed spawn. Trigger not working, because located inside other areatrigger, can be replaced by IsWithinDist(object, dist) in Battleground.Update().
				//buff_guid = BgObjects[BG_WS_OBJECT_SPEEDBUFF_2];
				break;
			case 3706: // Alliance elixir of regeneration spawn
				//buff_guid = BgObjects[BG_WS_OBJECT_REGENBUFF_1];
				break;
			case 3708: // Horde elixir of regeneration spawn
				//buff_guid = BgObjects[BG_WS_OBJECT_REGENBUFF_2];
				break;
			case 3707: // Alliance elixir of berserk spawn
				//buff_guid = BgObjects[BG_WS_OBJECT_BERSERKBUFF_1];
				break;
			case 3709: // Horde elixir of berserk spawn
				//buff_guid = BgObjects[BG_WS_OBJECT_BERSERKBUFF_2];
				break;
			case 3646: // Alliance Flag spawn
				if (_flagState[TeamIds.Horde] != 0 && _flagState[TeamIds.Alliance] == 0)
					if (GetFlagPickerGUID(TeamIds.Horde) == player.GUID)
						EventPlayerCapturedFlag(player);

				break;
			case 3647: // Horde Flag spawn
				if (_flagState[TeamIds.Alliance] != 0 && _flagState[TeamIds.Horde] == 0)
					if (GetFlagPickerGUID(TeamIds.Alliance) == player.GUID)
						EventPlayerCapturedFlag(player);

				break;
			case 3649: // unk1
			case 3688: // unk2
			case 4628: // unk3
			case 4629: // unk4
				break;
			default:
				base.HandleAreaTrigger(player, trigger, entered);

				break;
		}

		//if (buff_guid)
		//    HandleTriggerBuff(buff_guid, player);
	}

	public override bool SetupBattleground()
	{
		var result = true;
		result &= AddObject(WSGObjectTypes.AFlag, WSGObjectEntry.AFlag, 1540.423f, 1481.325f, 351.8284f, 3.089233f, 0, 0, 0.9996573f, 0.02617699f, WSGTimerOrScore.FlagRespawnTime / 1000);
		result &= AddObject(WSGObjectTypes.HFlag, WSGObjectEntry.HFlag, 916.0226f, 1434.405f, 345.413f, 0.01745329f, 0, 0, 0.008726535f, 0.9999619f, WSGTimerOrScore.FlagRespawnTime / 1000);

		if (!result)
		{
			Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn flag object!");

			return false;
		}

		// buffs
		result &= AddObject(WSGObjectTypes.Speedbuff1, Buff_Entries[0], 1449.93f, 1470.71f, 342.6346f, -1.64061f, 0, 0, 0.7313537f, -0.6819983f, BattlegroundConst.BuffRespawnTime);
		result &= AddObject(WSGObjectTypes.Speedbuff2, Buff_Entries[0], 1005.171f, 1447.946f, 335.9032f, 1.64061f, 0, 0, 0.7313537f, 0.6819984f, BattlegroundConst.BuffRespawnTime);
		result &= AddObject(WSGObjectTypes.Regenbuff1, Buff_Entries[1], 1317.506f, 1550.851f, 313.2344f, -0.2617996f, 0, 0, 0.1305263f, -0.9914448f, BattlegroundConst.BuffRespawnTime);
		result &= AddObject(WSGObjectTypes.Regenbuff2, Buff_Entries[1], 1110.451f, 1353.656f, 316.5181f, -0.6806787f, 0, 0, 0.333807f, -0.9426414f, BattlegroundConst.BuffRespawnTime);
		result &= AddObject(WSGObjectTypes.Berserkbuff1, Buff_Entries[2], 1320.09f, 1378.79f, 314.7532f, 1.186824f, 0, 0, 0.5591929f, 0.8290376f, BattlegroundConst.BuffRespawnTime);
		result &= AddObject(WSGObjectTypes.Berserkbuff2, Buff_Entries[2], 1139.688f, 1560.288f, 306.8432f, -2.443461f, 0, 0, 0.9396926f, -0.3420201f, BattlegroundConst.BuffRespawnTime);

		if (!result)
		{
			Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn buff object!");

			return false;
		}

		// alliance gates
		result &= AddObject(WSGObjectTypes.DoorA1, WSGObjectEntry.DoorA1, 1503.335f, 1493.466f, 352.1888f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
		result &= AddObject(WSGObjectTypes.DoorA2, WSGObjectEntry.DoorA2, 1492.478f, 1457.912f, 342.9689f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
		result &= AddObject(WSGObjectTypes.DoorA3, WSGObjectEntry.DoorA3, 1468.503f, 1494.357f, 351.8618f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
		result &= AddObject(WSGObjectTypes.DoorA4, WSGObjectEntry.DoorA4, 1471.555f, 1458.778f, 362.6332f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f, BattlegroundConst.RespawnImmediately);
		result &= AddObject(WSGObjectTypes.DoorA5, WSGObjectEntry.DoorA5, 1492.347f, 1458.34f, 342.3712f, -0.03490669f, 0, 0, 0.01745246f, -0.9998477f, BattlegroundConst.RespawnImmediately);
		result &= AddObject(WSGObjectTypes.DoorA6, WSGObjectEntry.DoorA6, 1503.466f, 1493.367f, 351.7352f, -0.03490669f, 0, 0, 0.01745246f, -0.9998477f, BattlegroundConst.RespawnImmediately);
		// horde gates
		result &= AddObject(WSGObjectTypes.DoorH1, WSGObjectEntry.DoorH1, 949.1663f, 1423.772f, 345.6241f, -0.5756807f, -0.01673368f, -0.004956111f, -0.2839723f, 0.9586737f, BattlegroundConst.RespawnImmediately);
		result &= AddObject(WSGObjectTypes.DoorH2, WSGObjectEntry.DoorH2, 953.0507f, 1459.842f, 340.6526f, -1.99662f, -0.1971825f, 0.1575096f, -0.8239487f, 0.5073641f, BattlegroundConst.RespawnImmediately);
		result &= AddObject(WSGObjectTypes.DoorH3, WSGObjectEntry.DoorH3, 949.9523f, 1422.751f, 344.9273f, 0.0f, 0, 0, 0, 1, BattlegroundConst.RespawnImmediately);
		result &= AddObject(WSGObjectTypes.DoorH4, WSGObjectEntry.DoorH4, 950.7952f, 1459.583f, 342.1523f, 0.05235988f, 0, 0, 0.02617695f, 0.9996573f, BattlegroundConst.RespawnImmediately);

		if (!result)
		{
			Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn door object Battleground not created!");

			return false;
		}

		var sg = _gameObjectManager.GetWorldSafeLoc(WSGGraveyards.MainAlliance);

		if (sg == null || !AddSpiritGuide(WSGCreatureTypes.SpiritMainAlliance, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.124139f, TeamIds.Alliance))
		{
			Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn Alliance spirit guide! Battleground not created!");

			return false;
		}

		sg = _gameObjectManager.GetWorldSafeLoc(WSGGraveyards.MainHorde);

		if (sg == null || !AddSpiritGuide(WSGCreatureTypes.SpiritMainHorde, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.193953f, TeamIds.Horde))
		{
			Log.outError(LogFilter.Sql, "BgWarsongGluch: Failed to spawn Horde spirit guide! Battleground not created!");

			return false;
		}

		return true;
	}

	public override void Reset()
	{
		//call parent's class reset
		base.Reset();

		m_FlagKeepers[TeamIds.Alliance].Clear();
		m_FlagKeepers[TeamIds.Horde].Clear();
		m_DroppedFlagGUID[TeamIds.Alliance] = ObjectGuid.Empty;
		m_DroppedFlagGUID[TeamIds.Horde] = ObjectGuid.Empty;
		_flagState[TeamIds.Alliance] = WSGFlagState.OnBase;
		_flagState[TeamIds.Horde] = WSGFlagState.OnBase;
		m_TeamScores[TeamIds.Alliance] = 0;
		m_TeamScores[TeamIds.Horde] = 0;

		if (Global.BattlegroundMgr.IsBGWeekend(GetTypeID()))
		{
			m_ReputationCapture = 45;
			m_HonorWinKills = 3;
			m_HonorEndKills = 4;
		}
		else
		{
			m_ReputationCapture = 35;
			m_HonorWinKills = 1;
			m_HonorEndKills = 2;
		}

		_lastFlagCaptureTeam = 0;
		_bothFlagsKept = false;
		_flagDebuffState = 0;
		_flagSpellForceTimer = 0;
		_flagsDropTimer[TeamIds.Alliance] = 0;
		_flagsDropTimer[TeamIds.Horde] = 0;
		_flagsTimer[TeamIds.Alliance] = 0;
		_flagsTimer[TeamIds.Horde] = 0;
	}

	public override void EndBattleground(TeamFaction winner)
	{
		// Win reward
		if (winner == TeamFaction.Alliance)
			RewardHonorToTeam(GetBonusHonorFromKill(m_HonorWinKills), TeamFaction.Alliance);

		if (winner == TeamFaction.Horde)
			RewardHonorToTeam(GetBonusHonorFromKill(m_HonorWinKills), TeamFaction.Horde);

		// Complete map_end rewards (even if no team wins)
		RewardHonorToTeam(GetBonusHonorFromKill(m_HonorEndKills), TeamFaction.Alliance);
		RewardHonorToTeam(GetBonusHonorFromKill(m_HonorEndKills), TeamFaction.Horde);

		base.EndBattleground(winner);
	}

	public override void HandleKillPlayer(Player victim, Player killer)
	{
		if (GetStatus() != BattlegroundStatus.InProgress)
			return;

		EventPlayerDroppedFlag(victim);

		base.HandleKillPlayer(victim, killer);
	}

	public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
	{
		if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
			return false;

		switch (type)
		{
			case ScoreType.FlagCaptures: // flags captured
				player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, WSObjectives.CaptureFlag);

				break;
			case ScoreType.FlagReturns: // flags returned
				player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, WSObjectives.ReturnFlag);

				break;
			default:
				break;
		}

		return true;
	}

	public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
	{
		//if status in progress, it returns main graveyards with spiritguides
		//else it will return the graveyard in the flagroom - this is especially good
		//if a player dies in preparation phase - then the player can't cheat
		//and teleport to the graveyard outside the flagroom
		//and start running around, while the doors are still closed
		if (GetPlayerTeam(player.GUID) == TeamFaction.Alliance)
		{
			if (GetStatus() == BattlegroundStatus.InProgress)
				return _gameObjectManager.GetWorldSafeLoc(WSGGraveyards.MainAlliance);
			else
				return _gameObjectManager.GetWorldSafeLoc(WSGGraveyards.FlagRoomAlliance);
		}
		else
		{
			if (GetStatus() == BattlegroundStatus.InProgress)
				return _gameObjectManager.GetWorldSafeLoc(WSGGraveyards.MainHorde);
			else
				return _gameObjectManager.GetWorldSafeLoc(WSGGraveyards.FlagRoomHorde);
		}
	}

	public override WorldSafeLocsEntry GetExploitTeleportLocation(TeamFaction team)
	{
		return _gameObjectManager.GetWorldSafeLoc(team == TeamFaction.Alliance ? ExploitTeleportLocationAlliance : ExploitTeleportLocationHorde);
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
		if (team == TeamIds.Alliance || team == TeamIds.Horde)
			return m_FlagKeepers[team];

		return ObjectGuid.Empty;
	}

	public override void SetDroppedFlagGUID(ObjectGuid guid, int team = -1)
	{
		if (team == TeamIds.Alliance || team == TeamIds.Horde)
			m_DroppedFlagGUID[team] = guid;
	}

	void RespawnFlag(TeamFaction Team, bool captured)
	{
		if (Team == TeamFaction.Alliance)
		{
			Log.outDebug(LogFilter.Battleground, "Respawn Alliance flag");
			_flagState[TeamIds.Alliance] = WSGFlagState.OnBase;
		}
		else
		{
			Log.outDebug(LogFilter.Battleground, "Respawn Horde flag");
			_flagState[TeamIds.Horde] = WSGFlagState.OnBase;
		}

		if (captured)
		{
			//when map_update will be allowed for Battlegrounds this code will be useless
			SpawnBGObject(WSGObjectTypes.HFlag, BattlegroundConst.RespawnImmediately);
			SpawnBGObject(WSGObjectTypes.AFlag, BattlegroundConst.RespawnImmediately);
			SendBroadcastText(WSGBroadcastTexts.FlagsPlaced, ChatMsg.BgSystemNeutral);
			PlaySoundToAll(WSGSound.FlagsRespawned); // flag respawned sound...
		}

		_bothFlagsKept = false;
	}

	void RespawnFlagAfterDrop(TeamFaction team)
	{
		if (GetStatus() != BattlegroundStatus.InProgress)
			return;

		RespawnFlag(team, false);

		if (team == TeamFaction.Alliance)
			SpawnBGObject(WSGObjectTypes.AFlag, BattlegroundConst.RespawnImmediately);
		else
			SpawnBGObject(WSGObjectTypes.HFlag, BattlegroundConst.RespawnImmediately);

		SendBroadcastText(WSGBroadcastTexts.FlagsPlaced, ChatMsg.BgSystemNeutral);
		PlaySoundToAll(WSGSound.FlagsRespawned);

		var obj = GetBgMap().GetGameObject(GetDroppedFlagGUID(team));

		if (obj)
			obj.Delete();
		else
			Log.Logger.Error("unknown droped flag ({0})", GetDroppedFlagGUID(team).ToString());

		SetDroppedFlagGUID(ObjectGuid.Empty, GetTeamIndexByTeamId(team));
		_bothFlagsKept = false;
		// Check opposing flag if it is in capture zone; if so, capture it
		HandleFlagRoomCapturePoint(team == TeamFaction.Alliance ? TeamIds.Horde : TeamIds.Alliance);
	}

	void EventPlayerCapturedFlag(Player player)
	{
		if (GetStatus() != BattlegroundStatus.InProgress)
			return;

		TeamFaction winner = 0;

		player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
		var team = GetPlayerTeam(player.GUID);

		if (team == TeamFaction.Alliance)
		{
			if (!IsHordeFlagPickedup())
				return;

			SetHordeFlagPicker(ObjectGuid.Empty); // must be before aura remove to prevent 2 events (drop+capture) at the same time
			// horde flag in base (but not respawned yet)
			_flagState[TeamIds.Horde] = WSGFlagState.WaitRespawn;
			// Drop Horde Flag from Player
			player.RemoveAura(WSGSpellId.WarsongFlag);

			if (_flagDebuffState == 1)
				player.RemoveAura(WSGSpellId.FocusedAssault);
			else if (_flagDebuffState == 2)
				player.RemoveAura(WSGSpellId.BrutalAssault);

			if (GetTeamScore(TeamIds.Alliance) < WSGTimerOrScore.MaxTeamScore)
				AddPoint(TeamFaction.Alliance, 1);

			PlaySoundToAll(WSGSound.FlagCapturedAlliance);
			RewardReputationToTeam(890, m_ReputationCapture, TeamFaction.Alliance);
		}
		else
		{
			if (!IsAllianceFlagPickedup())
				return;

			SetAllianceFlagPicker(ObjectGuid.Empty); // must be before aura remove to prevent 2 events (drop+capture) at the same time
			// alliance flag in base (but not respawned yet)
			_flagState[TeamIds.Alliance] = WSGFlagState.WaitRespawn;
			// Drop Alliance Flag from Player
			player.RemoveAura(WSGSpellId.SilverwingFlag);

			if (_flagDebuffState == 1)
				player.RemoveAura(WSGSpellId.FocusedAssault);
			else if (_flagDebuffState == 2)
				player.RemoveAura(WSGSpellId.BrutalAssault);

			if (GetTeamScore(TeamIds.Horde) < WSGTimerOrScore.MaxTeamScore)
				AddPoint(TeamFaction.Horde, 1);

			PlaySoundToAll(WSGSound.FlagCapturedHorde);
			RewardReputationToTeam(889, m_ReputationCapture, TeamFaction.Horde);
		}

		//for flag capture is reward 2 honorable kills
		RewardHonorToTeam(GetBonusHonorFromKill(2), team);

		SpawnBGObject(WSGObjectTypes.HFlag, WSGTimerOrScore.FlagRespawnTime);
		SpawnBGObject(WSGObjectTypes.AFlag, WSGTimerOrScore.FlagRespawnTime);

		if (team == TeamFaction.Alliance)
			SendBroadcastText(WSGBroadcastTexts.CapturedHordeFlag, ChatMsg.BgSystemAlliance, player);
		else
			SendBroadcastText(WSGBroadcastTexts.CapturedAllianceFlag, ChatMsg.BgSystemHorde, player);

		UpdateFlagState(team, WSGFlagState.WaitRespawn); // flag state none
		UpdateTeamScore(GetTeamIndexByTeamId(team));
		// only flag capture should be updated
		UpdatePlayerScore(player, ScoreType.FlagCaptures, 1); // +1 flag captures

		// update last flag capture to be used if teamscore is equal
		SetLastFlagCapture(team);

		if (GetTeamScore(TeamIds.Alliance) == WSGTimerOrScore.MaxTeamScore)
			winner = TeamFaction.Alliance;

		if (GetTeamScore(TeamIds.Horde) == WSGTimerOrScore.MaxTeamScore)
			winner = TeamFaction.Horde;

		if (winner != 0)
		{
			UpdateWorldState(WSGWorldStates.FlagStateAlliance, 1);
			UpdateWorldState(WSGWorldStates.FlagStateHorde, 1);
			UpdateWorldState(WSGWorldStates.StateTimerActive, 0);

			RewardHonorToTeam(Honor[(int)m_HonorMode][(int)WSGRewards.Win], winner);
			EndBattleground(winner);
		}
		else
		{
			_flagsTimer[GetTeamIndexByTeamId(team)] = WSGTimerOrScore.FlagRespawnTime;
		}
	}

	void HandleFlagRoomCapturePoint(int team)
	{
		var flagCarrier = _objectAccessor.GetPlayer(GetBgMap(), GetFlagPickerGUID(team));
		var areaTrigger = team == TeamIds.Alliance ? 3647 : 3646u;

		if (flagCarrier != null && flagCarrier.IsInAreaTriggerRadius(_cliDb.AreaTriggerStorage.LookupByKey(areaTrigger)))
			EventPlayerCapturedFlag(flagCarrier);
	}

	void UpdateFlagState(TeamFaction team, WSGFlagState value)
	{
		int transformValueToOtherTeamControlWorldState(WSGFlagState value)
		{
			switch (value)
			{
				case WSGFlagState.OnBase:
				case WSGFlagState.OnGround:
				case WSGFlagState.WaitRespawn:
					return 1;
				case WSGFlagState.OnPlayer:
					return 2;
				default:
					return 0;
			}
		}

		;

		if (team == TeamFaction.Horde)
		{
			UpdateWorldState(WSGWorldStates.FlagStateAlliance, (int)value);
			UpdateWorldState(WSGWorldStates.FlagControlHorde, transformValueToOtherTeamControlWorldState(value));
		}
		else
		{
			UpdateWorldState(WSGWorldStates.FlagStateHorde, (int)value);
			UpdateWorldState(WSGWorldStates.FlagControlAlliance, transformValueToOtherTeamControlWorldState(value));
		}
	}

	void UpdateTeamScore(int team)
	{
		if (team == TeamIds.Alliance)
			UpdateWorldState(WSGWorldStates.FlagCapturesAlliance, (int)GetTeamScore(team));
		else
			UpdateWorldState(WSGWorldStates.FlagCapturesHorde, (int)GetTeamScore(team));
	}

	void SetAllianceFlagPicker(ObjectGuid guid)
	{
		m_FlagKeepers[TeamIds.Alliance] = guid;
	}

	void SetHordeFlagPicker(ObjectGuid guid)
	{
		m_FlagKeepers[TeamIds.Horde] = guid;
	}

	bool IsAllianceFlagPickedup()
	{
		return !m_FlagKeepers[TeamIds.Alliance].IsEmpty;
	}

	bool IsHordeFlagPickedup()
	{
		return !m_FlagKeepers[TeamIds.Horde].IsEmpty;
	}

	WSGFlagState GetFlagState(TeamFaction team)
	{
		return _flagState[GetTeamIndexByTeamId(team)];
	}

	void SetLastFlagCapture(TeamFaction team)
	{
		_lastFlagCaptureTeam = (uint)team;
	}

	ObjectGuid GetDroppedFlagGUID(TeamFaction team)
	{
		return m_DroppedFlagGUID[GetTeamIndexByTeamId(team)];
	}

	void AddPoint(TeamFaction team, uint Points = 1)
	{
		m_TeamScores[GetTeamIndexByTeamId(team)] += Points;
	}
}

class BattlegroundWGScore : BattlegroundScore
{
	uint FlagCaptures;
	uint FlagReturns;
	public BattlegroundWGScore(ObjectGuid playerGuid, TeamFaction team) : base(playerGuid, team) { }

	public override void UpdateScore(ScoreType type, uint value)
	{
		switch (type)
		{
			case ScoreType.FlagCaptures: // Flags captured
				FlagCaptures += value;

				break;
			case ScoreType.FlagReturns: // Flags returned
				FlagReturns += value;

				break;
			default:
				base.UpdateScore(type, value);

				break;
		}
	}

	public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
	{
		base.BuildPvPLogPlayerDataPacket(out playerData);

		playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat(WSObjectives.CaptureFlag, FlagCaptures));
		playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat(WSObjectives.ReturnFlag, FlagReturns));
	}

	public override uint GetAttr1()
	{
		return FlagCaptures;
	}

	public override uint GetAttr2()
	{
		return FlagReturns;
	}
}

#region Constants

enum WSGRewards
{
	Win = 0,
	FlapCap,
	MapComplete,
	RewardNum
}

enum WSGFlagState
{
	OnBase = 1,
	OnPlayer = 2,
	OnGround = 3,
	WaitRespawn = 4
}

struct WSGObjectTypes
{
	public const int DoorA1 = 0;
	public const int DoorA2 = 1;
	public const int DoorA3 = 2;
	public const int DoorA4 = 3;
	public const int DoorA5 = 4;
	public const int DoorA6 = 5;
	public const int DoorH1 = 6;
	public const int DoorH2 = 7;
	public const int DoorH3 = 8;
	public const int DoorH4 = 9;
	public const int AFlag = 10;
	public const int HFlag = 11;
	public const int Speedbuff1 = 12;
	public const int Speedbuff2 = 13;
	public const int Regenbuff1 = 14;
	public const int Regenbuff2 = 15;
	public const int Berserkbuff1 = 16;
	public const int Berserkbuff2 = 17;
	public const int Max = 18;
}

public sealed class WSGObjectEntry
{
	public const uint DoorA1 = 179918;
	public const uint DoorA2 = 179919;
	public const uint DoorA3 = 179920;
	public const uint DoorA4 = 179921;
	public const uint DoorA5 = 180322;
	public const uint DoorA6 = 180322;
	public const uint DoorH1 = 179916;
	public const uint DoorH2 = 179917;
	public const uint DoorH3 = 180322;
	public const uint DoorH4 = 180322;
	public const uint AFlag = 179830;
	public const uint HFlag = 179831;
	public const uint AFlagGround = 179785;
	public const uint HFlagGround = 179786;
}

struct WSGCreatureTypes
{
	public const int SpiritMainAlliance = 0;
	public const int SpiritMainHorde = 1;

	public const int Max = 2;
}

struct WSGWorldStates
{
	public const uint FlagStateAlliance = 1545;
	public const uint FlagStateHorde = 1546;
	public const uint FlagStateNeutral = 1547;           // Unused
	public const uint HordeFlagCountPickedUp = 17712;    // Brawl
	public const uint AllianceFlagCountPickedUp = 17713; // Brawl
	public const uint FlagCapturesAlliance = 1581;
	public const uint FlagCapturesHorde = 1582;
	public const uint FlagCapturesMax = 1601;
	public const uint FlagCapturesMaxNew = 17303;
	public const uint FlagControlHorde = 2338;
	public const uint FlagControlAlliance = 2339;
	public const uint StateTimer = 4248;
	public const uint StateTimerActive = 4247;
}

struct WSGSpellId
{
	public const uint WarsongFlag = 23333;
	public const uint WarsongFlagDropped = 23334;
	public const uint WarsongFlagPicked = 61266; // Fake Spell; Does Not Exist But Used As Timer Start Event
	public const uint SilverwingFlag = 23335;
	public const uint SilverwingFlagDropped = 23336;
	public const uint SilverwingFlagPicked = 61265; // Fake Spell; Does Not Exist But Used As Timer Start Event
	public const uint FocusedAssault = 46392;
	public const uint BrutalAssault = 46393;
}

struct WSGTimerOrScore
{
	public const uint MaxTeamScore = 3;
	public const int FlagRespawnTime = 23000;
	public const int FlagDropTime = 10000;
	public const uint SpellForceTime = 600000;
	public const uint SpellBrutalTime = 900000;
}

struct WSGGraveyards
{
	public const uint FlagRoomAlliance = 769;
	public const uint FlagRoomHorde = 770;
	public const uint MainAlliance = 771;
	public const uint MainHorde = 772;
}

struct WSGSound
{
	public const uint FlagCapturedAlliance = 8173;
	public const uint FlagCapturedHorde = 8213;
	public const uint FlagPlaced = 8232;
	public const uint FlagReturned = 8192;
	public const uint HordeFlagPickedUp = 8212;
	public const uint AllianceFlagPickedUp = 8174;
	public const uint FlagsRespawned = 8232;
}

struct WSGBroadcastTexts
{
	public const uint StartOneMinute = 10015;
	public const uint StartHalfMinute = 10016;
	public const uint BattleHasBegun = 10014;

	public const uint CapturedHordeFlag = 9801;
	public const uint CapturedAllianceFlag = 9802;
	public const uint FlagsPlaced = 9803;
	public const uint AllianceFlagPickedUp = 9804;
	public const uint AllianceFlagDropped = 9805;
	public const uint HordeFlagPickedUp = 9807;
	public const uint HordeFlagDropped = 9806;
	public const uint AllianceFlagReturned = 9808;
	public const uint HordeFlagReturned = 9809;
}

struct WSObjectives
{
	public const int CaptureFlag = 42;
	public const int ReturnFlag = 44;
}

#endregion