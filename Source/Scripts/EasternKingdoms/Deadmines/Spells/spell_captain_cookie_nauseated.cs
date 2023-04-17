// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;
using static Scripts.EasternKingdoms.Deadmines.Bosses.BossCaptainCookie;

namespace Scripts.EasternKingdoms.Deadmines.Spells;

[SpellScript(89732)]
public class SpellCaptainCookieNauseated : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void HandleScript(int effIndex)
    {
        if (!Caster || !HitUnit)
            return;

        HitUnit.RemoveAuraFromStack(ESpell.SETIATED);
        HitUnit.RemoveAuraFromStack(ESpell.SETIATED_H);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
    }
}