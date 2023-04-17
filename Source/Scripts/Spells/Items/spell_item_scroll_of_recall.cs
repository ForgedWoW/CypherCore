// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 60321 - Scroll of Recall III
internal class SpellItemScrollOfRecall : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.TypeId == TypeId.Player;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.TeleportUnits, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster;
        byte maxSafeLevel = 0;

        switch (SpellInfo.Id)
        {
            case ItemSpellIds.SCROLL_OF_RECALL_I: // Scroll of Recall
                maxSafeLevel = 40;

                break;
            case ItemSpellIds.SCROLL_OF_RECALL_II: // Scroll of Recall II
                maxSafeLevel = 70;

                break;
            case ItemSpellIds.SCROLL_OF_RECALL_III: // Scroll of Recal III
                maxSafeLevel = 80;

                break;
        }

        if (caster.Level > maxSafeLevel)
        {
            caster.SpellFactory.CastSpell(caster, ItemSpellIds.LOST, true);

            // ALLIANCE from 60323 to 60330 - HORDE from 60328 to 60335
            var spellId = ItemSpellIds.SCROLL_OF_RECALL_FAIL_ALLIANCE1;

            if (Caster.AsPlayer.Team == TeamFaction.Horde)
                spellId = ItemSpellIds.SCROLL_OF_RECALL_FAIL_HORDE1;

            Caster.SpellFactory.CastSpell(Caster, spellId + RandomHelper.URand(0, 7), true);

            PreventHitDefaultEffect(effIndex);
        }
    }
}