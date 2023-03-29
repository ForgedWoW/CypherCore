﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script]
public class DH_DisableDoubleJump_OnMount : ScriptObjectAutoAdd, IPlayerOnSpellCast
{
    public PlayerClass PlayerClass => PlayerClass.DemonHunter;

    public DH_DisableDoubleJump_OnMount() : base("DH_DisableDoubleJump_OnMount") { }

    public void OnSpellCast(Player player, Spell spell, bool skipCheck)
    {
        if (player.Class == PlayerClass.DemonHunter && player.HasAura(DemonHunterSpells.DOUBLE_JUMP) && spell.SpellInfo.GetEffect(0).ApplyAuraName == AuraType.Mounted)
            player.SetCanDoubleJump(false);
    }

    public void OnUpdate(Player player, uint diff)
    {
        if (player.Class == PlayerClass.DemonHunter && player.HasAura(DemonHunterSpells.DOUBLE_JUMP) && !player.IsMounted && !player.HasExtraUnitMovementFlag(MovementFlag2.CanDoubleJump))
            player.SetCanDoubleJump(true);
    }
}