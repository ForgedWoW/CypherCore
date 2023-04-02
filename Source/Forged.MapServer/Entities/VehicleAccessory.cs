// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities;

public struct VehicleAccessory
{
    public uint AccessoryEntry;

    public bool IsMinion;

    public sbyte SeatId;

    public byte SummonedType;

    public uint SummonTime;

    public VehicleAccessory(uint entry, sbyte seatId, bool isMinion, byte summonType, uint summonTime)
    {
        AccessoryEntry = entry;
        IsMinion = isMinion;
        SummonTime = summonTime;
        SeatId = seatId;
        SummonedType = summonType;
    }
}