using System.Collections.Generic;
using Game.Entities;
using Game.Networking;

namespace Game.Maps;

public class PacketSenderRef : IDoWork<Player>
{
    ServerPacket Data;

    public PacketSenderRef(ServerPacket message)
    {
        Data = message;
    }

    public virtual void Invoke(Player player)
    {
        player.SendPacket(Data);
    }
}