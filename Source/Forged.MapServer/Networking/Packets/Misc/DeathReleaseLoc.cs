// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class DeathReleaseLoc : ServerPacket
{
    public WorldLocation Loc;
    public int MapID;
    public DeathReleaseLoc() : base(ServerOpcodes.DeathReleaseLoc) { }

    public override void Write()
    {
        _worldPacket.WriteInt32(MapID);
        _worldPacket.WriteXYZ(Loc);
    }
}