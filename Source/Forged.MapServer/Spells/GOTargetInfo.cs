// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class GOTargetInfo : TargetInfoBase
{
	public ObjectGuid TargetGUID;
	public ulong TimeDelay;

	public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
	{
		var go = spell.Caster.GUID == TargetGUID ? spell.Caster.AsGameObject : ObjectAccessor.GetGameObject(spell.Caster, TargetGUID);

		if (go == null)
			return;

		spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

		spell.HandleEffects(null, null, go, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);

		//AI functions
		if (go.AI != null)
			go.AI.SpellHit(spell.Caster, spell.SpellInfo);

		if (spell.Caster.IsCreature && spell.Caster.AsCreature.IsAIEnabled)
			spell.Caster.AsCreature.AI.SpellHitTarget(go, spell.SpellInfo);
		else if (spell.Caster.IsGameObject && spell.Caster.AsGameObject.AI != null)
			spell.Caster.AsGameObject.AI.SpellHitTarget(go, spell.SpellInfo);

		spell.CallScriptOnHitHandlers();
		spell.CallScriptAfterHitHandlers();
	}
}