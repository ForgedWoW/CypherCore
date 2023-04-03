// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Mails;

public class MailReceiver
{
    private readonly Player m_receiver;
    private readonly ulong m_receiver_lowguid;

    public MailReceiver(ulong receiver_lowguid)
    {
        m_receiver = null;
        m_receiver_lowguid = receiver_lowguid;
    }

    public MailReceiver(Player receiver)
    {
        m_receiver = receiver;
        m_receiver_lowguid = receiver.GUID.Counter;
    }

    public MailReceiver(Player receiver, ulong receiver_lowguid)
    {
        m_receiver = receiver;
        m_receiver_lowguid = receiver_lowguid;
    }

    public MailReceiver(Player receiver, ObjectGuid receiverGuid)
    {
        m_receiver = receiver;
        m_receiver_lowguid = receiverGuid.Counter;
    }

    public Player GetPlayer()
    {
        return m_receiver;
    }

    public ulong GetPlayerGUIDLow()
    {
        return m_receiver_lowguid;
    }
}