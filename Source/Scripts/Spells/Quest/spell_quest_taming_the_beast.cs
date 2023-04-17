// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script]
internal class SpellQuestTamingTheBeast : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (!Caster ||
            !Caster.IsAlive ||
            !Target.IsAlive)
            return;

        if (TargetApplication.RemoveMode != AuraRemoveMode.Expire)
            return;

        uint finalSpellId = Id switch
        {
            QuestSpellIds.TAME_ICE_CLAW_BEAR          => QuestSpellIds.TAME_ICE_CLAW_BEAR1,
            QuestSpellIds.TAME_LARGE_CRAG_BOAR        => QuestSpellIds.TAME_LARGE_CRAG_BOAR1,
            QuestSpellIds.TAME_SNOW_LEOPARD          => QuestSpellIds.TAME_SNOW_LEOPARD1,
            QuestSpellIds.TAME_ADULT_PLAINSTRIDER    => QuestSpellIds.TAME_ADULT_PLAINSTRIDER1,
            QuestSpellIds.TAME_PRAIRIE_STALKER       => QuestSpellIds.TAME_PRAIRIE_STALKER1,
            QuestSpellIds.TAME_SWOOP                => QuestSpellIds.TAME_SWOOP1,
            QuestSpellIds.TAME_WEBWOOD_LURKER        => QuestSpellIds.TAME_WEBWOOD_LURKER1,
            QuestSpellIds.TAME_DIRE_MOTTLED_BOAR      => QuestSpellIds.TAME_DIRE_MOTTLED_BOAR1,
            QuestSpellIds.TAME_SURF_CRAWLER          => QuestSpellIds.TAME_SURF_CRAWLER1,
            QuestSpellIds.TAME_ARMORED_SCORPID       => QuestSpellIds.TAME_ARMORED_SCORPID1,
            QuestSpellIds.TAME_NIGHTSABER_STALKER    => QuestSpellIds.TAME_NIGHTSABER_STALKER1,
            QuestSpellIds.TAME_STRIGID_SCREECHER     => QuestSpellIds.TAME_STRIGID_SCREECHER1,
            QuestSpellIds.TAME_BARBED_CRAWLER        => QuestSpellIds.TAME_BARBED_CRAWLER1,
            QuestSpellIds.TAME_GREATER_TIMBERSTRIDER => QuestSpellIds.TAME_GREATER_TIMBERSTRIDER1,
            QuestSpellIds.TAME_NIGHTSTALKER         => QuestSpellIds.TAME_NIGHTSTALKER1,
            QuestSpellIds.TAME_CRAZED_DRAGONHAWK     => QuestSpellIds.TAME_CRAZED_DRAGONHAWK1,
            QuestSpellIds.TAME_ELDER_SPRINGPAW       => QuestSpellIds.TAME_ELDER_SPRINGPAW1,
            QuestSpellIds.TAME_MISTBAT              => QuestSpellIds.TAME_MISTBAT1,
            _                                      => 0
        };

        if (finalSpellId != 0)
            Caster.SpellFactory.CastSpell(Target, finalSpellId, true);
    }
}