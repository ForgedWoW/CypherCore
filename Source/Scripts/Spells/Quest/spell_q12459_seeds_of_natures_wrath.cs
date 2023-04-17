// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 49587 Seeds of Nature's Wrath
internal class SpellQ12459SeedsOfNaturesWrath : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var creatureTarget = HitCreature;

        if (creatureTarget)
        {
            uint uiNewEntry = 0;

            switch (creatureTarget.Entry)
            {
                case CreatureIds.REANIMATED_FROSTWYRM:
                    uiNewEntry = CreatureIds.WEAK_REANIMATED_FROSTWYRM;

                    break;
                case CreatureIds.TURGID:
                    uiNewEntry = CreatureIds.WEAK_TURGID;

                    break;
                case CreatureIds.DEATHGAZE:
                    uiNewEntry = CreatureIds.WEAK_DEATHGAZE;

                    break;
            }

            if (uiNewEntry != 0)
                creatureTarget.UpdateEntry(uiNewEntry);
        }
    }
}