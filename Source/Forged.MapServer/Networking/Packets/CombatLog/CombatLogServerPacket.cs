﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Discord;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Spell;
using Framework.Constants;
using Framework.IO;

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
        WorldPacket.FlushBits();
    }

    public void SetAdvancedCombatLogging(bool value)
    {
        _includeLogData = value;
    }

    public override void Write() { }

    public void WriteLogData()
    {
        if (_includeLogData)
            LogData.Write(WorldPacket);
    }

    public void WriteLogDataBit()
    {
        WorldPacket.WriteBit(_includeLogData);
    }
}

//Structs