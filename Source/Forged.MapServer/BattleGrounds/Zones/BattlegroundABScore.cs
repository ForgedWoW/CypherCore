// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.BattleGround;
using Framework.Constants;

namespace Forged.MapServer.BattleGrounds.Zones;

internal class BattlegroundABScore : BattlegroundScore
{
    private uint _basesAssaulted;
    private uint _basesDefended;

    public BattlegroundABScore(ObjectGuid playerGuid, TeamFaction team) : base(playerGuid, team)
    {
        _basesAssaulted = 0;
        _basesDefended = 0;
    }

    public override void BuildPvPLogPlayerDataPacket(out PVPMatchStatistics.PVPMatchPlayerStatistics playerData)
    {
        base.BuildPvPLogPlayerDataPacket(out playerData);

        playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)ABObjectives.AssaultBase, _basesAssaulted));
        playerData.Stats.Add(new PVPMatchStatistics.PVPMatchPlayerPVPStat((int)ABObjectives.DefendBase, _basesDefended));
    }

    public override uint GetAttr1()
    {
        return _basesAssaulted;
    }

    public override uint GetAttr2()
    {
        return _basesDefended;
    }

    public override void UpdateScore(ScoreType type, uint value)
    {
        switch (type)
        {
            case ScoreType.BasesAssaulted:
                _basesAssaulted += value;

                break;
            case ScoreType.BasesDefended:
                _basesDefended += value;

                break;
            default:
                base.UpdateScore(type, value);

                break;
        }
    }
}