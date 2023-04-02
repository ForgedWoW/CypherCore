// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellEnergizeLog : CombatLogServerPacket
{
    public int Amount;
    public ObjectGuid CasterGUID;
    public int OverEnergize;
    public uint SpellID;
    public ObjectGuid TargetGUID;
    public PowerType Type;
    public SpellEnergizeLog() : base(ServerOpcodes.SpellEnergizeLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(TargetGUID);
        WorldPacket.WritePackedGuid(CasterGUID);

        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteUInt32((uint)Type);
        WorldPacket.WriteInt32(Amount);
        WorldPacket.WriteInt32(OverEnergize);

        WriteLogDataBit();
        FlushBits();
        WriteLogData();
    }
}