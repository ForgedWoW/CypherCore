// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.B;

public sealed record BarberShopStyleRecord
{
    public float CostModifier;
    public byte Data;
    public string Description;
    public string DisplayName;
    public uint Id;
    public byte Race;
    public byte Sex;

    public byte Type; // value 0 . hair, value 2 . facialhair
    // real ID to hair/facial hair
}