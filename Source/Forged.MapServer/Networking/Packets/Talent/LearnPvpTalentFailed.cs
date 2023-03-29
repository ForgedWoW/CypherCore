// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Talent;

internal class LearnPvpTalentFailed : ServerPacket
{
    public uint Reason;
    public uint SpellID;
    public List<PvPTalent> Talents = new();
    public LearnPvpTalentFailed() : base(ServerOpcodes.LearnPvpTalentFailed) { }

    public override void Write()
    {
        _worldPacket.WriteBits(Reason, 4);
        _worldPacket.WriteUInt32(SpellID);
        _worldPacket.WriteInt32(Talents.Count);

        foreach (var pvpTalent in Talents)
            pvpTalent.Write(_worldPacket);
    }
}