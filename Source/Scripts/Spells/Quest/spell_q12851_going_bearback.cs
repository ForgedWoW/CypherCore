// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 54798 FLAMING Arrow Triggered Effect
internal class SpellQ12851GoingBearback : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectAfterApply));
    }

    private void HandleEffectApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;

        if (caster)
        {
            var target = Target;

            // Already in fire
            if (target.HasAura(QuestSpellIds.ABLAZE))
                return;

            var player = caster.CharmerOrOwnerPlayerOrPlayerItself;

            if (player)
                switch (target.Entry)
                {
                    case CreatureIds.FROSTWORG:
                        target.SpellFactory.CastSpell(player, QuestSpellIds.FROSTWORG_CREDIT, true);
                        target.SpellFactory.CastSpell(target, QuestSpellIds.IMMOLATION, true);
                        target.SpellFactory.CastSpell(target, QuestSpellIds.ABLAZE, true);

                        break;
                    case CreatureIds.FROSTGIANT:
                        target.SpellFactory.CastSpell(player, QuestSpellIds.FROSTGIANT_CREDIT, true);
                        target.SpellFactory.CastSpell(target, QuestSpellIds.IMMOLATION, true);
                        target.SpellFactory.CastSpell(target, QuestSpellIds.ABLAZE, true);

                        break;
                }
        }
    }
}