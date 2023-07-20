// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Trade;
using Forged.MapServer.OpCodeHandlers;
using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class TradeData
{
    private readonly ObjectGuid[] _items = new ObjectGuid[(int)TradeSlots.Count];
    private readonly Player _player;
    private ObjectGuid _spellCastItem;

    public TradeData(Player player, Player trader)
    {
        _player = player;
        Trader = trader;
        ClientStateIndex = 1;
        ServerStateIndex = 1;
    }

    public uint ClientStateIndex { get; private set; }

    public bool HasSpellCastItem => !_spellCastItem.IsEmpty;
    public bool IsAccepted { get; private set; }
    public bool IsInAcceptProcess { get; private set; }
    public ulong Money { get; private set; }

    public uint ServerStateIndex { get; private set; }

    public uint Spell { get; private set; }

    public Item SpellCastItem => !_spellCastItem.IsEmpty ? _player.GetItemByGuid(_spellCastItem) : null;

    public Player Trader { get; }

    public TradeData TraderData => Trader.TradeData;
    public Item GetItem(TradeSlots slot)
    {
        return !_items[(int)slot].IsEmpty ? _player.GetItemByGuid(_items[(int)slot]) : null;
    }

    public TradeSlots GetTradeSlotForItem(ObjectGuid itemGuid)
    {
        for (TradeSlots i = 0; i < TradeSlots.Count; ++i)
            if (_items[(int)i] == itemGuid)
                return i;

        return TradeSlots.Invalid;
    }

    public bool HasItem(ObjectGuid itemGuid)
    {
        for (byte i = 0; i < (byte)TradeSlots.Count; ++i)
            if (_items[i] == itemGuid)
                return true;

        return false;
    }

    public void SetAccepted(bool state, bool crosssend = false)
    {
        IsAccepted = state;

        if (!state)
        {
            TradeStatusPkt info = new()
            {
                Status = TradeStatus.Unaccepted
            };

            if (crosssend)
                Trader.Session.PacketRouter.OpCodeHandler<TradeHandler>().SendTradeStatus(info);
            else
                _player.Session.PacketRouter.OpCodeHandler<TradeHandler>().SendTradeStatus(info);
        }
    }

    public void SetInAcceptProcess(bool state)
    {
        IsInAcceptProcess = state;
    }

    public void SetItem(TradeSlots slot, Item item, bool update = false)
    {
        var itemGuid = item?.GUID ?? ObjectGuid.Empty;

        if (_items[(int)slot] == itemGuid && !update)
            return;

        _items[(int)slot] = itemGuid;

        SetAccepted(false);
        TraderData.SetAccepted(false);

        UpdateServerStateIndex();

        Update();

        // need remove possible trader spell applied to changed item
        if (slot == TradeSlots.NonTraded)
            TraderData.SetSpell(0);

        // need remove possible player spell applied (possible move reagent)
        SetSpell(0);
    }

    public void SetMoney(ulong money)
    {
        if (Money == money)
            return;

        if (!_player.HasEnoughMoney(money))
        {
            TradeStatusPkt info = new()
            {
                Status = TradeStatus.Failed,
                BagResult = InventoryResult.NotEnoughMoney
            };

            _player.Session.PacketRouter.OpCodeHandler<TradeHandler>().SendTradeStatus(info);

            return;
        }

        Money = money;

        SetAccepted(false);
        TraderData.SetAccepted(false);

        UpdateServerStateIndex();

        Update();
    }

    public void SetSpell(uint spellID, Item castItem = null)
    {
        var itemGuid = castItem != null ? castItem.GUID : ObjectGuid.Empty;

        if (Spell == spellID && _spellCastItem == itemGuid)
            return;

        Spell = spellID;
        _spellCastItem = itemGuid;

        SetAccepted(false);
        TraderData.SetAccepted(false);

        UpdateServerStateIndex();

        Update();      // send spell info to item owner
        Update(false); // send spell info to caster self
    }

    public void UpdateClientStateIndex()
    {
        ++ClientStateIndex;
    }

    public void UpdateServerStateIndex()
    {
        ServerStateIndex = RandomHelper.Rand32();
    }

    private void Update(bool forTarget = true)
    {
        if (forTarget)
            Trader.Session.PacketRouter.OpCodeHandler<TradeHandler>().SendUpdateTrade(true); // player state for trader
        else
            _player.Session.PacketRouter.OpCodeHandler<TradeHandler>().SendUpdateTrade(false); // player state for player
    }
}