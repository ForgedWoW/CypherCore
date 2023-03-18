// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.STASIS)]
internal class aura_evoker_stasis_override_aura : AuraScript, IAuraOnRemove
{
    public void AuraRemoved(AuraRemoveMode removeMode)
    {
        Caster.RemoveAura(EvokerSpells.STASIS);
    }
}