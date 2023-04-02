// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AreaTrigger;

internal class AreaTriggerRePath : ServerPacket
{
    public AreaTriggerMovementScriptInfo? AreaTriggerMovementScript;
    public AreaTriggerOrbitInfo AreaTriggerOrbit;
    public AreaTriggerSplineInfo AreaTriggerSpline;
    public ObjectGuid TriggerGUID;
    public AreaTriggerRePath() : base(ServerOpcodes.AreaTriggerRePath) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(TriggerGUID);

        WorldPacket.WriteBit(AreaTriggerSpline != null);
        WorldPacket.WriteBit(AreaTriggerOrbit != null);
        WorldPacket.WriteBit(AreaTriggerMovementScript.HasValue);
        WorldPacket.FlushBits();

        AreaTriggerSpline?.Write(WorldPacket);

        AreaTriggerMovementScript?.Write(WorldPacket);

        AreaTriggerOrbit?.Write(WorldPacket);
    }
}