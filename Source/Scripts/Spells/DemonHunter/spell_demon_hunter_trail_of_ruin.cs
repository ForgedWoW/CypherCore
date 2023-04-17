// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(258881)]
public class SpellDemonHunterTrailOfRuin : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.SpellInfo.Id == Global.SpellMgr.GetSpellInfo(DemonHunterSpells.BLADE_DANCE, Difficulty.None).GetEffect(0).TriggerSpell;
    }
}