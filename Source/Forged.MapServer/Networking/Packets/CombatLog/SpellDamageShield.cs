// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellDamageShield : CombatLogServerPacket
{
    public ObjectGuid Attacker;
    public ObjectGuid Defender;
    public uint LogAbsorbed;
    public int OriginalDamage;
    public uint OverKill;
    public uint SchoolMask;
    public uint SpellID;
    public uint TotalDamage;
    public SpellDamageShield() : base(ServerOpcodes.SpellDamageShield, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Attacker);
        _worldPacket.WritePackedGuid(Defender);
        _worldPacket.WriteUInt32(SpellID);
        _worldPacket.WriteUInt32(TotalDamage);
        _worldPacket.WriteInt32(OriginalDamage);
        _worldPacket.WriteUInt32(OverKill);
        _worldPacket.WriteUInt32(SchoolMask);
        _worldPacket.WriteUInt32(LogAbsorbed);

        WriteLogDataBit();
        FlushBits();
        WriteLogData();
    }
}