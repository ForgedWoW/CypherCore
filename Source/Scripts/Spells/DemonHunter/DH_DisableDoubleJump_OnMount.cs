// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[Script]
public class DhDisableDoubleJumpOnMount : ScriptObjectAutoAdd, IPlayerOnSpellCast
{
    public DhDisableDoubleJumpOnMount() : base("DH_DisableDoubleJump_OnMount") { }
    public PlayerClass PlayerClass => PlayerClass.DemonHunter;

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