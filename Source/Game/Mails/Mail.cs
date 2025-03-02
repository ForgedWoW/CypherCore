﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.BlackMarket;
using Game.Entities;

namespace Game.Mails;

public class Mail
{
	public ulong messageID;
	public MailMessageType messageType;
	public MailStationery stationery;
	public uint mailTemplateId;
	public ulong sender;
	public ulong receiver;
	public string subject;
	public string body;
	public List<MailItemInfo> items = new();
	public List<ulong> removedItems = new();
	public long expire_time;
	public long deliver_time;
	public ulong money;
	public ulong COD;
	public MailCheckMask checkMask;
	public MailState state;

	public void AddItem(ulong itemGuidLow, uint item_template)
	{
		MailItemInfo mii = new();
		mii.item_guid = itemGuidLow;
		mii.item_template = item_template;
		items.Add(mii);
	}

	public bool RemoveItem(ulong itemGuid)
	{
		foreach (var item in items)
			if (item.item_guid == itemGuid)
			{
				items.Remove(item);

				return true;
			}

		return false;
	}

	public bool HasItems()
	{
		return !items.Empty();
	}
}

public class MailItemInfo
{
	public ulong item_guid;
	public uint item_template;
}

public class MailReceiver
{
	readonly Player m_receiver;
	readonly ulong m_receiver_lowguid;

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

public class MailSender
{
	readonly MailMessageType m_messageType;
	readonly ulong m_senderId; // player low guid or other object entry
	readonly MailStationery m_stationery;

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
				Log.outError(LogFilter.Server, "MailSender:MailSender - Mail have unexpected sender typeid ({0})", sender.TypeId);

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