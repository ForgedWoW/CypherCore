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
        WorldPacket.WritePackedGuid(CasterGUID);
        WorldPacket.WriteInt32(SpellID);

        Visual.Write(WorldPacket);

        WorldPacket.WriteUInt32(ChannelDuration);
        WorldPacket.WriteBit(InterruptImmunities.HasValue);
        WorldPacket.WriteBit(HealPrediction.HasValue);
        WorldPacket.FlushBits();

        InterruptImmunities?.Write(WorldPacket);

        HealPrediction?.Write(WorldPacket);
    }
}