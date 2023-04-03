// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AuctionHouse;
using Forged.MapServer.BlackMarket;
using Forged.MapServer.Calendar;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Mails;

public class MailSender
{
    private readonly MailMessageType m_messageType;
    private readonly ulong m_senderId; // player low guid or other object entry
    private readonly MailStationery m_stationery;

    public MailSender(MailMessageType messageType, ulong sender_guidlow_or_entry, MailStationery stationery = MailStationery.Default)
    {
        m_messageType = messageType;
        m_senderId = sender_guidlow_or_entry;
        m_stationery = stationery;
    }

    public MailSender(WorldObject sender, MailStationery stationery = MailStationery.Default)
    {
        m_stationery = stationery;

        switch (sender.TypeId)
        {
            case TypeId.Unit:
                m_messageType = MailMessageType.Creature;
                m_senderId = sender.Entry;

                break;
            case TypeId.GameObject:
                m_messageType = MailMessageType.Gameobject;
                m_senderId = sender.Entry;

                break;
            case TypeId.Player:
                m_messageType = MailMessageType.Normal;
                m_senderId = sender.GUID.Counter;

                break;
            default:
                m_messageType = MailMessageType.Normal;
                m_senderId = 0; // will show mail from not existed player
                Log.Logger.Error("MailSender:MailSender - Mail have unexpected sender typeid ({0})", sender.TypeId);

                break;
        }
    }

    public MailSender(CalendarEvent sender)
    {
        m_messageType = MailMessageType.Calendar;
        m_senderId = (uint)sender.EventId;
        m_stationery = MailStationery.Default;
    }

    public MailSender(AuctionHouseObject sender)
    {
        m_messageType = MailMessageType.Auction;
        m_senderId = sender.GetAuctionHouseId();
        m_stationery = MailStationery.Auction;
    }

    public MailSender(BlackMarketEntry sender)
    {
        m_messageType = MailMessageType.Blackmarket;
        m_senderId = sender.GetTemplate().SellerNPC;
        m_stationery = MailStationery.Auction;
    }

    public MailSender(Player sender)
    {
        m_messageType = MailMessageType.Normal;
        m_stationery = sender.IsGameMaster ? MailStationery.Gm : MailStationery.Default;
        m_senderId = sender.GUID.Counter;
    }

    public MailSender(uint senderEntry)
    {
        m_messageType = MailMessageType.Creature;
        m_senderId = senderEntry;
        m_stationery = MailStationery.Default;
    }

    public MailMessageType GetMailMessageType()
    {
        return m_messageType;
    }

    public ulong GetSenderId()
    {
        return m_senderId;
    }

    public MailStationery GetStationery()
    {
        return m_stationery;
    }
}