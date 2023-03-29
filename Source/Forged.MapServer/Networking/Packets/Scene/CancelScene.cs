// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Scene;

internal class CancelScene : ServerPacket
{
    public uint SceneInstanceID;
    public CancelScene() : base(ServerOpcodes.CancelScene, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteUInt32(SceneInstanceID);
    }
}