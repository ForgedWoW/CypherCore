// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Spell;

public class CancelCast : ClientPacket
{
    public ObjectGuid CastID;
    public uint SpellID;
    public CancelCast(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        CastID = _worldPacket.ReadPackedGuid();
        SpellID = _worldPacket.ReadUInt32();
    }
}