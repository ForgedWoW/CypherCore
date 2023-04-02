// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Talent;

internal class LearnTalentFailed : ServerPacket
{
    public uint Reason;
    public int SpellID;
    public List<ushort> Talents = new();
    public LearnTalentFailed() : base(ServerOpcodes.LearnTalentFailed) { }

    public override void Write()
    {
        WorldPacket.WriteBits(Reason, 4);
        WorldPacket.WriteInt32(SpellID);
        WorldPacket.WriteInt32(Talents.Count);

        foreach (var talent in Talents)
            WorldPacket.WriteUInt16(talent);
    }
}