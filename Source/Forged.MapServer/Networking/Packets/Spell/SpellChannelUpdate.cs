// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellChannelUpdate : ServerPacket
{
    public ObjectGuid CasterGUID;
    public int TimeRemaining;
    public SpellChannelUpdate() : base(ServerOpcodes.SpellChannelUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(CasterGUID);
        _worldPacket.WriteInt32(TimeRemaining);
    }
}