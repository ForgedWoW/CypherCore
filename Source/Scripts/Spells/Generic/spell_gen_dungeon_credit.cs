// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenDungeonCredit : SpellScript, ISpellAfterHit
{
    private bool _handled;

    public override bool Load()
    {
        _handled = false;

        return Caster.IsTypeId(TypeId.Unit);
    }

    public void AfterHit()
    {
        // This hook is executed for every Target, make sure we only credit instance once
        if (_handled)
            return;

        _handled = true;
        var caster = Caster;
        var instance = caster.InstanceScript;

        instance?.UpdateEncounterStateForSpellCast(SpellInfo.Id, caster);
    }
}