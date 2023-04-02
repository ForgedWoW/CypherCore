// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellMissLog : ServerPacket
{
    public ObjectGuid Caster;
    public List<SpellLogMissEntry> Entries = new();
    public uint SpellID;
    public SpellMissLog() : base(ServerOpcodes.SpellMissLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.WriteInt32(Entries.Count);

        foreach (var missEntry in Entries)
            missEntry.Write(WorldPacket);
    }
}