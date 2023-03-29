// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Item;

public struct UiEventToast
{
    public int UiEventToastID;
    public int Asset;

    public void Write(WorldPacket data)
    {
        data.WriteInt32(UiEventToastID);
        data.WriteInt32(Asset);
    }
}