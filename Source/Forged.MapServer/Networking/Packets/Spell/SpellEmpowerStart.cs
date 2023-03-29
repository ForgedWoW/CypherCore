// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellEmpowerStart : ServerPacket
{
    public uint SpellID;
    public ObjectGuid CastID;
    public ObjectGuid Caster;
    public SpellCastVisual Visual;
    public uint Duration;
    public uint FirstStageDuration;
    public uint FinalStageDuration;
    public List<ObjectGuid> Targets = new();
    public Dictionary<byte, uint> StageDurations = new();
    public SpellChannelStartInterruptImmunities? Immunities;
    public SpellHealPrediction? HealPrediction;
    public SpellEmpowerStart() : base(ServerOpcodes.SpellEmpowerStart, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(CastID);
        _worldPacket.WritePackedGuid(Caster);
        _worldPacket.WriteUInt32((uint)Targets.Count);
        _worldPacket.Write(SpellID);
        Visual.Write(_worldPacket);
        _worldPacket.Write(Duration);
        _worldPacket.Write(FirstStageDuration);
        _worldPacket.Write(FinalStageDuration);
        _worldPacket.WriteUInt32((uint)StageDurations.Count);

        foreach (var target in Targets)
            _worldPacket.Write(target);

        foreach (var val in StageDurations.Values)
            _worldPacket.Write(val);

        _worldPacket.Write(Immunities.HasValue);
        _worldPacket.Write(HealPrediction.HasValue);

        if (Immunities.HasValue)
            Immunities.Value.Write(_worldPacket);

        if (HealPrediction.HasValue)
            HealPrediction.Value.Write(_worldPacket);
    }
}