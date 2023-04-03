// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.BattleGround;
using Framework.Constants;

namespace Forged.MapServer.BattleGrounds.Zones;

internal class BgEyeOfStormScore : BattlegroundScore
{
    private uint _flagCaptures;
    public BgEyeOfStormScore(ObjectGuid playerGuid, TeamFaction team) : base(playerGuid, team) { }

    public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
    {
        base.BuildPvPLogPlayerDataPacket(out playerData);

        playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)EotSMisc.OBJECTIVE_CAPTURE_FLAG, _flagCaptures));
    }

    public override uint GetAttr1()
    {
        return _flagCaptures;
    }

    public override void UpdateScore(ScoreType type, uint value)
    {
        switch (type)
        {
            case ScoreType.FlagCaptures: // Flags captured
                _flagCaptures += value;

                break;
            default:
                base.UpdateScore(type, value);

                break;
        }
    }
}