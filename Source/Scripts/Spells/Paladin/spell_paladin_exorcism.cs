// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

//383185,
[SpellScript(383185)]
public class spell_paladin_exorcism : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var damage = player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
            var dot_damage = (damage * 0.23f * 6);
            HitUnit.CastSpell(HitUnit, PaladinSpells.EXORCISM_DF, damage);

            if (HitUnit.HasAura(26573))
            {
                var targets = new List<Unit>();
                var check = new AnyUnfriendlyUnitInObjectRangeCheck(HitUnit, player, 7);
                var searcher = new UnitListSearcher(HitUnit, targets, check, GridType.All);

                for (var i = targets.GetEnumerator(); i.MoveNext();)
                    HitUnit.CastSpell(i.Current, PaladinSpells.EXORCISM_DF, damage);
            }

            if (HitUnit.CreatureType == CreatureType.Undead || HitUnit.CreatureType == CreatureType.Demon)
                HitUnit.CastSpell(HitUnit, AuraType.ModStun, true);
        }
    }
}