// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(18562)]
public class SpellDruSwiftmend : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
    }


    private void HandleHit(int effIndex)
    {
        var caster = Caster;

        if (caster != null)
            if (caster.HasAura(Spells.SoulOfTheForest))
                caster.AddAura(Spells.SoulOfTheForestTriggered, caster);
    }


    private struct Spells
    {
        public static readonly uint SoulOfTheForest = 158478;
        public static readonly uint SoulOfTheForestTriggered = 114108;
    }
}