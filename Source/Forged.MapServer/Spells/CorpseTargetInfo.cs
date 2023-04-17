// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class CorpseTargetInfo : TargetInfoBase
{
    public ObjectGuid TargetGuid { get; set; }
    public ulong TimeDelay { get; set; }

    public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
    {
        var corpse = ObjectAccessor.GetCorpse(spell.Caster, TargetGuid);

        if (corpse == null)
            return;

        spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

        spell.HandleEffects(null, null, null, corpse, spellEffectInfo, SpellEffectHandleMode.HitTarget);

        spell.CallScriptOnHitHandlers();
        spell.CallScriptAfterHitHandlers();
    }
}