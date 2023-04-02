// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.GameObject;

internal class GameObjectActivateAnimKit : ServerPacket
{
    public int AnimKitID;
    public bool Maintain;
    public ObjectGuid ObjectGUID;
    public GameObjectActivateAnimKit() : base(ServerOpcodes.GameObjectActivateAnimKit, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(ObjectGUID);
        _worldPacket.WriteInt32(AnimKitID);
        _worldPacket.WriteBit(Maintain);
        _worldPacket.FlushBits();
    }
}