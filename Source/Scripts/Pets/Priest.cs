// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.CoreAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Pets
{
    namespace Priest
    {
        internal struct SpellIds
        {
            public const uint GLYPH_OF_SHADOW_FIEND = 58228;
            public const uint SHADOW_FIEND_DEATH = 57989;
            public const uint LIGHT_WELL_CHARGES = 59907;
        }

        [Script]
        internal class NPCPetPriLightwell : PassiveAI
        {
            public NPCPetPriLightwell(Creature creature) : base(creature)
            {
                DoCast(creature, SpellIds.LIGHT_WELL_CHARGES, new CastSpellExtraArgs(false));
            }

            public override void EnterEvadeMode(EvadeReason why)
            {
                if (!Me.IsAlive)
                    return;

                Me.CombatStop(true);
                EngagementOver();
                Me.ResetPlayerDamageReq();
            }
        }

        [Script]
        internal class NPCPetPriShadowfiend : PetAI
        {
            public NPCPetPriShadowfiend(Creature creature) : base(creature) { }

            public override void IsSummonedBy(WorldObject summoner)
            {
                var unitSummoner = summoner.AsUnit;

                if (unitSummoner == null)
                    return;

                if (unitSummoner.HasAura(SpellIds.GLYPH_OF_SHADOW_FIEND))
                    DoCastAOE(SpellIds.SHADOW_FIEND_DEATH);
            }
        }
    }
}