// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script("spell_q55_sacred_cleansing", SpellEffectName.Dummy, 1u, CreatureIds.MORBENT, CreatureIds.WEAKENED_MORBENT, true, 0)]
[Script("spell_q10255_administer_antidote", SpellEffectName.Dummy, 0u, CreatureIds.HELBOAR, CreatureIds.DREADTUSK, true, 0)]
[Script("spell_q11515_fel_siphon_dummy", SpellEffectName.Dummy, 0u, CreatureIds.FELBLOOD_INITIATE, CreatureIds.EMACIATED_FELBLOOD, true, 0)]
internal class SpellGenericQuestUpdateEntry : SpellScript, IHasSpellEffects
{
    private readonly uint _despawnTime;
    private readonly byte _effIndex;
    private readonly uint _newEntry;
    private readonly uint _originalEntry;
    private readonly bool _shouldAttack;

    private readonly SpellEffectName _spellEffect;

    public SpellGenericQuestUpdateEntry(SpellEffectName spellEffect, uint effIndex, uint originalEntry, uint newEntry, bool shouldAttack, uint despawnTime)
    {
        _spellEffect = spellEffect;
        _effIndex = (byte)effIndex;
        _originalEntry = originalEntry;
        _newEntry = newEntry;
        _shouldAttack = shouldAttack;
        _despawnTime = despawnTime;
    }

    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, _effIndex, _spellEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var creatureTarget = HitCreature;

        if (creatureTarget != null)
            if (!creatureTarget.IsPet &&
                creatureTarget.Entry == _originalEntry)
            {
                creatureTarget.UpdateEntry(_newEntry);

                if (_shouldAttack)
                    creatureTarget.EngageWithTarget(Caster);

                if (_despawnTime != 0)
                    creatureTarget.DespawnOrUnsummon(TimeSpan.FromMilliseconds(_despawnTime));
            }
    }
}