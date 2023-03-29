// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Spells;

namespace Forged.MapServer.Scripting.Interfaces.ISpellManager;

/// <summary>
///     Applies spell fixes after LoadSpellInfoImmunities, LoadSpellInfoDiminishing, LoadSpellInfoCustomAttributes and LoadSkillLineAbilityMap all have effected the spell.
///     This will override any of those calculations.
/// </summary>
public interface ISpellManagerSpellLateFix
{
    int[] SpellIds { get; }

    void ApplySpellFix(SpellInfo spellInfo);
}