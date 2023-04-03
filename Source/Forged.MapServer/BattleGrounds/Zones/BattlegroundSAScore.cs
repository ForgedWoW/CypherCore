// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.BattleGround;
using Framework.Constants;

namespace Forged.MapServer.BattleGrounds.Zones;

internal class BattlegroundSAScore : BattlegroundScore
{
    private uint DemolishersDestroyed;
    private uint GatesDestroyed;
    public BattlegroundSAScore(ObjectGuid playerGuid, TeamFaction team) : base(playerGuid, team) { }

    public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
    {
        base.BuildPvPLogPlayerDataPacket(out playerData);

        playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)SAObjectives.DemolishersDestroyed, DemolishersDestroyed));
        playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)SAObjectives.GatesDestroyed, GatesDestroyed));
    }

    public override uint GetAttr1()
    {
        return DemolishersDestroyed;
    }

    public override uint GetAttr2()
    {
        return GatesDestroyed;
    }

    public override void UpdateScore(ScoreType type, uint value)
    {
        switch (type)
        {
            case ScoreType.DestroyedDemolisher:
                DemolishersDestroyed += value;

                break;
            case ScoreType.DestroyedWall:
                GatesDestroyed += value;

                break;
            default:
                base.UpdateScore(type, value);

                break;
        }
    }
}