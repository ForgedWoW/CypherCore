﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Forged.MapServer.Chat.Commands;

internal class SpellCommands
{
    [CommandNonGroup("aura", RBACPermissions.CommandAura)]
    private static bool HandleAuraCommand(CommandHandler handler, uint spellId)
    {
        var target = handler.SelectedUnit;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

            return false;
        }

        var spellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(spellId, target.Location.Map.DifficultyID);

        if (spellInfo == null)
            return false;

        var castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, target.Location.MapId, spellId, target.Location.Map.GenerateLowGuid(HighGuid.Cast));
        AuraCreateInfo createInfo = new(castId, spellInfo, target.Location.Map.DifficultyID, SpellConst.MaxEffects, target);
        createInfo.SetCaster(target);

        Aura.TryRefreshStackOrCreate(createInfo);

        return true;
    }

    [CommandNonGroup("cooldown", RBACPermissions.CommandCooldown)]
    private static bool HandleCooldownCommand(CommandHandler handler, uint? spellIdArg)
    {
        var target = handler.SelectedUnit;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        var owner = target.CharmerOrOwnerPlayerOrPlayerItself;

        if (!owner)
        {
            owner = handler.Session.Player;
            target = owner;
        }

        var nameLink = handler.GetNameLink(owner);

        if (!spellIdArg.HasValue)
        {
            target.SpellHistory.ResetAllCooldowns();
            target.SpellHistory.ResetAllCharges();
            handler.SendSysMessage(CypherStrings.RemoveallCooldown, nameLink);
        }
        else
        {
            var spellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(spellIdArg.Value, target.Location.Map.DifficultyID);

            if (spellInfo == null)
            {
                handler.SendSysMessage(CypherStrings.UnknownSpell, owner == handler.Session.Player ? handler.GetCypherString(CypherStrings.You) : nameLink);

                return false;
            }

            target.SpellHistory.ResetCooldown(spellInfo.Id, true);
            target.SpellHistory.ResetCharges(spellInfo.ChargeCategoryId);
            handler.SendSysMessage(CypherStrings.RemoveallCooldown, spellInfo.Id, owner == handler.Session.Player ? handler.GetCypherString(CypherStrings.You) : nameLink);
        }

        return true;
    }
    [CommandNonGroup("setskill", RBACPermissions.CommandSetskill)]
    private static bool HandleSetSkillCommand(CommandHandler handler, uint skillId, uint level, uint? maxSkillArg)
    {
        var target = handler.SelectedPlayerOrSelf;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        var skillLine = handler.CliDB.SkillLineStorage.LookupByKey(skillId);

        if (skillLine == null)
        {
            handler.SendSysMessage(CypherStrings.InvalidSkillId, skillId);

            return false;
        }

        var targetHasSkill = target.GetSkillValue((SkillType)skillId) != 0;

        // If our target does not yet have the skill they are trying to add to them, the chosen level also becomes
        // the max level of the new profession.
        var max = (ushort)maxSkillArg.GetValueOrDefault(targetHasSkill ? target.GetPureMaxSkillValue((SkillType)skillId) : level);

        if (level == 0 || level > max)
            return false;

        // If the player has the skill, we get the current skill step. If they don't have the skill, we
        // add the skill to the player's book with step 1 (which is the first rank, in most cases something
        // like 'Apprentice <skill>'.
        target.SetSkill((SkillType)skillId, (uint)(targetHasSkill ? target.GetSkillStep((SkillType)skillId) : 1), level, max);
        handler.SendSysMessage(CypherStrings.SetSkill, skillId, skillLine.DisplayName[handler.SessionDbcLocale], handler.GetNameLink(target), level, max);

        return true;
    }

    [CommandNonGroup("unaura", RBACPermissions.CommandUnaura)]
    private static bool HandleUnAuraCommand(CommandHandler handler, uint spellId = 0)
    {
        var target = handler.SelectedUnit;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

            return false;
        }

        if (spellId == 0)
        {
            target.RemoveAllAuras();

            return true;
        }

        var spellInfo = handler.ClassFactory.Resolve<SpellManager>().GetSpellInfo(spellId, Difficulty.None);

        if (spellInfo != null)
        {
            target.RemoveAura(spellInfo.Id);

            return true;
        }

        return true;
    }
}