// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_captain_cookie;

namespace Scripts.EasternKingdoms.Deadmines.Spells;

[SpellScript(89732)]
public class spell_captain_cookie_nauseated : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void HandleScript(int effIndex)
    {
        if (!Caster || !HitUnit)
            return;

        HitUnit.RemoveAuraFromStack(eSpell.SETIATED);
        HitUnit.RemoveAuraFromStack(eSpell.SETIATED_H);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
    }
}