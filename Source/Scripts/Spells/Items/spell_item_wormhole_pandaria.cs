// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 126755 - Wormhole: Pandaria
internal class SpellItemWormholePandaria : SpellScript, IHasSpellEffects
{
    private readonly uint[] _wormholeTargetLocations =
    {
        ItemSpellIds.WORMHOLEPANDARIAISLEOFRECKONING, ItemSpellIds.WORMHOLEPANDARIAKUNLAIUNDERWATER, ItemSpellIds.WORMHOLEPANDARIASRAVESS, ItemSpellIds.WORMHOLEPANDARIARIKKITUNVILLAGE, ItemSpellIds.WORMHOLEPANDARIAZANVESSTREE, ItemSpellIds.WORMHOLEPANDARIAANGLERSWHARF, ItemSpellIds.WORMHOLEPANDARIACRANESTATUE, ItemSpellIds.WORMHOLEPANDARIAEMPERORSOMEN, ItemSpellIds.WORMHOLEPANDARIAWHITEPETALLAKE
    };

    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleTeleport, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleTeleport(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        var spellId = _wormholeTargetLocations.SelectRandom();
        Caster.SpellFactory.CastSpell(HitUnit, spellId, true);
    }
}