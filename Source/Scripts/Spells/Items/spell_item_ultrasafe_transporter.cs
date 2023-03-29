// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script] // 36941 - Ultrasafe Transporter: Toshley's Station
internal class spell_item_ultrasafe_transporter : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override bool Load()
    {
        return Caster.IsPlayer;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.TeleportUnits, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        if (!RandomHelper.randChance(50)) // 50% success
            return;

        var caster = Caster;

        uint spellId = 0;

        switch (RandomHelper.URand(0, 6))
        {
            case 0:
                spellId = ItemSpellIds.TransporterMalfunctionSmaller;

                break;
            case 1:
                spellId = ItemSpellIds.TransporterMalfunctionBigger;

                break;
            case 2:
                spellId = ItemSpellIds.SoulSplitEvil;

                break;
            case 3:
                spellId = ItemSpellIds.SoulSplitGood;

                break;
            case 4:
                if (caster.AsPlayer.TeamId == TeamIds.Alliance)
                    spellId = ItemSpellIds.TransformHorde;
                else
                    spellId = ItemSpellIds.TransformAlliance;

                break;
            case 5:
                spellId = ItemSpellIds.TransporterMalfunctionChicken;

                break;
            case 6:
                spellId = ItemSpellIds.EvilTwin;

                break;
        }

        caster.CastSpell(caster, spellId, true);
    }
}