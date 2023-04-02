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
    public ObjectGuid Transport;   // Used when Target = Empty && (SpellVisual::Flags & 0x400) == 0
                                   // Overrides missile destination for SpellVisual::SpellVisualMissileSetID
    public float TravelSpeed;
    public PlaySpellVisual() : base(ServerOpcodes.PlaySpellVisual) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Source);
        _worldPacket.WritePackedGuid(Target);
        _worldPacket.WritePackedGuid(Transport);
        _worldPacket.WriteVector3(TargetPosition);
        _worldPacket.WriteUInt32(SpellVisualID);
        _worldPacket.WriteFloat(TravelSpeed);
        _worldPacket.WriteUInt16(HitReason);
        _worldPacket.WriteUInt16(MissReason);
        _worldPacket.WriteUInt16(ReflectStatus);
        _worldPacket.WriteFloat(LaunchDelay);
        _worldPacket.WriteFloat(MinDuration);
        _worldPacket.WriteBit(SpeedAsTime);
        _worldPacket.FlushBits();
    }
}