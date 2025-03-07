﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Entities;

public class RestMgr
{
	readonly Player _player;
	readonly double[] _restBonus = new double[(int)RestTypes.Max];
	long _restTime;
	uint _innAreaTriggerId;
	RestFlag _restFlagMask;

	public RestMgr(Player player)
	{
		_player = player;
	}

	public void SetRestBonus(RestTypes restType, double restBonus)
	{
		uint nextLevelXp;
		var affectedByRaF = false;

		switch (restType)
		{
			case RestTypes.XP:
				// Reset restBonus (XP only) for max level players
				if (_player.Level >= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
					restBonus = 0;

				nextLevelXp = _player.ActivePlayerData.NextLevelXP;
				affectedByRaF = true;

				break;
			case RestTypes.Honor:
				// Reset restBonus (Honor only) for players with max honor level.
				if (_player.IsMaxHonorLevel)
					restBonus = 0;

				nextLevelXp = _player.ActivePlayerData.HonorNextLevel;

				break;
			default:
				return;
		}

		var rest_bonus_max = nextLevelXp * 1.5f / 2;

		if (restBonus < 0)
			restBonus = 0;

		if (restBonus > rest_bonus_max)
			restBonus = rest_bonus_max;

		var oldBonus = (uint)(_restBonus[(int)restType]);
		_restBonus[(int)restType] = restBonus;

		var oldRestState = (PlayerRestState)(int)_player.ActivePlayerData.RestInfo[(int)restType].StateID;
		var newRestState = PlayerRestState.Normal;

		if (affectedByRaF && _player.GetsRecruitAFriendBonus(true) && (_player.Session.IsARecruiter || _player.Session.RecruiterId != 0))
			newRestState = PlayerRestState.RAFLinked;
		else if (_restBonus[(int)restType] >= 1)
			newRestState = PlayerRestState.Rested;

		if (oldBonus == restBonus && oldRestState == newRestState)
			return;

		// update data for client
		_player.SetRestThreshold(restType, (uint)_restBonus[(int)restType]);
		_player.SetRestState(restType, newRestState);
	}

	public void AddRestBonus(RestTypes restType, double restBonus)
	{
		// Don't add extra rest bonus to max level players. Note: Might need different condition in next expansion for honor XP (PLAYER_LEVEL_MIN_HONOR perhaps).
		if (_player.Level >= WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
			restBonus = 0;

		var totalRestBonus = GetRestBonus(restType) + restBonus;
		SetRestBonus(restType, totalRestBonus);
	}

	public void SetRestFlag(RestFlag restFlag, uint triggerId = 0)
	{
		var oldRestMask = _restFlagMask;
		_restFlagMask |= restFlag;

		if (oldRestMask == 0 && _restFlagMask != 0) // only set flag/time on the first rest state
		{
			_restTime = GameTime.GetGameTime();
			_player.SetPlayerFlag(PlayerFlags.Resting);
		}

		if (triggerId != 0)
			_innAreaTriggerId = triggerId;
	}

	public void RemoveRestFlag(RestFlag restFlag)
	{
		var oldRestMask = _restFlagMask;
		_restFlagMask &= ~restFlag;

		if (oldRestMask != 0 && _restFlagMask == 0) // only remove flag/time on the last rest state remove
		{
			_restTime = 0;
			_player.RemovePlayerFlag(PlayerFlags.Resting);
		}
	}

	public double GetRestBonusFor(RestTypes restType, uint xp)
	{
		var rested_bonus = GetRestBonus(restType); // xp for each rested bonus

		if (rested_bonus > xp) // max rested_bonus == xp or (r+x) = 200% xp
			rested_bonus = xp;

		var rested_loss = rested_bonus;

		if (restType == RestTypes.XP)
			MathFunctions.AddPct(ref rested_loss, _player.GetTotalAuraModifier(AuraType.ModRestedXpConsumption));

		SetRestBonus(restType, GetRestBonus(restType) - rested_loss);

		Log.outDebug(LogFilter.Player,
					"RestMgr.GetRestBonus: Player '{0}' ({1}) gain {2} xp (+{3} Rested Bonus). Rested points={4}",
					_player.GUID.ToString(),
					_player.GetName(),
					xp + rested_bonus,
					rested_bonus,
					GetRestBonus(restType));

		return rested_bonus;
	}

	public void Update(uint now)
	{
		if (RandomHelper.randChance(3) && _restTime > 0) // freeze update
		{
			var timeDiff = now - _restTime;

			if (timeDiff >= 10)
			{
				_restTime = now;

				var bubble = 0.125f * WorldConfig.GetFloatValue(WorldCfg.RateRestIngame);
				AddRestBonus(RestTypes.XP, timeDiff * CalcExtraPerSec(RestTypes.XP, bubble));
			}
		}
	}

	public void LoadRestBonus(RestTypes restType, PlayerRestState state, float restBonus)
	{
		_restBonus[(int)restType] = restBonus;
		_player.SetRestState(restType, state);
		_player.SetRestThreshold(restType, (uint)restBonus);
	}

	public float CalcExtraPerSec(RestTypes restType, float bubble)
	{
		switch (restType)
		{
			case RestTypes.Honor:
				return _player.ActivePlayerData.HonorNextLevel / 72000.0f * bubble;
			case RestTypes.XP:
				return _player.ActivePlayerData.NextLevelXP / 72000.0f * bubble;
			default:
				return 0.0f;
		}
	}

	public double GetRestBonus(RestTypes restType)
	{
		return _restBonus[(int)restType];
	}

	public bool HasRestFlag(RestFlag restFlag)
	{
		return (_restFlagMask & restFlag) != 0;
	}

	public uint GetInnTriggerId()
	{
		return _innAreaTriggerId;
	}
}