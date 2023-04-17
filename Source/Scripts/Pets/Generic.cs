// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Pets
{
    namespace Generic
    {
        internal struct SpellIds
        {
            //Mojo
            public const uint FEELING_FROGGY = 43906;
            public const uint SEDUCTION_VISUAL = 43919;

            //SoulTrader
            public const uint ETHEREAL_ON_SUMMON = 50052;
            public const uint ETHEREAL_PET_REMOVE_AURA = 50055;

            // LichPet
            public const uint LICH_PET_AURA = 69732;
            public const uint LICH_PET_AURA_ONKILL = 69731;
            public const uint LICH_PET_EMOTE = 70049;
        }

        internal struct CreatureIds
        {
            // LichPet
            public const uint LICH_PET = 36979;
        }

        internal struct TextIds
        {
            //Mojo
            public const uint SAY_MOJO = 0;

            //SoulTrader
            public const uint SAY_SOUL_TRADER_INTO = 0;
        }

        [Script]
        internal class NPCPetGenSoulTrader : ScriptedAI
        {
            public NPCPetGenSoulTrader(Creature creature) : base(creature) { }

            public override void OnDespawn()
            {
                var owner = Me.OwnerUnit;

                if (owner != null)
                    DoCast(owner, SpellIds.ETHEREAL_PET_REMOVE_AURA);
            }

            public override void JustAppeared()
            {
                Talk(TextIds.SAY_SOUL_TRADER_INTO);

                var owner = Me.OwnerUnit;

                if (owner != null)
                    DoCast(owner, SpellIds.ETHEREAL_ON_SUMMON);

                base.JustAppeared();
            }
        }

        [Script] // 69735 - Lich Pet OnSummon
        internal class SpellGenLichPetOnsummon : SpellScript, IHasSpellEffects
        {
            public List<ISpellEffect> SpellEffects { get; } = new();


            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
            }

            private void HandleScriptEffect(int effIndex)
            {
                var target = HitUnit;
                target.SpellFactory.CastSpell(target, SpellIds.LICH_PET_AURA, true);
            }
        }

        [Script] // 69736 - Lich Pet Aura Remove
        internal class SpellGenLichPetAuraRemove : SpellScript, IHasSpellEffects
        {
            public List<ISpellEffect> SpellEffects { get; } = new();


            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
            }

            private void HandleScriptEffect(int effIndex)
            {
                HitUnit.RemoveAura(SpellIds.LICH_PET_AURA);
            }
        }

        [Script] // 69732 - Lich Pet Aura
        internal class SpellGenLichPetAura : AuraScript, IAuraCheckProc, IHasAuraEffects
        {
            public List<IAuraEffectHandler> AuraEffects { get; } = new();


            public bool CheckProc(ProcEventInfo eventInfo)
            {
                return eventInfo.ProcTarget.IsPlayer;
            }

            public override void Register()
            {
                AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
            }

            private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                PreventDefaultAction();

                List<TempSummon> minionList = new();
                OwnerAsUnit.GetAllMinionsByEntry(minionList, CreatureIds.LICH_PET);

                foreach (Creature minion in minionList)
                    if (minion.IsAIEnabled)
                        minion.AI.DoCastSelf(SpellIds.LICH_PET_AURA_ONKILL);
            }
        }

        [Script] // 70050 - [DND] Lich Pet
        internal class SpellPetGenLichPetPeriodicEmote : AuraScript, IHasAuraEffects
        {
            public List<IAuraEffectHandler> AuraEffects { get; } = new();


            public override void Register()
            {
                AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PeriodicTriggerSpell));
            }

            private void OnPeriodic(AuraEffect aurEff)
            {
                // The chance to cast this spell is not 100%.
                // Triggered spell roots creature for 3 sec and plays anim and sound (doesn't require any script).
                // Emote and sound never shows up in sniffs because both comes from spell visual directly.
                // Both 69683 and 70050 can trigger spells at once and are not linked together in any way.
                // Effect of 70050 is overlapped by effect of 69683 but not instantly (69683 is a series of spell casts, takes longer to execute).
                // However, for some reason Emote is not played if creature is idle and only if creature is moving or is already rooted.
                // For now it's scripted manually in script below to play Emote always.
                if (RandomHelper.randChance(50))
                    Target.SpellFactory.CastSpell(Target, SpellIds.LICH_PET_EMOTE, true);
            }
        }

        [Script] // 70049 - [DND] Lich Pet
        internal class SpellPetGenLichPetEmote : AuraScript, IHasAuraEffects
        {
            public List<IAuraEffectHandler> AuraEffects { get; } = new();

            public override void Register()
            {
                AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.ModRoot, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
            }

            private void AfterApply(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                Target.HandleEmoteCommand(Emote.OneshotCustomSpell01);
            }
        }

        [Script] // 69682 - Lil' K.T. Focus
        internal class SpellPetGenLichPetFocus : SpellScript, IHasSpellEffects
        {
            public List<ISpellEffect> SpellEffects { get; } = new();


            public override void Register()
            {
                SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
            }

            private void HandleScript(int effIndex)
            {
                Caster.SpellFactory.CastSpell(HitUnit, (uint)EffectValue);
            }
        }
    }
}