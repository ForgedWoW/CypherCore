﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellPeriodicAuraLog : CombatLogServerPacket
{
    public ObjectGuid CasterGUID;
    public List<SpellLogEffect> Effects = new();
    public uint SpellID;
    public ObjectGuid TargetGUID;
    public SpellPeriodicAuraLog() : base(ServerOpcodes.SpellPeriodicAuraLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(TargetGUID);
        WorldPacket.WritePackedGuid(CasterGUID);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteInt32(Effects.Count);
        WriteLogDataBit();
        FlushBits();

        Effects.ForEach(p => p.Write(WorldPacket));

        WriteLogData();
    }

    public struct PeriodicalAuraLogEffectDebugInfo
    {
        public float CritRollMade;
        public float CritRollNeeded;
    }

    public class SpellLogEffect
    {
        public uint AbsorbedOrAmplitude;
        public uint Amount;
        public ContentTuningParams ContentTuning;
        public bool Crit;
        public PeriodicalAuraLogEffectDebugInfo? DebugInfo;
        public uint Effect;
        public int OriginalDamage;
        public uint OverHealOrKill;
        public uint Resisted;
        public uint SchoolMaskOrPower;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Effect);
            data.WriteUInt32(Amount);
            data.WriteInt32(OriginalDamage);
            data.WriteUInt32(OverHealOrKill);
            data.WriteUInt32(SchoolMaskOrPower);
            data.WriteUInt32(AbsorbedOrAmplitude);
            data.WriteUInt32(Resisted);

            data.WriteBit(Crit);
            data.WriteBit(DebugInfo.HasValue);
            data.WriteBit(ContentTuning != null);
            data.FlushBits();

            ContentTuning?.Write(data);

            if (DebugInfo.HasValue)
            {
                data.WriteFloat(DebugInfo.Value.CritRollMade);
                data.WriteFloat(DebugInfo.Value.CritRollNeeded);
            }
        }
    }
}