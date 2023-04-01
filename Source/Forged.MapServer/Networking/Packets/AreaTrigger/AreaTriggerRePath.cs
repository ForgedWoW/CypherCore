// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.AreaTrigger;

internal class AreaTriggerRePath : ServerPacket
{
    public AreaTriggerSplineInfo AreaTriggerSpline;
    public AreaTriggerOrbitInfo AreaTriggerOrbit;
    public AreaTriggerMovementScriptInfo? AreaTriggerMovementScript;
    public ObjectGuid TriggerGUID;
    public AreaTriggerRePath() : base(ServerOpcodes.AreaTriggerRePath) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(TriggerGUID);

        _worldPacket.WriteBit(AreaTriggerSpline != null);
        _worldPacket.WriteBit(AreaTriggerOrbit != null);
        _worldPacket.WriteBit(AreaTriggerMovementScript.HasValue);
        _worldPacket.FlushBits();

        AreaTriggerSpline?.Write(_worldPacket);

        AreaTriggerMovementScript?.Write(_worldPacket);

        AreaTriggerOrbit?.Write(_worldPacket);
    }
}