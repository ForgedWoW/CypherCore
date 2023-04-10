﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking;

namespace Forged.MapServer.Entities.AreaTriggers;

public class AreaTriggerOrbitInfo
{
    public float BlendFromRadius { get; set; }
    public bool CanLoop { get; set; }
    public Vector3? Center { get; set; }
    public bool CounterClockwise { get; set; }
    public int ElapsedTimeForMovement { get; set; }
    public float InitialAngle { get; set; }
    public ObjectGuid? PathTarget { get; set; }
    public float Radius { get; set; }
    public uint StartDelay { get; set; }
    public uint TimeToTarget { get; set; }
    public float ZOffset { get; set; }

    public void Write(WorldPacket data)
    {
        data.WriteBit(PathTarget.HasValue);
        data.WriteBit(Center.HasValue);
        data.WriteBit(CounterClockwise);
        data.WriteBit(CanLoop);

        data.WriteUInt32(TimeToTarget);
        data.WriteInt32(ElapsedTimeForMovement);
        data.WriteUInt32(StartDelay);
        data.WriteFloat(Radius);
        data.WriteFloat(BlendFromRadius);
        data.WriteFloat(InitialAngle);
        data.WriteFloat(ZOffset);

        if (PathTarget.HasValue)
            data.WritePackedGuid(PathTarget.Value);

        if (Center.HasValue)
            data.WriteVector3(Center.Value);
    }
}