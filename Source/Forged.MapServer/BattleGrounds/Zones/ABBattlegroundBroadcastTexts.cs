// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct ABBattlegroundBroadcastTexts
{
    public const uint AllianceNearVictory = 10598;
    public const uint HordeNearVictory = 10599;

    public static ABNodeInfo[] ABNodes =
    {
        new(ABBattlegroundNodes.NodeStables, 10199, 10200, 10203, 10204, 10201, 10202, 10286, 10287), new(ABBattlegroundNodes.NodeBlacksmith, 10211, 10212, 10213, 10214, 10215, 10216, 10290, 10291), new(ABBattlegroundNodes.NodeFarm, 10217, 10218, 10219, 10220, 10221, 10222, 10288, 10289), new(ABBattlegroundNodes.NodeLumberMill, 10224, 10225, 10226, 10227, 10228, 10229, 10284, 10285), new(ABBattlegroundNodes.NodeGoldMine, 10230, 10231, 10232, 10233, 10234, 10235, 10282, 10283)
    };
}