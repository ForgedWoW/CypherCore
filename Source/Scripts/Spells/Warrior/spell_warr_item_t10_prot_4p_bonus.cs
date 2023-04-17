// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// 70844 - Item - Warrior T10 Protection 4P Bonus
[Script] // 7.1.5
internal class SpellWarrItemT10Prot4PBonus : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        var target = eventInfo.ActionTarget;
        var bp0 = (int)MathFunctions.CalculatePct(target.MaxHealth, GetEffectInfo(1).CalcValue());
        CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
        args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
        target.SpellFactory.CastSpell((Unit)null, WarriorSpells.STOICISM, args);
    }
}