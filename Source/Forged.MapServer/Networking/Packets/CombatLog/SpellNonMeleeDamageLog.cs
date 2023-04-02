﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;

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
    public SpellNonMeleeDamageLog() : base(ServerOpcodes.SpellNonMeleeDamageLog, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Me);
        _worldPacket.WritePackedGuid(CasterGUID);
        _worldPacket.WritePackedGuid(CastID);
        _worldPacket.WriteInt32(SpellID);
        Visual.Write(_worldPacket);
        _worldPacket.WriteInt32(Damage);
        _worldPacket.WriteInt32(OriginalDamage);
        _worldPacket.WriteInt32(Overkill);
        _worldPacket.WriteUInt8(SchoolMask);
        _worldPacket.WriteInt32(Absorbed);
        _worldPacket.WriteInt32(Resisted);
        _worldPacket.WriteInt32(ShieldBlock);

        _worldPacket.WriteBit(Periodic);
        _worldPacket.WriteBits(Flags, 7);
        _worldPacket.WriteBit(false); // Debug info
        WriteLogDataBit();
        _worldPacket.WriteBit(ContentTuning != null);
        FlushBits();
        WriteLogData();

        ContentTuning?.Write(_worldPacket);
    }
}