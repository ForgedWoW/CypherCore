// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.D;

public sealed class DurabilityCostsRecord
{
	public uint Id;
	public ushort[] WeaponSubClassCost = new ushort[21];
	public ushort[] ArmorSubClassCost = new ushort[8];
}
