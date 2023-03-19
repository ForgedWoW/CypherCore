// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_PYRE_MISSILE)]
internal class spell_evoker_pyre : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var args = new CastSpellExtraArgs(TriggerCastFlags.TriggeredAllowProc);
        Caster.CastSpell(ExplTargetUnit.Location, EvokerSpells.RED_PYRE_DAMAGE, args);
    }
}