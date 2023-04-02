// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.GameObject;

internal class GameObjectSetStateLocal : ServerPacket
{
    public ObjectGuid ObjectGUID;
    public byte State;
    public GameObjectSetStateLocal() : base(ServerOpcodes.GameObjectSetStateLocal, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ObjectGUID);
        WorldPacket.WriteUInt8(State);
    }
}