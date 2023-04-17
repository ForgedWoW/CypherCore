// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[Script]
public class SpellDkRunicEmpowerment : ScriptObjectAutoAdd, IPlayerOnModifyPower
{
    public SpellDkRunicEmpowerment() : base("spell_dk_runic_empowerment") { }
    public PlayerClass PlayerClass { get; } = PlayerClass.Deathknight;

    public void OnModifyPower(Player pPlayer, PowerType pPower, int pOldValue, ref int pNewValue, bool pRegen)
    {
        if (pPlayer.Class != PlayerClass.Deathknight || pPower != PowerType.RunicPower || pRegen || pNewValue > pOldValue)
            return;

        var lRunicEmpowerment = pPlayer.GetAuraEffect(ESpells.RUNIC_EMPOWERMENT, 0);

        if (lRunicEmpowerment != null)
        {
            /// 1.00% chance per Runic Power spent
            var lChance = (lRunicEmpowerment.Amount / 100.0f);

            if (RandomHelper.randChance(lChance))
            {
                var lLstRunesUsed = new List<byte>();

                for (byte i = 0; i < PlayerConst.MaxRunes; ++i)
                    if (pPlayer.GetRuneCooldown(i) != 0)
                        lLstRunesUsed.Add(i);

                if (lLstRunesUsed.Count == 0)
                    return;

                var lRuneRandom = lLstRunesUsed.SelectRandom();

                pPlayer.SetRuneCooldown(lRuneRandom, 0);
                pPlayer.ResyncRunes();
            }
        }
    }

    public struct ESpells
    {
        public const uint RUNIC_EMPOWERMENT = 81229;
    }
}