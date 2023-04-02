// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellCooldownPkt : ServerPacket
{
    public ObjectGuid Caster;
    public SpellCooldownFlags Flags;
    public List<SpellCooldownStruct> SpellCooldowns = new();
    public SpellCooldownPkt() : base(ServerOpcodes.SpellCooldown, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Caster);
        _worldPacket.WriteUInt8((byte)Flags);
        _worldPacket.WriteInt32(SpellCooldowns.Count);
        SpellCooldowns.ForEach(p => p.Write(_worldPacket));
    }
}