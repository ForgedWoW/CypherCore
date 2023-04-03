// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.BattleGround;
using Framework.Constants;

namespace Forged.MapServer.Arenas;

internal class ArenaScore : BattlegroundScore
{
    private readonly uint _postMatchMmr;
    private readonly uint _postMatchRating;
    private readonly uint _preMatchMmr;
    private readonly uint _preMatchRating;

    public ArenaScore(ObjectGuid playerGuid, TeamFaction team) : base(playerGuid, team)
    {
        TeamId = (int)(team == TeamFaction.Alliance ? PvPTeamId.Alliance : PvPTeamId.Horde);
    }

    public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
    {
        base.BuildPvPLogPlayerDataPacket(out playerData);

        if (_preMatchRating != 0)
            playerData.PreMatchRating = _preMatchRating;

        if (_postMatchRating != _preMatchRating)
            playerData.RatingChange = (int)(_postMatchRating - _preMatchRating);

        if (_preMatchMmr != 0)
            playerData.PreMatchMMR = _preMatchMmr;

        if (_postMatchMmr != _preMatchMmr)
            playerData.MmrChange = (int)(_postMatchMmr - _preMatchMmr);
    }

    // For Logging purpose
    public override string ToString()
    {
        return $"Damage done: {DamageDone} Healing done: {HealingDone} Killing blows: {KillingBlows} PreMatchRating: {_preMatchRating} " +
               $"PreMatchMMR: {_preMatchMmr} PostMatchRating: {_postMatchRating} PostMatchMMR: {_postMatchMmr}";
    }
}