// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Movement;

public struct VehicleTeleport
{
    public bool VehicleExitTeleport;
    public bool VehicleExitVoluntary;
    public byte VehicleSeatIndex;
}