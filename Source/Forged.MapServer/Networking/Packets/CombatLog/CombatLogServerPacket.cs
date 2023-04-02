// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

public class CombatLogServerPacket : ServerPacket
{
    internal SpellCastLogData LogData;
    private bool _includeLogData;

    public CombatLogServerPacket(ServerOpcodes opcode, ConnectionType connection = ConnectionType.Realm) : base(opcode, connection)
    {
        LogData = new SpellCastLogData();
    }

    public void FlushBits()
    {
        _worldPacket.FlushBits();
    }

    public void SetAdvancedCombatLogging(bool value)
    {
        _includeLogData = value;
    }

    public override void Write() { }
    public void WriteLogData()
    {
        if (_includeLogData)
            LogData.Write(_worldPacket);
    }

    public void WriteLogDataBit()
    {
        _worldPacket.WriteBit(_includeLogData);
    }
}

//Structs