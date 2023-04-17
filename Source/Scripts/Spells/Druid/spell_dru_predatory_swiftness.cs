// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(16974)]
public class SpellDruPredatorySwiftness : SpellScript, ISpellCheckCast, ISpellOnHit
{
    private int _cp;

    public override bool Load()
    {
        _cp = Caster.GetPower(PowerType.ComboPoints);

        return true;
    }

    public SpellCastResult CheckCast()
    {
        if (Caster)
        {
            if (Caster.TypeId != TypeId.Player)
                return SpellCastResult.DontReport;

            if (Caster.AsPlayer.GetPower(PowerType.ComboPoints) != 0)
                return SpellCastResult.NoComboPoints;
        }
        else
        {
            return SpellCastResult.DontReport;
        }

        return SpellCastResult.SpellCastOk;
    }

    public void OnHit()
    {
        var player = Caster.AsPlayer;

        if (player != null)
            if (player.HasAura(PredatorySwiftnessSpells.PredatorySwiftness) && RandomHelper.randChance(20 * _cp))
                player.SpellFactory.CastSpell(player, PredatorySwiftnessSpells.PredatorySwiftnessAura, true);
    }
}