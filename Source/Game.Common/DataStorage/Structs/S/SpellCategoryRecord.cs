// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellCategoryRecord
{
	public uint Id;
	public string Name;
	public SpellCategoryFlags Flags;
	public byte UsesPerWeek;
	public byte MaxCharges;
	public int ChargeRecoveryTime;
	public int TypeMask;
}
