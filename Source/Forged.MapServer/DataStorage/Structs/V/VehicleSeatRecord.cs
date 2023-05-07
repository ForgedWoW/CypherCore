// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.V;

public sealed record VehicleSeatRecord
{
    public sbyte AttachmentID;
    public Vector3 AttachmentOffset;
    public float CameraEnteringDelay;
    public float CameraEnteringDuration;
    public float CameraEnteringZoom;
    public float CameraExitingDelay;
    public float CameraExitingDuration;
    public float CameraFacingChaseRate;
    public short CameraModeID;
    public Vector3 CameraOffset;
    public float CameraPosChaseRate;
    public float CameraSeatZoomMax;
    public float CameraSeatZoomMin;
    public short EnterAnimKitID;
    public int EnterAnimLoop;
    public int EnterAnimStart;
    public float EnterGravity;
    public float EnterMaxArcHeight;
    public float EnterMaxDuration;
    public float EnterMinArcHeight;
    public float EnterMinDuration;
    public float EnterPreDelay;
    public float EnterSpeed;
    public uint EnterUISoundID;
    public int ExitAnimEnd;
    public short ExitAnimKitID;
    public int ExitAnimLoop;
    public int ExitAnimStart;
    public float ExitGravity;
    public float ExitMaxArcHeight;
    public float ExitMaxDuration;
    public float ExitMinArcHeight;
    public float ExitMinDuration;
    public float ExitPreDelay;
    public float ExitSpeed;
    public uint ExitUISoundID;
    public int Flags;
    public int FlagsB;
    public int FlagsC;
    public uint Id;
    public sbyte PassengerAttachmentID;
    public float PassengerPitch;
    public float PassengerRoll;
    public float PassengerYaw;
    public short RideAnimKitID;
    public int RideAnimLoop;
    public int RideAnimStart;
    public int RideUpperAnimLoop;
    public int RideUpperAnimStart;
    public int UiSkinFileDataID;
    public sbyte VehicleAbilityDisplay;
    public short VehicleEnterAnim;
    public sbyte VehicleEnterAnimBone;
    public float VehicleEnterAnimDelay;
    public short VehicleEnterAnimKitID;
    public short VehicleExitAnim;
    public sbyte VehicleExitAnimBone;
    public float VehicleExitAnimDelay;
    public short VehicleExitAnimKitID;
    public short VehicleRideAnimKitID;
    public short VehicleRideAnimLoop;
    public sbyte VehicleRideAnimLoopBone;

    public bool CanEnterOrExit()
    {
        return HasFlag(VehicleSeatFlags.CanEnterOrExit) ||
               //If it has anmation for enter/ride, means it can be entered/exited by logic
               HasFlag(VehicleSeatFlags.HasLowerAnimForEnter | VehicleSeatFlags.HasLowerAnimForRide);
    }

    public bool CanSwitchFromSeat()
    {
        return Flags.HasAnyFlag((int)VehicleSeatFlags.CanSwitch);
    }

    public bool HasFlag(VehicleSeatFlags flag)
    {
        return Flags.HasAnyFlag((int)flag);
    }

    public bool HasFlag(VehicleSeatFlagsB flag)
    {
        return FlagsB.HasAnyFlag((int)flag);
    }

    public bool IsEjectable()
    {
        return HasFlag(VehicleSeatFlagsB.Ejectable);
    }

    public bool IsUsableByOverride()
    {
        return HasFlag(VehicleSeatFlags.Uncontrolled | VehicleSeatFlags.Unk18) ||
               HasFlag(VehicleSeatFlagsB.UsableForced |
                       VehicleSeatFlagsB.UsableForced2 |
                       VehicleSeatFlagsB.UsableForced3 |
                       VehicleSeatFlagsB.UsableForced4);
    }
}