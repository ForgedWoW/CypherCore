// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public struct EventId
{
    public const uint AssistMove = 1009;
    public const uint Charge = 1003;

    /// Special charge event which is used for charge spells that have explicit targets
    /// and had a path already generated - using it in PointMovementGenerator will not
    /// create a new spline and launch it
    public const uint ChargePrepath = 1005;

    public const uint Face = 1006;
    public const uint Jump = 1004;
    public const uint SmartEscortLastOCCPoint = 0xFFFFFF;
    public const uint SmartRandomPoint = 0xFFFFFE;
    public const uint VehicleBoard = 1007;
    public const uint VehicleExit = 1008;
}