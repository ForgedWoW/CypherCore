// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellDispellLog : ServerPacket
{
    public ObjectGuid CasterGUID;
    public List<SpellDispellData> DispellData = new();
    public uint DispelledBySpellID;
    public bool IsBreak;
    public bool IsSteal;
    public ObjectGuid TargetGUID;
    public SpellDispellLog() : base(ServerOpcodes.SpellDispellLog, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteBit(IsSteal);
        WorldPacket.WriteBit(IsBreak);
        WorldPacket.WritePackedGuid(TargetGUID);
        WorldPacket.WritePackedGuid(CasterGUID);
        WorldPacket.WriteUInt32(DispelledBySpellID);

        WorldPacket.WriteInt32(DispellData.Count);

        foreach (var data in DispellData)
        {
            WorldPacket.WriteUInt32(data.SpellID);
            WorldPacket.WriteBit(data.Harmful);
            WorldPacket.WriteBit(data.Rolled.HasValue);
            WorldPacket.WriteBit(data.Needed.HasValue);

            if (data.Rolled.HasValue)
                WorldPacket.WriteInt32(data.Rolled.Value);

            if (data.Needed.HasValue)
                WorldPacket.WriteInt32(data.Needed.Value);

            WorldPacket.FlushBits();
        }
    }
}