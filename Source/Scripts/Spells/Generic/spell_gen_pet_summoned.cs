// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenPetSummoned : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.IsTypeId(TypeId.Player);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var player = Caster.AsPlayer;

        if (player.LastPetNumber == 0)
            return;

        var newPetType = (player.Class == PlayerClass.Hunter) ? PetType.Hunter : PetType.Summon;
        var newPet = ClassFactory.ResolveWithPositionalParameters<Pet>(player, newPetType);

        if (!newPet.LoadPetFromDB(player, 0, player.LastPetNumber, true))
            return;

        // revive the pet if it is dead
        if (newPet.DeathState != DeathState.Alive &&
            newPet.DeathState != DeathState.JustRespawned)
            newPet.SetDeathState(DeathState.JustRespawned);

        newPet.SetFullHealth();
        newPet.SetFullPower(newPet.DisplayPowerType);

        var summonScript = Spell.GetSpellScripts<ISpellOnSummon>();

        foreach (var summon in summonScript.Cast<ISpellOnSummon>())
            summon.OnSummon(newPet);

        switch (newPet.Entry)
        {
            case CreatureIds.DOOMGUARD:
            case CreatureIds.INFERNAL:
                newPet.Entry = CreatureIds.IMP;

                break;
        }
    }
}