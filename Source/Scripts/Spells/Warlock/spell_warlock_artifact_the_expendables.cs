// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

// 211219 - The Expendables
[SpellScript(211219)]
public class spell_warlock_artifact_the_expendables : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (caster.AsPlayer)
            return;

        var player = caster.CharmerOrOwnerPlayerOrPlayerItself;

        if (player == null)
            return;

        foreach (var unit in player.Controlled)
            player.CastSpell(unit, WarlockSpells.THE_EXPANDABLES_BUFF, true);
    }
}