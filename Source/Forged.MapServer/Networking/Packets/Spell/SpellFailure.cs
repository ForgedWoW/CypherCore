// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellFailure : ServerPacket
{
    public ObjectGuid CasterUnit;
    public ObjectGuid CastID;
    public ushort Reason;
    public uint SpellID;
    public SpellCastVisual Visual;
    public SpellFailure() : base(ServerOpcodes.SpellFailure, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(CasterUnit);
        WorldPacket.WritePackedGuid(CastID);
        WorldPacket.WriteUInt32(SpellID);

        Visual.Write(WorldPacket);

        WorldPacket.WriteUInt16(Reason);
    }
}