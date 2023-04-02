// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class LearnedSpells : ServerPacket
{
    public List<LearnedSpellInfo> ClientLearnedSpellData = new();
    public uint SpecializationID;
    public bool SuppressMessaging;
    public LearnedSpells() : base(ServerOpcodes.LearnedSpells, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteInt32(ClientLearnedSpellData.Count);
        WorldPacket.WriteUInt32(SpecializationID);
        WorldPacket.WriteBit(SuppressMessaging);
        WorldPacket.FlushBits();

        foreach (var spell in ClientLearnedSpellData)
            spell.Write(WorldPacket);
    }
}