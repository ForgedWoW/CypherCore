// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking.Packets;

namespace Forged.RealmServer.Entities;

public class TradeData
{
	readonly Player _player;
	readonly Player _trader;
	readonly ObjectGuid[] _items = new ObjectGuid[(int)TradeSlots.Count];
	bool _accepted;
	bool _acceptProccess;
	ulong _money;
	uint _spell;
	ObjectGuid _spellCastItem;
	uint _clientStateIndex;
	uint _serverStateIndex;

	public TradeData(Player player, Player trader)
	{
		_player = player;
		_trader = trader;
		_clientStateIndex = 1;
		_serverStateIndex = 1;
	}

	public TradeData GetTraderData()
	{
		return _trader.GetTradeData();
	}

	public Item GetItem(TradeSlots slot)
	{
		return !_items[(int)slot].IsEmpty ? _player.GetItemByGuid(_items[(int)slot]) : null;
	}

	public bool HasItem(ObjectGuid itemGuid)
	{
		for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
			if (_items[i] == itemGuid)
				return true;

		return false;
	}

	public TradeSlots GetTradeSlotForItem(ObjectGuid itemGuid)
	{
		for (TradeSlots i = 0; i < TradeSlots.Count; ++i)
			if (_items[(int)i] == itemGuid)
				return i;

		return TradeSlots.Invalid;
	}

	public uint GetSpell()
	{
		return _spell;
	}

	public Player GetTrader()
	{
		return _trader;
	}

	public bool HasSpellCastItem()
	{
		return !_spellCastItem.IsEmpty;
	}

	public ulong GetMoney()
	{
		return _money;
	}

	public bool IsAccepted()
	{
		return _accepted;
	}

	public bool IsInAcceptProcess()
	{
		return _acceptProccess;
	}

	public void SetInAcceptProcess(bool state)
	{
		_acceptProccess = state;
	}

	public uint GetClientStateIndex()
	{
		return _clientStateIndex;
	}

	public void UpdateClientStateIndex()
	{
		++_clientStateIndex;
	}

	public uint GetServerStateIndex()
	{
		return _serverStateIndex;
	}

	public void UpdateServerStateIndex()
	{
		_serverStateIndex = RandomHelper.Rand32();
	}
}