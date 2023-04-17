// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 76245 - Area Restrict Abom
internal class SpellQ13264Q13276Q13288Q13289AreaRestrictAbom : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var creature = HitCreature;

        if (creature != null)
        {
            var area = creature.Area;

            if (area != Misc.AREA_THE_BROKEN_FRONT &&
                area != Misc.AREA_MORD_RETHAR_THE_DEATH_GATE)
                creature.DespawnOrUnsummon();
        }
    }
}