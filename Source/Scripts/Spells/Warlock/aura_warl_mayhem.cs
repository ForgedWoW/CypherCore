// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.MAYHEM)]
public class AuraWarlMayhem : AuraScript, IAuraCheckProc, IAuraOnProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        if (info.ProcTarget != null)
            return RandomHelper.randChance(GetEffectInfo(0).BasePoints);

        return false;
    }

    public void OnProc(ProcEventInfo info)
    {
        Caster.SpellFactory.CastSpell(info.ProcTarget, WarlockSpells.HAVOC, new CastSpellExtraArgs(SpellValueMod.Duration, GetEffectInfo(2).BasePoints * Time.IN_MILLISECONDS).SetIsTriggered(true));
    }
}