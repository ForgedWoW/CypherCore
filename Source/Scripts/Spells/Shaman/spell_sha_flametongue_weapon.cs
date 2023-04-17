// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 318038 - Flametongue Weapon
[SpellScript(318038)]
internal class SpellShaFlametongueWeapon : SpellScript, ISpellOnCast, ISpellCheckCast
{
    Item _item;

    public SpellCastResult CheckCast()
    {
        var player = Caster.AsPlayer;
        var slot = EquipmentSlot.MainHand;

        if (player.GetPrimarySpecialization() == TalentSpecialization.ShamanEnhancement)
            slot = EquipmentSlot.OffHand;

        _item = player.GetItemByPos(InventorySlots.Bag0, slot);

        return _item == null || !_item.Template.IsWeapon ? SpellCastResult.TargetNoWeapons : SpellCastResult.SpellCastOk;
    }


    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player);
    }

    public void OnCast()
    {
        Caster.SpellFactory.CastSpell(_item, ShamanSpells.FlametongueWeaponEnchant, true);
    }
}