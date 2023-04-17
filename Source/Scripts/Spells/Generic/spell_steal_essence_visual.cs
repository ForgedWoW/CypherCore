// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellStealEssenceVisual : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;

        if (caster != null)
        {
            caster.SpellFactory.CastSpell(caster, GenericSpellIds.CREATE_TOKEN, true);
            var soulTrader = caster.AsCreature;

            soulTrader?.AI.Talk(TextIds.SAY_CREATE_TOKEN);
        }
    }
}