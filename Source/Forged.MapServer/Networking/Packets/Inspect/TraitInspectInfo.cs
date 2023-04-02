// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.Trait;

namespace Forged.MapServer.Networking.Packets.Inspect;

public struct TraitInspectInfo
{
    public int ChrSpecializationID;
    public TraitConfigPacket Config;
    public int Level;
    public void Write(WorldPacket data)
    {
        data.WriteInt32(Level);
        data.WriteInt32(ChrSpecializationID);

        Config?.Write(data);
    }
}