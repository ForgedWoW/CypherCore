// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 46023 The Ultrasonic Screwdriver
internal class SpellQ11730UltrasonicScrewdriver : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player) && CastItem;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var castItem = CastItem;
        var caster = Caster;

        var target = HitCreature;

        if (target)
        {
            uint spellId;

            switch (target.Entry)
            {
                case CreatureIds.SCAVENGEBOT004_A8:
                    spellId = QuestSpellIds.SUMMON_SCAVENGEBOT004_A8;

                    break;
                case CreatureIds.SENTRYBOT57_K:
                    spellId = QuestSpellIds.SUMMON_SENTRYBOT57_K;

                    break;
                case CreatureIds.DEFENDOTANK66D:
                    spellId = QuestSpellIds.SUMMON_DEFENDOTANK66D;

                    break;
                case CreatureIds.SCAVENGEBOT005_B6:
                    spellId = QuestSpellIds.SUMMON_SCAVENGEBOT005_B6;

                    break;
                case CreatureIds.NPC55D_COLLECTATRON:
                    spellId = QuestSpellIds.SUMMON55D_COLLECTATRON;

                    break;
                default:
                    return;
            }

            caster.SpellFactory.CastSpell(caster, spellId, new CastSpellExtraArgs(castItem));
            caster.SpellFactory.CastSpell(caster, QuestSpellIds.ROBOT_KILL_CREDIT, true);
            target.DespawnOrUnsummon();
        }
    }
}