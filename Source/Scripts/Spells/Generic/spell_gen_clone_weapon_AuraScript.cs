﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_clone_weapon_AuraScript : AuraScript, IHasAuraEffects
{
    private uint prevItem;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        prevItem = 0;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectRemove));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;
        var target = Target;

        if (!caster)
            return;

        switch (SpellInfo.Id)
        {
            case GenericSpellIds.WeaponAura:
            case GenericSpellIds.Weapon2Aura:
            case GenericSpellIds.Weapon3Aura:
            {
                prevItem = target.GetVirtualItemId(0);

                var player = caster.AsPlayer;

                if (player)
                {
                    var mainItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

                    if (mainItem)
                        target.SetVirtualItem(0, mainItem.Entry);
                }
                else
                {
                    target.SetVirtualItem(0, caster.GetVirtualItemId(0));
                }

                break;
            }
            case GenericSpellIds.OffhandAura:
            case GenericSpellIds.Offhand2Aura:
            {
                prevItem = target.GetVirtualItemId(1);

                var player = caster.AsPlayer;

                if (player)
                {
                    var offItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.OffHand);

                    if (offItem)
                        target.SetVirtualItem(1, offItem.Entry);
                }
                else
                {
                    target.SetVirtualItem(1, caster.GetVirtualItemId(1));
                }

                break;
            }
            case GenericSpellIds.RangedAura:
            {
                prevItem = target.GetVirtualItemId(2);

                var player = caster.AsPlayer;

                if (player)
                {
                    var rangedItem = player.GetItemByPos(InventorySlots.Bag0, EquipmentSlot.MainHand);

                    if (rangedItem)
                        target.SetVirtualItem(2, rangedItem.Entry);
                }
                else
                {
                    target.SetVirtualItem(2, caster.GetVirtualItemId(2));
                }

                break;
            }
            
        }
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;

        switch (SpellInfo.Id)
        {
            case GenericSpellIds.WeaponAura:
            case GenericSpellIds.Weapon2Aura:
            case GenericSpellIds.Weapon3Aura:
                target.SetVirtualItem(0, prevItem);

                break;
            case GenericSpellIds.OffhandAura:
            case GenericSpellIds.Offhand2Aura:
                target.SetVirtualItem(1, prevItem);

                break;
            case GenericSpellIds.RangedAura:
                target.SetVirtualItem(2, prevItem);

                break;
            
        }
    }
}