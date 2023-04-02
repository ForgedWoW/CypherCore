// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Spell;

namespace Forged.MapServer.Networking.Packets.Toy;

internal class UseToy : ClientPacket
{
    public SpellCastRequest Cast = new();
    public UseToy(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        Cast.Read(WorldPacket);
    }
}