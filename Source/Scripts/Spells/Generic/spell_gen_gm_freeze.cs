// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenGMFreeze : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModStun, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // Do what was done before to the Target in HandleFreezeCommand
        var player = Target.AsPlayer;

        if (player)
        {
            // stop combat + make player unattackable + Duel stop + stop some spells
            player. // stop combat + make player unattackable + Duel stop + stop some spells
                Faction = 35;

            player.CombatStop();

            if (player.IsNonMeleeSpellCast(true))
                player.InterruptNonMeleeSpells(true);

            player.SetUnitFlag(UnitFlags.NonAttackable);

            // if player class = hunter || warlock Remove pet if alive
            if ((player.Class == PlayerClass.Hunter) ||
                (player.Class == PlayerClass.Warlock))
            {
                var pet = player.CurrentPet;

                if (pet)
                {
                    pet.SavePetToDB(PetSaveMode.AsCurrent);

                    // not let dismiss dead pet
                    if (pet.IsAlive)
                        player.RemovePet(pet, PetSaveMode.NotInSlot);
                }
            }
        }
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // Do what was done before to the Target in HandleUnfreezeCommand
        var player = Target.AsPlayer;

        if (player)
        {
            // Reset player faction + allow combat + allow duels
            player.SetFactionForRace(player.Race);
            player.RemoveUnitFlag(UnitFlags.NonAttackable);
            // save player
            player.SaveToDB();
        }
    }
}