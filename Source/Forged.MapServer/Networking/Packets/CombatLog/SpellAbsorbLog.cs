// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
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
    public SpellAbsorbLog() : base(ServerOpcodes.SpellAbsorbLog, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Attacker);
        _worldPacket.WritePackedGuid(Victim);
        _worldPacket.WriteUInt32(AbsorbedSpellID);
        _worldPacket.WriteUInt32(AbsorbSpellID);
        _worldPacket.WritePackedGuid(Caster);
        _worldPacket.WriteInt32(Absorbed);
        _worldPacket.WriteUInt32(OriginalDamage);

        _worldPacket.WriteBit(Unk);
        WriteLogDataBit();
        FlushBits();

        WriteLogData();
    }
}