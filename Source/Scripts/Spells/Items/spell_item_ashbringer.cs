// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemAshbringer : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        return Caster.TypeId == TypeId.Player;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(OnDummyEffect, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
    }

    private void OnDummyEffect(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);

        var player = Caster.AsPlayer;

        var soundID = RandomHelper.RAND(SoundIds.ASHBRINGER1,
                                        SoundIds.ASHBRINGER2,
                                        SoundIds.ASHBRINGER3,
                                        SoundIds.ASHBRINGER4,
                                        SoundIds.ASHBRINGER5,
                                        SoundIds.ASHBRINGER6,
                                        SoundIds.ASHBRINGER7,
                                        SoundIds.ASHBRINGER8,
                                        SoundIds.ASHBRINGER9,
                                        SoundIds.ASHBRINGER10,
                                        SoundIds.ASHBRINGER11,
                                        SoundIds.ASHBRINGER12);

        // Ashbringers effect (SpellIds.ID 28441) retriggers every 5 seconds, with a chance of making it say one of the above 12 sounds
        if (RandomHelper.URand(0, 60) < 1)
            player.PlayDirectSound(soundID, player);
    }
}