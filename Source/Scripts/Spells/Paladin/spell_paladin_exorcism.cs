// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin
{
    //383185,
    [SpellScript(383185)]
    public class spell_paladin_exorcism : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Player player = GetCaster().ToPlayer();
            if (player != null)
            {
                var damage = player.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);
                var dot_damage = (damage * 0.23f * 6);
                GetHitUnit().CastSpell(GetHitUnit(), PaladinSpells.EXORCISM_DF, damage);

                if (GetHitUnit().HasAura(26573))
                {
                    List<Unit> targets = new List<Unit>();
                    AnyUnfriendlyUnitInObjectRangeCheck check = new AnyUnfriendlyUnitInObjectRangeCheck(GetHitUnit(), player, 7);
                    UnitListSearcher searcher = new UnitListSearcher(GetHitUnit(), targets, check, GridType.All);
                    for (List<Unit>.Enumerator i = targets.GetEnumerator(); i.MoveNext();)
                    {
                        GetHitUnit().CastSpell(i.Current, PaladinSpells.EXORCISM_DF, damage);
                    }
                }

                if (GetHitUnit().GetCreatureType() == CreatureType.Undead || GetHitUnit().GetCreatureType() == CreatureType.Demon)
                {
                    GetHitUnit().CastSpell(GetHitUnit(), AuraType.ModStun, true);
                }
            }
        }
    }
}
