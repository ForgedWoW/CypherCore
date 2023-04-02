// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class ChrModelRecord
{
    public float BarberShopCameraHeightOffsetScale;
    public float BarberShopCameraOffsetScale;
    // applied after BarberShopCameraOffsetScale
    public float BarberShopCameraRotationOffset;

    public float CameraDistanceOffset;
    public int CharComponentTextureLayoutID;
    public float CustomizeFacing;
    public float[] CustomizeOffset = new float[3];
    public float CustomizeScale;
    public uint DisplayID;
    public float[] FaceCustomizationOffset = new float[3];
    public int Flags;
    public int HelmVisFallbackChrModelID;
    public uint Id;
    public int ModelFallbackChrModelID;
    public sbyte Sex;
    public int SkeletonFileDataID;
    public int TextureFallbackChrModelID;
}