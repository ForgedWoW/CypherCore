// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class PlayOrphanSpellVisual : ServerPacket
{
    public float LaunchDelay;

    // Always zero
    public float MinDuration;

    public Position SourceLocation;
    public Vector3 SourceRotation;
    public bool SpeedAsTime;
    public uint SpellVisualID;

    public ObjectGuid Target; // Exclusive with TargetLocation

    // Vector of rotations, Orientation is z
    public Vector3 TargetLocation;

    public float TravelSpeed;

    // Exclusive with Target
    public PlayOrphanSpellVisual() : base(ServerOpcodes.PlayOrphanSpellVisual) { }

    public override void Write()
    {
        WorldPacket.WriteXYZ(SourceLocation);
        WorldPacket.WriteVector3(SourceRotation);
        WorldPacket.WriteVector3(TargetLocation);
        WorldPacket.WritePackedGuid(Target);
        WorldPacket.WriteUInt32(SpellVisualID);
        WorldPacket.WriteFloat(TravelSpeed);
        WorldPacket.WriteFloat(LaunchDelay);
        WorldPacket.WriteFloat(MinDuration);
        WorldPacket.WriteBit(SpeedAsTime);
        WorldPacket.FlushBits();
    }
}