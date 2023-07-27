// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;
using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellNonMeleeDamageLog : CombatLogServerPacket
{
    public int Absorbed;
    public ObjectGuid CasterGUID;

    public ObjectGuid CastID;

    // Optional<SpellNonMeleeDamageLogDebugInfo> DebugInfo;
    public ContentTuningParams ContentTuning;

    public int Damage;
    public int Flags;
    public ObjectGuid Me;
    public int OriginalDamage;
    public int Overkill = -1;
    public bool Periodic;
    public int Resisted;
    public byte SchoolMask;
    public int ShieldBlock;
    public int SpellID;
    public SpellCastVisual Visual;
    public List<CombatWorldTextViewerInfo> WorldTextViewers;
    public List<SpellSupportInfo> Supporters;

    public SpellNonMeleeDamageLog() : base(ServerOpcodes.SpellNonMeleeDamageLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Me);
        WorldPacket.WritePackedGuid(CasterGUID);
        WorldPacket.WritePackedGuid(CastID);
        WorldPacket.WriteInt32(SpellID);
        Visual.Write(WorldPacket);
        WorldPacket.WriteInt32(Damage);
        WorldPacket.WriteInt32(OriginalDamage);
        WorldPacket.WriteInt32(Overkill);
        WorldPacket.WriteUInt8(SchoolMask);
        WorldPacket.WriteInt32(Absorbed);
        WorldPacket.WriteInt32(Resisted);
        WorldPacket.WriteInt32(ShieldBlock);
        WorldPacket.WriteUInt32((uint)WorldTextViewers.Count);
        WorldPacket.WriteUInt32((uint)Supporters.Count);

        foreach (var supporter in Supporters)
            supporter.Write(WorldPacket);

        WorldPacket.WriteBit(Periodic);
        WorldPacket.WriteBits(Flags, 7);
        WorldPacket.WriteBit(false); // Debug info
        WriteLogDataBit();
        WorldPacket.WriteBit(ContentTuning != null);
        FlushBits();

        foreach (var viewer in WorldTextViewers)
            viewer.Write(WorldPacket);

        WriteLogData();

        ContentTuning?.Write(WorldPacket);
    }
}