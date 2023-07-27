// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellAbsorbLog : CombatLogServerPacket
{
    public int Absorbed;
    public uint AbsorbedSpellID;
    public uint AbsorbSpellID;
    public ObjectGuid Attacker;
    public ObjectGuid Caster;
    public uint OriginalDamage;
    public bool Unk;
    public ObjectGuid Victim;
    public List<SpellSupportInfo> Supporters;
    public SpellAbsorbLog() : base(ServerOpcodes.SpellAbsorbLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Attacker);
        WorldPacket.WritePackedGuid(Victim);
        WorldPacket.WriteUInt32(AbsorbedSpellID);
        WorldPacket.WriteUInt32(AbsorbSpellID);
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.WriteInt32(Absorbed);
        WorldPacket.WriteUInt32(OriginalDamage);
        WorldPacket.WriteUInt32((uint)Supporters.Count);

        foreach (var supporter in Supporters)
            supporter.Write(WorldPacket);

        WorldPacket.WriteBit(Unk);
        WriteLogDataBit();
        FlushBits();

        WriteLogData();
    }
}