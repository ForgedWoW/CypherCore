// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 63845 - Create Lance
internal class SpellGenCreateLance : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        var target = HitPlayer;

        if (target)
        {
            if (target.Team == TeamFaction.Alliance)
                Caster.SpellFactory.CastSpell(target, GenericSpellIds.CREATE_LANCE_ALLIANCE, true);
            else
                Caster.SpellFactory.CastSpell(target, GenericSpellIds.CREATE_LANCE_HORDE, true);
        }
    }
}