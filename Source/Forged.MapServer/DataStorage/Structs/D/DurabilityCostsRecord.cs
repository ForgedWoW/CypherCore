// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.D;

public sealed record DurabilityCostsRecord
{
    public ushort[] ArmorSubClassCost = new ushort[8];
    public uint Id;
    public ushort[] WeaponSubClassCost = new ushort[21];
}