// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;
using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellHealLog : CombatLogServerPacket
{
    public uint Absorbed;
    public ObjectGuid CasterGUID;
    public ContentTuningParams ContentTuning;
    public bool Crit;
    public float? CritRollMade;
    public float? CritRollNeeded;
    public uint Health;
    public int OriginalHeal;
    public uint OverHeal;
    public uint SpellID;
    public ObjectGuid TargetGUID;
    public List<SpellSupportInfo> Supporters;
    public SpellHealLog() : base(ServerOpcodes.SpellHealLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(TargetGUID);
        WorldPacket.WritePackedGuid(CasterGUID);

        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteUInt32(Health);
        WorldPacket.WriteInt32(OriginalHeal);
        WorldPacket.WriteUInt32(OverHeal);
        WorldPacket.WriteUInt32(Absorbed);
        WorldPacket.WriteUInt32((uint)Supporters.Count);

        foreach (var supporter in Supporters)
            supporter.Write(WorldPacket);

        WorldPacket.WriteBit(Crit);

        WorldPacket.WriteBit(CritRollMade.HasValue);
        WorldPacket.WriteBit(CritRollNeeded.HasValue);
        WriteLogDataBit();
        WorldPacket.WriteBit(ContentTuning != null);
        FlushBits();

        WriteLogData();

        if (CritRollMade.HasValue)
            WorldPacket.WriteFloat(CritRollMade.Value);

        if (CritRollNeeded.HasValue)
            WorldPacket.WriteFloat(CritRollNeeded.Value);

        ContentTuning?.Write(WorldPacket);
    }
}