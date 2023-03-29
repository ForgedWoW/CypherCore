﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        [CreatureScript(89)]
        public class npc_warlock_infernal : SmartAI
        {
            public Position spawnPos = new();

            public npc_warlock_infernal(Creature creature) : base(creature)
            {
                if (!Me.TryGetOwner(out Player owner))
                    return;

                if (owner.TryGetAsPlayer(out var player) && player.HasAura(WarlockSpells.INFERNAL_BRAND))
                    Me.AddAura(WarlockSpells.INFERNAL_BRAND_INFERNAL_AURA, Me);

                creature.SetLevel(owner.Level);
                creature.UpdateLevelDependantStats();
                creature.ReactState = ReactStates.Assist;
                creature.SetCreatorGUID(owner.GUID);

                var summon = creature.ToTempSummon();

                if (summon != null)
                    StartAttackOnOwnersInCombatWith();
            }

            public override void Reset()
            {
                spawnPos = Me.Location;

                // if we leave default State (ASSIST) it will passively be controlled by warlock
                Me.
                    // if we leave default State (ASSIST) it will passively be controlled by warlock
                    ReactState = ReactStates.Passive;

                // melee Damage
                if (Me.TryGetOwner(out Player owner) && owner.TryGetAsPlayer(out var player))
                {
                    var isLordSummon = Me.Entry == 108452;

                    var spellPower = player.SpellBaseDamageBonusDone(SpellSchoolMask.Fire);
                    var dmg = MathFunctions.CalculatePct(spellPower, isLordSummon ? 30 : 50);
                    var diff = MathFunctions.CalculatePct(dmg, 10);

                    Me.SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MinDamage, dmg - diff);
                    Me.SetBaseWeaponDamage(WeaponAttackType.BaseAttack, WeaponDamageRange.MaxDamage, dmg + diff);


                    if (isLordSummon)
                        return;

                    if (player.HasAura(WarlockSpells.LORD_OF_THE_FLAMES) &&
                        !player.HasAura(WarlockSpells.LORD_OF_THE_FLAMES_CD))
                    {
                        List<double> angleOffsets = new()
                        {
                            (double)Math.PI / 2.0f,
                            (double)Math.PI,
                            3.0f * (double)Math.PI / 2.0f
                        };

                        for (uint i = 0; i < 3; ++i)
                            player.CastSpell(Me, WarlockSpells.LORD_OF_THE_FLAMES_SUMMON, true);

                        player.CastSpell(player, WarlockSpells.LORD_OF_THE_FLAMES_CD, true);
                    }
                }
            }

            public override void UpdateAI(uint UnnamedParameter)
            {
                if (!Me.HasAura(WarlockSpells.IMMOLATION))
                    DoCast(WarlockSpells.IMMOLATION);

                //DoMeleeAttackIfReady();
                base.UpdateAI(UnnamedParameter);
            }

            public override void OnMeleeAttack(CalcDamageInfo damageInfo, WeaponAttackType attType, bool extra)
            {
                if (Me != damageInfo.Attacker || !Me.TryGetOwner(out Player owner))
                    return;

                if (owner.TryGetAsPlayer(out var player) && player.HasAura(WarlockSpells.INFERNAL_BRAND))
                    Me.AddAura(WarlockSpells.INFERNAL_BRAND_ENEMY_AURA, damageInfo.Target);
            }
        }
    }
}