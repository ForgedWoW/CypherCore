// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Chrono;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities.Players;

public class RestMgr
{
    private readonly Player _player;
    private readonly double[] _restBonus = new double[(int)RestTypes.Max];
    private uint _innAreaTriggerId;
    private RestFlag _restFlagMask;
    private long _restTime;
    public RestMgr(Player player)
    {
        _player = player;
    }

    public void AddRestBonus(RestTypes restType, double restBonus)
    {
        // Don't add extra rest bonus to max level players. Note: Might need different condition in next expansion for honor XP (PLAYER_LEVEL_MIN_HONOR perhaps).
        if (_player.Level >= GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            restBonus = 0;

        var totalRestBonus = GetRestBonus(restType) + restBonus;
        SetRestBonus(restType, totalRestBonus);
    }

    public float CalcExtraPerSec(RestTypes restType, float bubble)
    {
        return restType switch
        {
            RestTypes.Honor => _player.ActivePlayerData.HonorNextLevel / 72000.0f * bubble,
            RestTypes.XP    => _player.ActivePlayerData.NextLevelXP / 72000.0f * bubble,
            _               => 0.0f
        };
    }

    public uint GetInnTriggerId()
    {
        return _innAreaTriggerId;
    }

    public double GetRestBonus(RestTypes restType)
    {
        return _restBonus[(int)restType];
    }

    public double GetRestBonusFor(RestTypes restType, uint xp)
    {
        var restedBonus = GetRestBonus(restType); // xp for each rested bonus

        if (restedBonus > xp) // max rested_bonus == xp or (r+x) = 200% xp
            restedBonus = xp;

        var restedLoss = restedBonus;

        if (restType == RestTypes.XP)
            MathFunctions.AddPct(ref restedLoss, _player.GetTotalAuraModifier(AuraType.ModRestedXpConsumption));

        SetRestBonus(restType, GetRestBonus(restType) - restedLoss);

        Log.Logger.Debug("RestMgr.GetRestBonus: Player '{0}' ({1}) gain {2} xp (+{3} Rested Bonus). Rested points={4}",
                         _player.GUID.ToString(),
                         _player.GetName(),
                         xp + restedBonus,
                         restedBonus,
                         GetRestBonus(restType));

        return restedBonus;
    }

    public bool HasRestFlag(RestFlag restFlag)
    {
        return (_restFlagMask & restFlag) != 0;
    }

    public void LoadRestBonus(RestTypes restType, PlayerRestState state, float restBonus)
    {
        _restBonus[(int)restType] = restBonus;
        _player.SetRestState(restType, state);
        _player.SetRestThreshold(restType, (uint)restBonus);
    }

    public void RemoveRestFlag(RestFlag restFlag)
    {
        var oldRestMask = _restFlagMask;
        _restFlagMask &= ~restFlag;

        if (oldRestMask != 0 && _restFlagMask == 0) // only remove Id/time on the last rest state remove
        {
            _restTime = 0;
            _player.RemovePlayerFlag(PlayerFlags.Resting);
        }
    }

    public void SetRestBonus(RestTypes restType, double restBonus)
    {
        uint nextLevelXp;
        var affectedByRaF = false;

        switch (restType)
        {
            case RestTypes.XP:
                // Reset restBonus (XP only) for max level players
                if (_player.Level >= GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
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

        var restBonusMax = nextLevelXp * 1.5f / 2;

        if (restBonus < 0)
            restBonus = 0;

        if (restBonus > restBonusMax)
            restBonus = restBonusMax;

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
    public void SetRestFlag(RestFlag restFlag, uint triggerId = 0)
    {
        var oldRestMask = _restFlagMask;
        _restFlagMask |= restFlag;

        if (oldRestMask == 0 && _restFlagMask != 0) // only set Id/time on the first rest state
        {
            _restTime = GameTime.CurrentTime;
            _player.SetPlayerFlag(PlayerFlags.Resting);
        }

        if (triggerId != 0)
            _innAreaTriggerId = triggerId;
    }
    public void Update(uint now)
    {
        if (RandomHelper.randChance(3) && _restTime > 0) // freeze update
        {
            var timeDiff = now - _restTime;

            if (timeDiff >= 10)
            {
                _restTime = now;

                var bubble = 0.125f * GetDefaultValue("Rate.Rest.InGame", 1.0f);
                AddRestBonus(RestTypes.XP, timeDiff * CalcExtraPerSec(RestTypes.XP, bubble));
            }
        }
    }
}