// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

//212056
[Script]
public class SpellPalAbsolution : ScriptObjectAutoAdd, IPlayerOnSpellCast
{
    public SpellPalAbsolution() : base("absolution") { }
    public PlayerClass PlayerClass { get; } = PlayerClass.Paladin;

    public void OnSpellCast(Player player, Spell spell, bool skipCheck)
    {
        if (player.Class != PlayerClass.Paladin)
            return;

        uint absolution = 212056;

        if (spell.SpellInfo.Id == absolution)
        {
            var allies = new List<Unit>();
            player.GetFriendlyUnitListInRange(allies, 30.0f, false);

            foreach (var targets in allies)
                if (targets.IsDead)
                {
                    var playerTarget = targets.AsPlayer;

                    if (playerTarget != null)
                        playerTarget.ResurrectPlayer(0.35f, false);
                }
        }
    }
}