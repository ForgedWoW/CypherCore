// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

// trigger on any heal cast
[SpellScript(EvokerSpells.SCARLET_ADAPTATION)]
internal class AuraEvokerScarletAdaptation : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo info)
    {
        if (!Caster.TryGetAsPlayer(out var player) || info.HealInfo == null || info.HealInfo.Heal == 0)
            return;

        var max = player.GetTotalSpellPowerValue(Framework.Constants.SpellSchoolMask.Fire, false) * 1.61;
        var heal = info.HealInfo.Heal * (Aura.GetEffect(0).Amount * 0.01);

        if (player.TryGetAura(EvokerSpells.SCARLET_ADAPTATION_AURA, out var saAura))
            heal += saAura.GetEffect(0).Amount;
        else
            saAura = player.AddAura(EvokerSpells.SCARLET_ADAPTATION_AURA);

        saAura.GetEffect(0).SetAmount(double.Min(heal, max));
    }
}