// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Spell;

public class PetCastSpell : ClientPacket
{
    public SpellCastRequest Cast;
    public ObjectGuid PetGUID;
    public PetCastSpell(WorldPacket packet) : base(packet)
    {
        Cast = new SpellCastRequest();
    }

    public override void Read()
    {
        PetGUID = _worldPacket.ReadPackedGuid();
        Cast.Read(_worldPacket);
    }
}