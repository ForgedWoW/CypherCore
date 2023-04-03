// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct BattlegroundEYCapturingPointStruct
{
    public int DespawnNeutralObjectType;

    public uint GraveYardId;

    public uint MessageIdAlliance;

    public uint MessageIdHorde;

    public int SpawnObjectTypeAlliance;

    public int SpawnObjectTypeHorde;

    public BattlegroundEYCapturingPointStruct(int _DespawnNeutralObjectType, int _SpawnObjectTypeAlliance, uint _MessageIdAlliance, int _SpawnObjectTypeHorde, uint _MessageIdHorde, uint _GraveYardId)
    {
        DespawnNeutralObjectType = _DespawnNeutralObjectType;
        SpawnObjectTypeAlliance = _SpawnObjectTypeAlliance;
        MessageIdAlliance = _MessageIdAlliance;
        SpawnObjectTypeHorde = _SpawnObjectTypeHorde;
        MessageIdHorde = _MessageIdHorde;
        GraveYardId = _GraveYardId;
    }
}