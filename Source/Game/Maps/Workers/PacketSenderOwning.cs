using System.Collections.Generic;
using Game.Entities;
using Game.Networking;

namespace Game.Maps;

public class PacketSenderOwning<T> : IDoWork<Player> where T : ServerPacket, new()
{
    public T Data = new();

    public void Invoke(Player player)
    {
        player.SendPacket(Data);
    }
}