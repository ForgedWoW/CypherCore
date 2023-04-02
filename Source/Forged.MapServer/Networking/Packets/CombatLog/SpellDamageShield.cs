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
        WorldPacket.WritePackedGuid(Attacker);
        WorldPacket.WritePackedGuid(Defender);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteUInt32(TotalDamage);
        WorldPacket.WriteInt32(OriginalDamage);
        WorldPacket.WriteUInt32(OverKill);
        WorldPacket.WriteUInt32(SchoolMask);
        WorldPacket.WriteUInt32(LogAbsorbed);

        WriteLogDataBit();
        FlushBits();
        WriteLogData();
    }
}