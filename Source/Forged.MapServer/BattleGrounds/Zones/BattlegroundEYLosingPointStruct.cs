// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct BattlegroundEYLosingPointStruct
{
    public int DespawnObjectTypeAlliance;

    public int DespawnObjectTypeHorde;

    public uint MessageIdAlliance;

    public uint MessageIdHorde;

    public int SpawnNeutralObjectType;

    public BattlegroundEYLosingPointStruct(int _SpawnNeutralObjectType, int _DespawnObjectTypeAlliance, uint _MessageIdAlliance, int _DespawnObjectTypeHorde, uint _MessageIdHorde)
    {
        SpawnNeutralObjectType = _SpawnNeutralObjectType;
        DespawnObjectTypeAlliance = _DespawnObjectTypeAlliance;
        MessageIdAlliance = _MessageIdAlliance;
        DespawnObjectTypeHorde = _DespawnObjectTypeHorde;
        MessageIdHorde = _MessageIdHorde;
    }
}