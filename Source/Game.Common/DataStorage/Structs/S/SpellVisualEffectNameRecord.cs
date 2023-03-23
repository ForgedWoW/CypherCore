using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.S;

public sealed class SpellVisualEffectNameRecord
{
	public uint Id;
	public int ModelFileDataID;
	public float BaseMissileSpeed;
	public float Scale;
	public float MinAllowedScale;
	public float MaxAllowedScale;
	public float Alpha;
	public uint Flags;
	public int TextureFileDataID;
	public float EffectRadius;
	public uint Type;
	public int GenericID;
	public uint RibbonQualityID;
	public int DissolveEffectID;
	public int ModelPosition;
	public sbyte Unknown901;
}
