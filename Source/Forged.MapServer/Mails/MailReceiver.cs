// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Mails;

public class MailReceiver
{
    private readonly Player _receiver;
    private readonly ulong _receiverLowguid;

    public MailReceiver(ulong receiverLowguid)
    {
        _receiver = null;
        _receiverLowguid = receiverLowguid;
    }

    public MailReceiver(Player receiver)
    {
        _receiver = receiver;
        _receiverLowguid = receiver.GUID.Counter;
    }

    public MailReceiver(Player receiver, ulong receiverLowguid)
    {
        _receiver = receiver;
        _receiverLowguid = receiverLowguid;
    }

    public MailReceiver(Player receiver, ObjectGuid receiverGuid)
    {
        _receiver = receiver;
        _receiverLowguid = receiverGuid.Counter;
    }

    public Player GetPlayer()
    {
        return _receiver;
    }

    public ulong GetPlayerGUIDLow()
    {
        return _receiverLowguid;
    }
}