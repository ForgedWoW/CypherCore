// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Warrior;

[CreatureScript(76168)] // Ravager - 76168
public class npc_warr_ravager : ScriptedAI
{
	public const uint RAVAGER_DISPLAYID = 55644;
	public const uint RAVAGER_VISUAL = 153709;

	public npc_warr_ravager(Creature creature) : base(creature) { }

	public override void IsSummonedBy(WorldObject summoner)
	{
		Me.SetDisplayId(RAVAGER_DISPLAYID);
		Me.CastSpell(Me, RAVAGER_VISUAL, true);
		Me.ReactState = ReactStates.Passive;
		Me.AddUnitState(UnitState.Root);
		Me.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.CanSwim | UnitFlags.PlayerControlled);

		if (summoner == null || !summoner.IsPlayer)
			return;

		var player = summoner.AsPlayer;

		if (player != null)
		{
			var item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

			if (item != null)
			{
				var l_Proto = Global.ObjectMgr.GetItemTemplate(item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

				if (l_Proto != null)
					Me.SetVirtualItem(0, l_Proto.Id);
			}
			else
			{
				Me.SetVirtualItem(0, item.Template.Id);
			}

			item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

			if (item != null)
			{
				var l_Proto = Global.ObjectMgr.GetItemTemplate(item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

				if (l_Proto != null)
					Me.SetVirtualItem(2, l_Proto.Id);
			}
			else
			{
				Me.SetVirtualItem(2, item.Template.Id);
			}
		}
	}
}