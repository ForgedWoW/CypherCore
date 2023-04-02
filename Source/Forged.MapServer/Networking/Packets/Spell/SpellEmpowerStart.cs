// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellEmpowerStart : ServerPacket
{
    public ObjectGuid Caster;
    public ObjectGuid CastID;
    public uint Duration;
    public uint FinalStageDuration;
    public uint FirstStageDuration;
    public SpellHealPrediction? HealPrediction;
    public SpellChannelStartInterruptImmunities? Immunities;
    public uint SpellID;
    public Dictionary<byte, uint> StageDurations = new();
    public List<ObjectGuid> Targets = new();
    public SpellCastVisual Visual;
    public SpellEmpowerStart() : base(ServerOpcodes.SpellEmpowerStart, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(CastID);
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.WriteUInt32((uint)Targets.Count);
        WorldPacket.Write(SpellID);
        Visual.Write(WorldPacket);
        WorldPacket.Write(Duration);
        WorldPacket.Write(FirstStageDuration);
        WorldPacket.Write(FinalStageDuration);
        WorldPacket.WriteUInt32((uint)StageDurations.Count);

        foreach (var target in Targets)
            WorldPacket.Write(target);

        foreach (var val in StageDurations.Values)
            WorldPacket.Write(val);

        WorldPacket.Write(Immunities.HasValue);
        WorldPacket.Write(HealPrediction.HasValue);

        Immunities?.Write(WorldPacket);

        HealPrediction?.Write(WorldPacket);
    }
}