// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.V;

public sealed record VehicleRecord
{
    public float CameraFadeDistScalarMax;
    public float CameraFadeDistScalarMin;
    public float CameraPitchOffset;
    public float CameraYawOffset;
    public float FacingLimitLeft;
    public float FacingLimitRight;
    public VehicleFlags Flags;
    public int FlagsB;
    public uint Id;
    public int MissileTargetingID;
    public float MouseLookOffsetPitch;
    public float PitchMax;
    public float PitchMin;
    public float PitchSpeed;
    public ushort[] PowerDisplayID = new ushort[3];
    public ushort[] SeatID = new ushort[8];
    public float TurnSpeed;
    public ushort VehiclePOITypeID;
    public ushort VehicleUIIndicatorID;
}