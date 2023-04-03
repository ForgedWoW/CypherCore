// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct BattlegroundEyLosingPointStruct
{
    public int DespawnObjectTypeAlliance;

    public int DespawnObjectTypeHorde;

    public uint MessageIdAlliance;

    public uint MessageIdHorde;

    public int SpawnNeutralObjectType;

    public BattlegroundEyLosingPointStruct(int spawnNeutralObjectType, int despawnObjectTypeAlliance, uint messageIdAlliance, int despawnObjectTypeHorde, uint messageIdHorde)
    {
        SpawnNeutralObjectType = spawnNeutralObjectType;
        DespawnObjectTypeAlliance = despawnObjectTypeAlliance;
        MessageIdAlliance = messageIdAlliance;
        DespawnObjectTypeHorde = despawnObjectTypeHorde;
        MessageIdHorde = messageIdHorde;
    }
}