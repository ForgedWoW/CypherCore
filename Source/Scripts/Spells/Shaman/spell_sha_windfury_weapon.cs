// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 33757 - Windfury Weapon
[SpellScript(33757)]
internal class SpellShaWindfuryWeapon : SpellScript, ISpellOnCast, ISpellCheckCast
{
    Item _item;

    public SpellCastResult CheckCast()
    {
        _item = Caster.AsPlayer.GetWeaponForAttack(WeaponAttackType.BaseAttack, false);

        return _item == null || !_item.Template.IsWeapon ? SpellCastResult.TargetNoWeapons : SpellCastResult.SpellCastOk;
    }


    public override bool Load()
    {
        return Caster.IsPlayer;
    }

    public void OnCast()
    {
        Caster.SpellFactory.CastSpell(_item, ShamanSpells.WINDFURY_ENCHANTMENT, Spell);
    }
}