// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.AI.SmartScripts;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;
using Scripts.Spells.Warlock;

namespace Scripts.Pets
{
    namespace Warlock
    {
        [CreatureScript(89)]
        public class NPCWarlockInfernal : SmartAI
        {
            public Position SpawnPos = new();

            public NPCWarlockInfernal(Creature creature) : base(creature)
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
                SpawnPos = Me.Location;

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
                            player.SpellFactory.CastSpell(Me, WarlockSpells.LORD_OF_THE_FLAMES_SUMMON, true);

                        player.SpellFactory.CastSpell(player, WarlockSpells.LORD_OF_THE_FLAMES_CD, true);
                    }
                }
            }

            public override void UpdateAI(uint unnamedParameter)
            {
                if (!Me.HasAura(WarlockSpells.IMMOLATION))
                    DoCast(WarlockSpells.IMMOLATION);

                //DoMeleeAttackIfReady();
                base.UpdateAI(unnamedParameter);
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