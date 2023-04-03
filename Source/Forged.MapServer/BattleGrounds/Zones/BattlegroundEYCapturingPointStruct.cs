// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct BattlegroundEyCapturingPointStruct
{
    public int DespawnNeutralObjectType;

    public uint GraveYardId;

    public uint MessageIdAlliance;

    public uint MessageIdHorde;

    public int SpawnObjectTypeAlliance;

    public int SpawnObjectTypeHorde;

    public BattlegroundEyCapturingPointStruct(int despawnNeutralObjectType, int spawnObjectTypeAlliance, uint messageIdAlliance, int spawnObjectTypeHorde, uint messageIdHorde, uint graveYardId)
    {
        DespawnNeutralObjectType = despawnNeutralObjectType;
        SpawnObjectTypeAlliance = spawnObjectTypeAlliance;
        MessageIdAlliance = messageIdAlliance;
        SpawnObjectTypeHorde = spawnObjectTypeHorde;
        MessageIdHorde = messageIdHorde;
        GraveYardId = graveYardId;
    }
}