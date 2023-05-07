// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.I;

public sealed record ItemModifiedAppearanceExtraRecord
{
    public sbyte DisplayInventoryType;
    public sbyte DisplayWeaponSubclassID;
    public int IconFileDataID;
    public uint Id;
    public byte SheatheType;
    public int UnequippedIconFileDataID;
}