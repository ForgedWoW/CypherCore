// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Spells;

public class CorpseTargetInfo : TargetInfoBase
{
	public ObjectGuid TargetGuid;
	public ulong TimeDelay;

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