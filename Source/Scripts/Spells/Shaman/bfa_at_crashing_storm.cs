// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Shaman;

// 6826
[Script]
public class BfaAtCrashingStorm : AreaTriggerScript, IAreaTriggerOnInitialize, IAreaTriggerOnUpdate
{
    public uint DamageTimer;

    public void OnInitialize()
    {
        DamageTimer = 0;
    }

    public void OnUpdate(uint diff)
    {
        DamageTimer += diff;

        if (DamageTimer >= 2 * Time.IN_MILLISECONDS)
        {
            CheckPlayers();
            DamageTimer = 0;
        }
    }

    public void CheckPlayers()
    {
        var caster = At.GetCaster();

        if (caster != null)
        {
            var radius = 2.5f;

            var targetList = caster.GetPlayerListInGrid(radius);

            if (targetList.Count != 0)
                foreach (Player player in targetList)
                    if (!player.IsGameMaster)
                        caster.SpellFactory.CastSpell(player, ShamanSpells.CRASHING_STORM_TALENT_DAMAGE, true);
        }
    }
}