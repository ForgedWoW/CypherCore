// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Networking.Packets.Toy;
using Forged.MapServer.Spells;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.Handlers;

public class ToyHandler : IWorldSessionHandler
{
	[WorldPacketHandler(ClientOpcodes.AddToy)]
	void HandleAddToy(AddToy packet)
	{
		if (packet.Guid.IsEmpty)
			return;

		var item = _player.GetItemByGuid(packet.Guid);

		if (!item)
		{
			_player.SendEquipError(InventoryResult.ItemNotFound);

			return;
		}

		if (!Global.DB2Mgr.IsToyItem(item.Entry))
			return;

		var msg = _player.CanUseItem(item);

		if (msg != InventoryResult.Ok)
		{
			_player.SendEquipError(msg, item);

			return;
		}

		if (_collectionMgr.AddToy(item.Entry, false, false))
			_player.DestroyItem(item.BagSlot, item.Slot, true);
	}

	[WorldPacketHandler(ClientOpcodes.UseToy, Processing = PacketProcessing.Inplace)]
	void HandleUseToy(UseToy packet)
	{
		var itemId = packet.Cast.Misc[0];
		var item = Global.ObjectMgr.GetItemTemplate(itemId);

		if (item == null)
			return;

		if (!_collectionMgr.HasToy(itemId))
			return;

		var effect = item.Effects.Find(eff => packet.Cast.SpellID == eff.SpellID);

		if (effect == null)
			return;

		var spellInfo = Global.SpellMgr.GetSpellInfo(packet.Cast.SpellID, Difficulty.None);

		if (spellInfo == null)
		{
			Log.Logger.Error("HandleUseToy: unknown spell id: {0} used by Toy Item entry {1}", packet.Cast.SpellID, itemId);

			return;
		}

		if (_player.IsPossessing)
			return;

		SpellCastTargets targets = new(_player, packet.Cast);

		Spell spell = new(_player, spellInfo, TriggerCastFlags.None);

		SpellPrepare spellPrepare = new();
		spellPrepare.ClientCastID = packet.Cast.CastID;
		spellPrepare.ServerCastID = spell.CastId;
		SendPacket(spellPrepare);

		spell.FromClient = true;
		spell.CastItemEntry = itemId;
		spell.SpellMisc.Data0 = packet.Cast.Misc[0];
		spell.SpellMisc.Data1 = packet.Cast.Misc[1];
		spell.CastFlagsEx |= SpellCastFlagsEx.UseToySpell;
		spell.Prepare(targets);
	}

	[WorldPacketHandler(ClientOpcodes.ToyClearFanfare)]
	void HandleToyClearFanfare(ToyClearFanfare toyClearFanfare)
	{
		_collectionMgr.ToyClearFanfare(toyClearFanfare.ItemID);
	}
}