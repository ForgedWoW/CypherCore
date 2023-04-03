// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.BattleGround;
using Framework.Constants;

namespace Forged.MapServer.BattleGrounds.Zones;

internal class BattlegroundSaScore : BattlegroundScore
{
    private uint _demolishersDestroyed;
    private uint _gatesDestroyed;
    public BattlegroundSaScore(ObjectGuid playerGuid, TeamFaction team) : base(playerGuid, team) { }

    public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
    {
        base.BuildPvPLogPlayerDataPacket(out playerData);

        playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)SaObjectives.DemolishersDestroyed, _demolishersDestroyed));
        playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)SaObjectives.GatesDestroyed, _gatesDestroyed));
    }

    public override uint GetAttr1()
    {
        return _demolishersDestroyed;
    }

    public override uint GetAttr2()
    {
        return _gatesDestroyed;
    }

    public override void UpdateScore(ScoreType type, uint value)
    {
        switch (type)
        {
            case ScoreType.DestroyedDemolisher:
                _demolishersDestroyed += value;

                break;
            case ScoreType.DestroyedWall:
                _gatesDestroyed += value;

                break;
            default:
                base.UpdateScore(type, value);

                break;
        }
    }
}