// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Grimoire of Service - 108501
[SpellScript(108501)]
internal class SpellWarlGrimoireOfServiceAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public void Handlearn(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var player = Caster.AsPlayer;

        if (Caster.AsPlayer)
        {
            player.LearnSpell(WarlockSpells.GRIMOIRE_IMP, false);
            player.LearnSpell(WarlockSpells.GRIMOIRE_VOIDWALKER, false);
            player.LearnSpell(WarlockSpells.GRIMOIRE_SUCCUBUS, false);
            player.LearnSpell(WarlockSpells.GRIMOIRE_FELHUNTER, false);

            if (player.GetPrimarySpecialization() == TalentSpecialization.WarlockDemonology)
                player.LearnSpell(WarlockSpells.GRIMOIRE_FELGUARD, false);
        }
    }

    public void HandleRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var player = Caster.AsPlayer;

        if (Caster.AsPlayer)
        {
            player.RemoveSpell(WarlockSpells.GRIMOIRE_IMP, false, false);
            player.RemoveSpell(WarlockSpells.GRIMOIRE_VOIDWALKER, false, false);
            player.RemoveSpell(WarlockSpells.GRIMOIRE_SUCCUBUS, false, false);
            player.RemoveSpell(WarlockSpells.GRIMOIRE_FELHUNTER, false, false);
            player.RemoveSpell(WarlockSpells.GRIMOIRE_FELGUARD, false, false);
        }
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(Handlearn, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }
}