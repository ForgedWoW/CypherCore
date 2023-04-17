// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 59579 - Burst at the Seams
internal class SpellQ13264Q13276Q13288Q13289BurstAtTheSeams59579 : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        target.SpellFactory.CastSpell(target, QuestSpellIds.TROLL_EXPLOSION, true);
        target.SpellFactory.CastSpell(target, QuestSpellIds.EXPLODE_ABOMINATION_MEAT, true);
        target.SpellFactory.CastSpell(target, QuestSpellIds.EXPLODE_TROLL_MEAT, true);
        target.SpellFactory.CastSpell(target, QuestSpellIds.EXPLODE_TROLL_MEAT, true);
        target.SpellFactory.CastSpell(target, QuestSpellIds.EXPLODE_TROLL_BLOODY_MEAT, true);
        target.SpellFactory.CastSpell(target, QuestSpellIds.BURST_AT_THE_SEAMS_BONE, true);
    }

    private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var target = Target;
        var caster = Caster;

        if (caster != null)
            switch (target.Entry)
            {
                case CreatureIds.ICY_GHOUL:
                    target.SpellFactory.CastSpell(caster, QuestSpellIds.ASSIGN_GHOUL_KILL_CREDIT_TO_MASTER, true);

                    break;
                case CreatureIds.VICIOUS_GEIST:
                    target.SpellFactory.CastSpell(caster, QuestSpellIds.ASSIGN_GEIST_KILL_CREDIT_TO_MASTER, true);

                    break;
                case CreatureIds.RISEN_ALLIANCE_SOLDIERS:
                    target.SpellFactory.CastSpell(caster, QuestSpellIds.ASSIGN_SKELETON_KILL_CREDIT_TO_MASTER, true);

                    break;
            }

        target.SpellFactory.CastSpell(target, QuestSpellIds.BURST_AT_THE_SEAMS59580, true);
    }
}