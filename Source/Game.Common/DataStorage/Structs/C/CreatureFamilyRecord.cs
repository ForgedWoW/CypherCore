// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.ClientReader;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.C;

public sealed class CreatureFamilyRecord
{
	public uint Id;
	public LocalizedString Name;
	public float MinScale;
	public sbyte MinScaleLevel;
	public float MaxScale;
	public sbyte MaxScaleLevel;
	public ushort PetFoodMask;
	public sbyte PetTalentType;
	public int IconFileID;
	public short[] SkillLine = new short[2];
}
