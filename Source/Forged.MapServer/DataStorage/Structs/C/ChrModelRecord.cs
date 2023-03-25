﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class ChrModelRecord
{
	public float[] FaceCustomizationOffset = new float[3];
	public float[] CustomizeOffset = new float[3];
	public uint Id;
	public sbyte Sex;
	public uint DisplayID;
	public int CharComponentTextureLayoutID;
	public int Flags;
	public int SkeletonFileDataID;
	public int ModelFallbackChrModelID;
	public int TextureFallbackChrModelID;
	public int HelmVisFallbackChrModelID;
	public float CustomizeScale;
	public float CustomizeFacing;
	public float CameraDistanceOffset;
	public float BarberShopCameraOffsetScale;
	public float BarberShopCameraHeightOffsetScale; // applied after BarberShopCameraOffsetScale
	public float BarberShopCameraRotationOffset;
}