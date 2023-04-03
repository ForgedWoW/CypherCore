// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.BattleGround;
using Framework.Constants;

namespace Forged.MapServer.BattleGrounds.Zones;

internal class BattlegroundABScore : BattlegroundScore
{
    private uint BasesAssaulted;
    private uint BasesDefended;

    public BattlegroundABScore(ObjectGuid playerGuid, TeamFaction team) : base(playerGuid, team)
    {
        BasesAssaulted = 0;
        BasesDefended = 0;
    }

    public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
    {
        base.BuildPvPLogPlayerDataPacket(out playerData);

        playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)ABObjectives.AssaultBase, BasesAssaulted));
        playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)ABObjectives.DefendBase, BasesDefended));
    }

    public override uint GetAttr1()
    {
        return BasesAssaulted;
    }

    public override uint GetAttr2()
    {
        return BasesDefended;
    }

    public override void UpdateScore(ScoreType type, uint value)
    {
        switch (type)
        {
            case ScoreType.BasesAssaulted:
                BasesAssaulted += value;

                break;
            case ScoreType.BasesDefended:
                BasesDefended += value;

                break;
            default:
                base.UpdateScore(type, value);

                break;
        }
    }
}