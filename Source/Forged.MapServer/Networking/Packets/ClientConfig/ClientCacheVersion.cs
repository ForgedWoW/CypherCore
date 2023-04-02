// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.ClientConfig;

public class ClientCacheVersion : ServerPacket
{
    public uint CacheVersion = 0;
    public ClientCacheVersion() : base(ServerOpcodes.CacheVersion) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(CacheVersion);
    }
}