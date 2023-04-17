// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenDespawnTarget : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDespawn, SpellConst.EffectAll, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDespawn(int effIndex)
    {
        if (EffectInfo.IsEffect(SpellEffectName.Dummy) ||
            EffectInfo.IsEffect(SpellEffectName.ScriptEffect))
        {
            var target = HitCreature;

            target?.DespawnOrUnsummon();
        }
    }
}