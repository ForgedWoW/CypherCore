// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.V
{
    public sealed record VehicleRecord
    {
        public uint Id;
        public VehicleFlags Flags;
        public int FlagsB;
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
}
