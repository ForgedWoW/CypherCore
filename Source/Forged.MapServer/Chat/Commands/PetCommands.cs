﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("pet")]
internal class PetCommands
{
    private static Pet GetSelectedPlayerPetOrOwn(CommandHandler handler)
    {
        var target = handler.SelectedUnit;

        if (target)
        {
            if (target.IsTypeId(TypeId.Player))
                return target.AsPlayer.CurrentPet;

            return target.IsPet switch
            {
                true => target.AsPet,
                _    => null
            };
        }

        var player = handler.Session.Player;

        return player ? player.CurrentPet : null;
    }

    [Command("create", RBACPermissions.CommandPetCreate)]
    private static bool HandlePetCreateCommand(CommandHandler handler)
    {
        var player = handler.Session.Player;
        var creatureTarget = handler.SelectedCreature;

        if (!creatureTarget || creatureTarget.IsPet || creatureTarget.IsTypeId(TypeId.Player))
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        var creatureTemplate = creatureTarget.Template;

        // Creatures with family CreatureFamily.None crashes the server
        if (creatureTemplate.Family == CreatureFamily.None)
        {
            handler.SendSysMessage("This creature cannot be tamed. (Family id: 0).");

            return false;
        }

        if (!player.PetGUID.IsEmpty)
        {
            handler.SendSysMessage("You already have a pet");

            return false;
        }

        // Everything looks OK, create new pet
        var pet = player.CreateTamedPetFrom(creatureTarget);

        // "kill" original creature
        creatureTarget.DespawnOrUnsummon();

        // prepare visual effect for levelup
        pet.SetLevel(player.Level - 1);

        // add to world
        pet.Location.
            // add to world
            Map.AddToMap(pet.AsCreature);

        // visual effect for levelup
        pet.SetLevel(player.Level);

        // caster have pet now
        player.SetMinion(pet, true);

        pet.SavePetToDB(PetSaveMode.AsCurrent);
        player.PetSpellInitialize();

        return true;
    }

    [Command("learn", RBACPermissions.CommandPetLearn)]
    private static bool HandlePetLearnCommand(CommandHandler handler, uint spellId)
    {
        var pet = GetSelectedPlayerPetOrOwn(handler);

        if (!pet)
        {
            handler.SendSysMessage(CypherStrings.SelectPlayerOrPet);

            return false;
        }

        if (spellId == 0 || !handler.ClassFactory.Resolve<SpellManager>().HasSpellInfo(spellId))
            return false;

        // Check if pet already has it
        if (pet.HasSpell(spellId))
        {
            handler.SendSysMessage("Pet already has spell: {0}", spellId);

            return false;
        }

        // Check if spell is valid
        var spellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(spellId);

        if (spellInfo == null || !handler.ClassFactory.Resolve<SpellManager>().IsSpellValid(spellInfo))
        {
            handler.SendSysMessage(CypherStrings.CommandSpellBroken, spellId);

            return false;
        }

        pet.LearnSpell(spellId);

        handler.SendSysMessage("Pet has learned spell {0}", spellId);

        return true;
    }

    [Command("level", RBACPermissions.CommandPetLevel)]
    private static bool HandlePetLevelCommand(CommandHandler handler, int level)
    {
        var pet = GetSelectedPlayerPetOrOwn(handler);
        var owner = pet ? pet.OwningPlayer : null;

        if (!pet || !owner)
        {
            handler.SendSysMessage(CypherStrings.SelectPlayerOrPet);

            return false;
        }

        level = level switch
        {
            0 => (int)(owner.Level - pet.Level),
            _ => level
        };

        if (level is 0 or < -SharedConst.StrongMaxLevel or > SharedConst.StrongMaxLevel)
        {
            handler.SendSysMessage(CypherStrings.BadValue);

            return false;
        }

        var newLevel = (int)pet.Level + level;

        if (newLevel < 1)
            newLevel = 1;
        else if (newLevel > owner.Level)
            newLevel = (int)owner.Level;

        pet.GivePetLevel(newLevel);

        return true;
    }

    [Command("unlearn", RBACPermissions.CommandPetUnlearn)]
    private static bool HandlePetUnlearnCommand(CommandHandler handler, uint spellId)
    {
        var pet = GetSelectedPlayerPetOrOwn(handler);

        if (!pet)
        {
            handler.SendSysMessage(CypherStrings.SelectPlayerOrPet);

            return false;
        }

        if (pet.HasSpell(spellId))
            pet.RemoveSpell(spellId, false);
        else
            handler.SendSysMessage("Pet doesn't have that spell");

        return true;
    }
}