// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script("spell_item_deathbringers_will_normal", ItemSpellIds.STRENGTH_OF_THE_TAUNKA, ItemSpellIds.AGILITY_OF_THE_VRYKUL, ItemSpellIds.POWER_OF_THE_TAUNKA, ItemSpellIds.AIM_OF_THE_IRON_DWARVES, ItemSpellIds.SPEED_OF_THE_VRYKUL)]
[Script("spell_item_deathbringers_will_heroic", ItemSpellIds.STRENGTH_OF_THE_TAUNKA_HERO, ItemSpellIds.AGILITY_OF_THE_VRYKUL_HERO, ItemSpellIds.POWER_OF_THE_TAUNKA_HERO, ItemSpellIds.AIM_OF_THE_IRON_DWARVES_HERO, ItemSpellIds.SPEED_OF_THE_VRYKUL_HERO)]
internal class SpellItemDeathbringersWill : AuraScript, IHasAuraEffects
{
    private readonly uint _agilitySpellId;
    private readonly uint _apSpellId;
    private readonly uint _criticalSpellId;
    private readonly uint _hasteSpellId;

    private readonly uint _strengthSpellId;

    public SpellItemDeathbringersWill(uint strengthSpellId, uint agilitySpellId, uint apSpellId, uint criticalSpellId, uint hasteSpellId)
    {
        _strengthSpellId = strengthSpellId;
        _agilitySpellId = agilitySpellId;
        _apSpellId = apSpellId;
        _criticalSpellId = criticalSpellId;
        _hasteSpellId = hasteSpellId;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        uint[][] triggeredSpells =
        {
            //CLASS_NONE
            Array.Empty<uint>(),
            //CLASS_WARRIOR
            new uint[]
            {
                _strengthSpellId, _criticalSpellId, _hasteSpellId
            },
            //CLASS_PALADIN
            new uint[]
            {
                _strengthSpellId, _criticalSpellId, _hasteSpellId
            },
            //CLASS_HUNTER
            new uint[]
            {
                _agilitySpellId, _criticalSpellId, _apSpellId
            },
            //CLASS_ROGUE
            new uint[]
            {
                _agilitySpellId, _hasteSpellId, _apSpellId
            },
            //CLASS_PRIEST
            Array.Empty<uint>(),
            //CLASS_DEATH_KNIGHT
            new uint[]
            {
                _strengthSpellId, _criticalSpellId, _hasteSpellId
            },
            //CLASS_SHAMAN
            new uint[]
            {
                _agilitySpellId, _hasteSpellId, _apSpellId
            },
            //CLASS_MAGE
            Array.Empty<uint>(),
            //CLASS_WARLOCK
            Array.Empty<uint>(),
            //CLASS_UNK
            Array.Empty<uint>(),
            //CLASS_DRUID
            new uint[]
            {
                _strengthSpellId, _agilitySpellId, _hasteSpellId
            }
        };

        PreventDefaultAction();
        var caster = eventInfo.Actor;
        var randomSpells = triggeredSpells[(int)caster.Class];

        if (randomSpells.Empty())
            return;

        var spellId = randomSpells.SelectRandom();
        caster.SpellFactory.CastSpell(caster, spellId, new CastSpellExtraArgs(aurEff));
    }
}