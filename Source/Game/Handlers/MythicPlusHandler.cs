// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets.MythicPlus;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.RequestMythicPlusSeasonData)]
        void RequestMythicPlusSeasonData(ClientPacket packet)
        {
            SendPacket(new MythicPlusSeasonData());
        }
    }
}
