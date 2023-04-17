// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[SpellScript(new uint[]
{
    3409, 8679, 108211
})]
public class SpellRogPoisonsSpellScript : SpellScript, ISpellBeforeHit
{
    public void BeforeHit(SpellMissInfo missInfo)
    {
        if (missInfo != SpellMissInfo.None)
            return;

        var player = Caster.AsPlayer;

        if (player != null)
            RemovePreviousPoisons();
    }

    private void RemovePreviousPoisons()
    {
        var plr = Caster.AsPlayer;

        if (plr != null)
        {
            if (plr.HasAura(EPoisons.WoundPoison))
                plr.RemoveAura(EPoisons.WoundPoison);

            if (plr.HasAura(EPoisons.MindNumbingPoison))
                plr.RemoveAura(EPoisons.MindNumbingPoison);

            if (plr.HasAura(EPoisons.CripplingPoison))
                plr.RemoveAura(EPoisons.CripplingPoison);

            if (plr.HasAura(EPoisons.LeechingPoison))
                plr.RemoveAura(EPoisons.LeechingPoison);

            if (plr.HasAura(EPoisons.ParalyticPoison))
                plr.RemoveAura(EPoisons.ParalyticPoison);

            if (plr.HasAura(EPoisons.DeadlyPoison))
                plr.RemoveAura(EPoisons.DeadlyPoison);

            if (plr.HasAura(EPoisons.InstantPoison))
                plr.RemoveAura(EPoisons.InstantPoison);
        }
    }
}