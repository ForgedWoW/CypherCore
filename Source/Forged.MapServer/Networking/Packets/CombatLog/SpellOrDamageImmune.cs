// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.CombatLog;

internal class SpellOrDamageImmune : ServerPacket
{
    public ObjectGuid CasterGUID;
    public bool IsPeriodic;
    public uint SpellID;
    public ObjectGuid VictimGUID;
    public SpellOrDamageImmune() : base(ServerOpcodes.SpellOrDamageImmune, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(CasterGUID);
        WorldPacket.WritePackedGuid(VictimGUID);
        WorldPacket.WriteUInt32(SpellID);
        WorldPacket.WriteBit(IsPeriodic);
        WorldPacket.FlushBits();
    }
}