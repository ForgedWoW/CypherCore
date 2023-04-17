// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenTeleporting : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var target = HitUnit;

        if (!target.IsPlayer)
            return;

        // return from top
        if (target.AsPlayer.Area == Misc.AREA_VIOLET_CITADEL_SPIRE)
            target.SpellFactory.CastSpell(target, GenericSpellIds.TELEPORT_SPIRE_DOWN, true);
        // teleport atop
        else
            target.SpellFactory.CastSpell(target, GenericSpellIds.TELEPORT_SPIRE_UP, true);
    }
}