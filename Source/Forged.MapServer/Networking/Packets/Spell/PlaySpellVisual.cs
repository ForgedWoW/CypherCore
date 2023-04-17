// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class PlaySpellVisual : ServerPacket
{
    public ushort HitReason;
    public float LaunchDelay;
    public float MinDuration;
    public ushort MissReason;
    public ushort ReflectStatus;
    public ObjectGuid Source;
    public bool SpeedAsTime;
    public uint SpellVisualID;
    public ObjectGuid Target;
    public Vector3 TargetPosition;

    public ObjectGuid Transport; // Used when Target = Empty && (SpellVisual::Flags & 0x400) == 0

    // Overrides missile destination for SpellVisual::SpellVisualMissileSetID
    public float TravelSpeed;
    public PlaySpellVisual() : base(ServerOpcodes.PlaySpellVisual) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Source);
        WorldPacket.WritePackedGuid(Target);
        WorldPacket.WritePackedGuid(Transport);
        WorldPacket.WriteVector3(TargetPosition);
        WorldPacket.WriteUInt32(SpellVisualID);
        WorldPacket.WriteFloat(TravelSpeed);
        WorldPacket.WriteUInt16(HitReason);
        WorldPacket.WriteUInt16(MissReason);
        WorldPacket.WriteUInt16(ReflectStatus);
        WorldPacket.WriteFloat(LaunchDelay);
        WorldPacket.WriteFloat(MinDuration);
        WorldPacket.WriteBit(SpeedAsTime);
        WorldPacket.FlushBits();
    }
}