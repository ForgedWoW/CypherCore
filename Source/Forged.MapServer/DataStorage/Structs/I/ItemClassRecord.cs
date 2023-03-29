// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed class ItemClassRecord
{
    public uint Id;
    public string ClassName;
    public sbyte ClassID;
    public float PriceModifier;
    public byte Flags;
}