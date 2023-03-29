﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_summon_tournament_mount : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        if (Caster.IsInDisallowedMountForm)
            Caster.RemoveAurasByType(AuraType.ModShapeshift);

        if (!Caster.HasAura(GenericSpellIds.LanceEquipped))
        {
            SetCustomCastResultMessage(SpellCustomErrors.MustHaveLanceEquipped);

            return SpellCastResult.CustomError;
        }

        return SpellCastResult.SpellCastOk;
    }
}