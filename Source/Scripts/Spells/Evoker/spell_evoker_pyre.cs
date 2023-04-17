// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.RED_PYRE_MISSILE)]
internal class SpellEvokerPyre : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var args = new CastSpellExtraArgs(TriggerCastFlags.TriggeredAllowProc);
        Caster.SpellFactory.CastSpell(ExplTargetUnit.Location, EvokerSpells.RED_PYRE_DAMAGE, args);
    }
}