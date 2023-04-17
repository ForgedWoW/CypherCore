// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(124682)]
public class SpellMonkEnvelopingMist : SpellScript, ISpellAfterCast, ISpellBeforeCast
{
    public void AfterCast()
    {
        var caster = Caster.AsPlayer;

        if (caster == null)
            return;

        if (caster.HasAura(MonkSpells.LIFECYCLES))
            caster.SpellFactory.CastSpell(caster, MonkSpells.LIFECYCLES_VIVIFY, true);
    }

    public void BeforeCast()
    {
        if (Caster.GetCurrentSpell(CurrentSpellTypes.Channeled) && Caster.GetCurrentSpell(CurrentSpellTypes.Channeled).SpellInfo.Id == MonkSpells.SOOTHING_MIST)
        {
            Spell.CastFlagsEx = SpellCastFlagsEx.None;
            var targets = Caster.GetCurrentSpell(CurrentSpellTypes.Channeled).Targets;
            Spell.InitExplicitTargets(targets);
        }
    }
}