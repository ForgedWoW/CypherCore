// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellChannelStart : ServerPacket
{
    public ObjectGuid CasterGUID;
    public uint ChannelDuration;
    public SpellTargetedHealPrediction? HealPrediction;
    public SpellChannelStartInterruptImmunities? InterruptImmunities;
    public int SpellID;
    public SpellCastVisual Visual;
    public SpellChannelStart() : base(ServerOpcodes.SpellChannelStart, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(CasterGUID);
        _worldPacket.WriteInt32(SpellID);

        Visual.Write(_worldPacket);

        _worldPacket.WriteUInt32(ChannelDuration);
        _worldPacket.WriteBit(InterruptImmunities.HasValue);
        _worldPacket.WriteBit(HealPrediction.HasValue);
        _worldPacket.FlushBits();

        InterruptImmunities?.Write(_worldPacket);

        HealPrediction?.Write(_worldPacket);
    }
}