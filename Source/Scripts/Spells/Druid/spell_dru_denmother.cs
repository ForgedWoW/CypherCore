// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(201522)]
public class SpellDruDenmother : SpellScript, ISpellOnHit
{
    private const int DenMother = 201522;
    private const int DenMotherIronfur = 201629;

    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (player.HasAura(DenMother))
            {
                var validTargets = new List<Unit>();
                var groupList = new List<Unit>();

                player.GetPartyMembers(groupList);

                if (groupList.Count == 0)
                    return;

                foreach (var itr in groupList)
                    if ((itr.GUID != player.GUID) && (itr.IsInRange(player, 0, 50, true)))
                        validTargets.Add(itr.AsUnit);

                if (validTargets.Count == 0)
                    return;

                validTargets.Sort(new HealthPctOrderPred());
                var lowTarget = validTargets.First();

                player.SpellFactory.CastSpell(lowTarget, 201629, true);
            }
    }
}