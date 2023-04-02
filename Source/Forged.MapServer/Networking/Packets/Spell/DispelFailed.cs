// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class DispelFailed : ServerPacket
{
    public ObjectGuid CasterGUID;
    public List<uint> FailedSpells = new();
    public uint SpellID;
    public ObjectGuid VictimGUID;
    public DispelFailed() : base(ServerOpcodes.DispelFailed) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(CasterGUID);
        WorldPacket.WritePackedGuid(VictimGUID);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteInt32(FailedSpells.Count);

        FailedSpells.ForEach(FailedSpellID => WorldPacket.WriteUInt32(FailedSpellID));
    }
}