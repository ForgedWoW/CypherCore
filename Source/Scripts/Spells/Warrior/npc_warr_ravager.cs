// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

[CreatureScript(76168)] // Ravager - 76168
public class NPCWarrRavager : ScriptedAI
{
    public const uint RAVAGER_DISPLAYID = 55644;
    public const uint RAVAGER_VISUAL = 153709;

    public NPCWarrRavager(Creature creature) : base(creature) { }

    public override void IsSummonedBy(WorldObject summoner)
    {
        Me.SetDisplayId(RAVAGER_DISPLAYID);
        Me.SpellFactory.CastSpell(Me, RAVAGER_VISUAL, true);
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
                var lProto = Global.ObjectMgr.GetItemTemplate(item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

                if (lProto != null)
                    Me.SetVirtualItem(0, lProto.Id);
            }
            else
                Me.SetVirtualItem(0, item.Template.Id);

            item = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

            if (item != null)
            {
                var lProto = Global.ObjectMgr.GetItemTemplate(item.GetModifier(ItemModifier.TransmogAppearanceAllSpecs));

                if (lProto != null)
                    Me.SetVirtualItem(2, lProto.Id);
            }
            else
                Me.SetVirtualItem(2, item.Template.Id);
        }
    }
}