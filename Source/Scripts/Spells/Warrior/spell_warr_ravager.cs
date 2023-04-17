// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// Ravager - 152277
// Ravager - 228920
[SpellScript(new uint[]
{
    152277, 228920
})]
public class SpellWarrRavager : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void HandleOnHit(int effIndex)
    {
        var dest = ExplTargetDest;

        if (dest != null)
            Caster.SpellFactory.CastSpell(dest, WarriorSpells.RAVAGER_SUMMON, true);
    }
}