// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Spells;

public class GOTargetInfo : TargetInfoBase
{
	public ObjectGuid TargetGUID;
	public ulong TimeDelay;

	public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
	{
		var go = spell.Caster.GetGUID() == TargetGUID ? spell.Caster.ToGameObject() : ObjectAccessor.GetGameObject(spell.Caster, TargetGUID);

		if (go == null)
			return;

		spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

		spell.HandleEffects(null, null, go, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);

		//AI functions
		if (go.GetAI() != null)
			go.GetAI().SpellHit(spell.Caster, spell.SpellInfo);

		if (spell.Caster.IsCreature() && spell.Caster.ToCreature().IsAIEnabled())
			spell.Caster.ToCreature().GetAI().SpellHitTarget(go, spell.SpellInfo);
		else if (spell.Caster.IsGameObject() && spell.Caster.ToGameObject().GetAI() != null)
			spell.Caster.ToGameObject().GetAI().SpellHitTarget(go, spell.SpellInfo);

		spell.CallScriptOnHitHandlers();
		spell.CallScriptAfterHitHandlers();
	}
}