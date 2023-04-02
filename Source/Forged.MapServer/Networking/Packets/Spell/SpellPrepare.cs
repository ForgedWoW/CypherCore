// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class SpellPrepare : ServerPacket
{
    public ObjectGuid ClientCastID;
    public ObjectGuid ServerCastID;
    public SpellPrepare() : base(ServerOpcodes.SpellPrepare) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ClientCastID);
        WorldPacket.WritePackedGuid(ServerCastID);
    }
}