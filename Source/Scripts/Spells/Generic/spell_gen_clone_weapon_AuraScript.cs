// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenCloneWeaponAuraScript : AuraScript, IHasAuraEffects
{
    private uint _prevItem;
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        _prevItem = 0;

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
            case GenericSpellIds.WEAPON_AURA:
            case GenericSpellIds.WEAPON2_AURA:
            case GenericSpellIds.WEAPON3_AURA:
            {
                _prevItem = target.GetVirtualItemId(0);

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
            case GenericSpellIds.OFFHAND_AURA:
            case GenericSpellIds.OFFHAND2_AURA:
            {
                _prevItem = target.GetVirtualItemId(1);

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
            case GenericSpellIds.RANGED_AURA:
            {
                _prevItem = target.GetVirtualItemId(2);

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
            case GenericSpellIds.WEAPON_AURA:
            case GenericSpellIds.WEAPON2_AURA:
            case GenericSpellIds.WEAPON3_AURA:
                target.SetVirtualItem(0, _prevItem);

                break;
            case GenericSpellIds.OFFHAND_AURA:
            case GenericSpellIds.OFFHAND2_AURA:
                target.SetVirtualItem(1, _prevItem);

                break;
            case GenericSpellIds.RANGED_AURA:
                target.SetVirtualItem(2, _prevItem);

                break;
        }
    }
}