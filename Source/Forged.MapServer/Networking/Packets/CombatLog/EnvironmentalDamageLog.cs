// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class EnvironmentalDamageLog : CombatLogServerPacket
{
    public int Absorbed;
    public int Amount;
    public int Resisted;
    public EnviromentalDamage Type;
    public ObjectGuid Victim;
    public EnvironmentalDamageLog() : base(ServerOpcodes.EnvironmentalDamageLog) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(Victim);
        WorldPacket.WriteUInt8((byte)Type);
        WorldPacket.WriteInt32(Amount);
        WorldPacket.WriteInt32(Resisted);
        WorldPacket.WriteInt32(Absorbed);

        WriteLogDataBit();
        FlushBits();
        WriteLogData();
    }
}