// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Game.Common.DataStorage.Structs.G;

public sealed class GameObjectDisplayInfoRecord
{
	public uint Id;
	public float[] GeoBox = new float[6];
	public int FileDataID;
	public short ObjectEffectPackageID;
	public float OverrideLootEffectScale;
	public float OverrideNameScale;

	public Vector3 GeoBoxMin
	{
		get { return new Vector3(GeoBox[0], GeoBox[1], GeoBox[2]); }
		set
		{
			GeoBox[0] = value.X;
			GeoBox[1] = value.Y;
			GeoBox[2] = value.Z;
		}
	}

	public Vector3 GeoBoxMax
	{
		get { return new Vector3(GeoBox[3], GeoBox[4], GeoBox[5]); }
		set
		{
			GeoBox[3] = value.X;
			GeoBox[4] = value.Y;
			GeoBox[5] = value.Z;
		}
	}
}
