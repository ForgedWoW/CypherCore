// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellEmpowerStageUpdate : ServerPacket
{
    public ObjectGuid Caster;
    public ObjectGuid CastID;
    public List<uint> RemainingStageDurations = new();
    public int TimeRemaining;
    public bool Unk;
    public SpellEmpowerStageUpdate() : base(ServerOpcodes.SpellEmpowerUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(CastID);
        WorldPacket.WritePackedGuid(Caster);
        WorldPacket.Write(TimeRemaining);
        WorldPacket.Write((uint)RemainingStageDurations.Count);
        WorldPacket.Write(Unk);

        foreach (var stageDuration in RemainingStageDurations)
            WorldPacket.Write(stageDuration);
    }
}