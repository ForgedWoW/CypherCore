// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Networking.Packets.Toy;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class ToyHandler : IWorldSessionHandler
{
    private readonly CollectionMgr _collectionMgr;
    private readonly DB2Manager _db2Manager;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly WorldSession _session;
    private readonly SpellManager _spellManager;

    public ToyHandler(WorldSession session, CollectionMgr collectionMgr, DB2Manager db2Manager, SpellManager spellManager, ItemTemplateCache itemTemplateCache)
    {
        _session = session;
        _collectionMgr = collectionMgr;
        _db2Manager = db2Manager;
        _spellManager = spellManager;
        _itemTemplateCache = itemTemplateCache;
    }

    [WorldPacketHandler(ClientOpcodes.AddToy)]
    private void HandleAddToy(AddToy packet)
    {
        if (packet.Guid.IsEmpty)
            return;

        var item = _session.Player.GetItemByGuid(packet.Guid);

        if (item == null)
        {
            _session.Player.SendEquipError(InventoryResult.ItemNotFound);

            return;
        }

        if (!_db2Manager.IsToyItem(item.Entry))
            return;

        var msg = _session.Player.CanUseItem(item);

        if (msg != InventoryResult.Ok)
        {
            _session.Player.SendEquipError(msg, item);

            return;
        }

        if (_collectionMgr.AddToy(item.Entry, false, false))
            _session.Player.DestroyItem(item.BagSlot, item.Slot, true);
    }

    [WorldPacketHandler(ClientOpcodes.ToyClearFanfare)]
    private void HandleToyClearFanfare(ToyClearFanfare toyClearFanfare)
    {
        _collectionMgr.ToyClearFanfare(toyClearFanfare.ItemID);
    }

    [WorldPacketHandler(ClientOpcodes.UseToy, Processing = PacketProcessing.Inplace)]
    private void HandleUseToy(UseToy packet)
    {
        var itemId = packet.Cast.Misc[0];
        var item = _itemTemplateCache.GetItemTemplate(itemId);

        if (item == null)
            return;

        if (!_collectionMgr.HasToy(itemId))
            return;

        var effect = item.Effects.Find(eff => packet.Cast.SpellID == eff.SpellID);

        if (effect == null)
            return;

        var spellInfo = _spellManager.GetSpellInfo(packet.Cast.SpellID);

        if (spellInfo == null)
        {
            Log.Logger.Error("HandleUseToy: unknown spell id: {0} used by Toy Item entry {1}", packet.Cast.SpellID, itemId);

            return;
        }

        if (_session.Player.IsPossessing)
            return;

        SpellCastTargets targets = new(_session.Player, packet.Cast);

        var spell = _session.Player.SpellFactory.NewSpell(spellInfo, TriggerCastFlags.None);

        SpellPrepare spellPrepare = new()
        {
            ClientCastID = packet.Cast.CastID,
            ServerCastID = spell.CastId
        };

        _session.SendPacket(spellPrepare);

        spell.FromClient = true;
        spell.CastItemEntry = itemId;
        spell.SpellMisc.Data0 = packet.Cast.Misc[0];
        spell.SpellMisc.Data1 = packet.Cast.Misc[1];
        spell.CastFlagsEx |= SpellCastFlagsEx.UseToySpell;
        spell.Prepare(targets);
    }
}