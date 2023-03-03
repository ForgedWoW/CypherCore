// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Paladin
{
    [SpellScript(224239)] // 224239 - Divine Storm
    internal class spell_pal_divine_storm : SpellScript, ISpellOnCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return CliDB.SpellVisualKitStorage.HasRecord(PaladinSpellVisualKit.DivineStorm);
        }

        public void OnCast()
        {
            GetCaster().SendPlaySpellVisualKit(PaladinSpellVisualKit.DivineStorm, 0, 0);
        }
    }
}
