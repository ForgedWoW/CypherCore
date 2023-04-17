// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[SpellScript(108199)]
public class SpellDkGorefiendsGrasp : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }


    private void HandleScript(int effIndex)
    {
        var player = Caster.AsPlayer;

        if (player != null)
        {
            var target = HitUnit;

            if (target != null)
            {
                var tempList = new List<Unit>();
                var gripList = new List<Unit>();

                player.GetAttackableUnitListInRange(tempList, 20.0f);

                foreach (var itr in tempList)
                {
                    if (itr.GUID == player.GUID)
                        continue;

                    if (!player.IsValidAttackTarget(itr))
                        continue;

                    if (itr.IsImmunedToSpell(SpellInfo, Caster))
                        continue;

                    if (!itr.IsWithinLOSInMap(player))
                        continue;

                    gripList.Add(itr);
                }

                foreach (var itr in gripList)
                {
                    itr.SpellFactory.CastSpell(target, DeathKnightSpells.DEATH_GRIP_ONLY_JUMP, true);
                    itr.SpellFactory.CastSpell(target, DeathKnightSpells.GOREFIENDS_GRASP_GRIP_VISUAL, true);
                }
            }
        }
    }
}