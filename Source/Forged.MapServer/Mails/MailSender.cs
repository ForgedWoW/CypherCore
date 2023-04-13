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
    public MailSender(MailMessageType messageType, ulong senderGuidlowOrEntry, MailStationery stationery = MailStationery.Default)
    {
        MailMessageType = messageType;
        SenderId = senderGuidlowOrEntry;
        Stationery = stationery;
    }

    public MailSender(WorldObject sender, MailStationery stationery = MailStationery.Default)
    {
        Stationery = stationery;

        switch (sender.TypeId)
        {
            case TypeId.Unit:
                MailMessageType = MailMessageType.Creature;
                SenderId = sender.Entry;

                break;
            case TypeId.GameObject:
                MailMessageType = MailMessageType.Gameobject;
                SenderId = sender.Entry;

                break;
            case TypeId.Player:
                MailMessageType = MailMessageType.Normal;
                SenderId = sender.GUID.Counter;

                break;
            default:
                MailMessageType = MailMessageType.Normal;
                SenderId = 0; // will show mail from not existed player
                Log.Logger.Error("MailSender:MailSender - Mail have unexpected sender typeid ({0})", sender.TypeId);

                break;
        }
    }

    public MailSender(CalendarEvent sender)
    {
        MailMessageType = MailMessageType.Calendar;
        SenderId = (uint)sender.EventId;
        Stationery = MailStationery.Default;
    }

    public MailSender(AuctionHouseObject sender)
    {
        MailMessageType = MailMessageType.Auction;
        SenderId = sender.GetAuctionHouseId();
        Stationery = MailStationery.Auction;
    }

    public MailSender(BlackMarketEntry sender)
    {
        MailMessageType = MailMessageType.Blackmarket;
        SenderId = sender.GetTemplate().SellerNPC;
        Stationery = MailStationery.Auction;
    }

    public MailSender(Player sender)
    {
        MailMessageType = MailMessageType.Normal;
        Stationery = sender.IsGameMaster ? MailStationery.Gm : MailStationery.Default;
        SenderId = sender.GUID.Counter;
    }

    public MailSender(uint senderEntry)
    {
        MailMessageType = MailMessageType.Creature;
        SenderId = senderEntry;
        Stationery = MailStationery.Default;
    }

    public MailMessageType MailMessageType { get; }

    public ulong SenderId { get; }

    public MailStationery Stationery { get; }
}