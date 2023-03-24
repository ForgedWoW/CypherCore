// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Common.DataStorage.Structs.V;

public sealed class VehicleRecord
{
	public uint Id;
	public VehicleFlags Flags;
	public byte FlagsB;
	public float TurnSpeed;
	public float PitchSpeed;
	public float PitchMin;
	public float PitchMax;
	public float MouseLookOffsetPitch;
	public float CameraFadeDistScalarMin;
	public float CameraFadeDistScalarMax;
	public float CameraPitchOffset;
	public float FacingLimitRight;
	public float FacingLimitLeft;
	public float CameraYawOffset;
	public ushort VehicleUIIndicatorID;
	public int MissileTargetingID;
	public ushort VehiclePOITypeID;
	public ushort[] SeatID = new ushort[8];
	public ushort[] PowerDisplayID = new ushort[3];
}
