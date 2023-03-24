// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Entities;
using Game.Common.Entities.Creatures;
using Game.Common.Entities.GameObjects;
using Game.Common.Entities.Items;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Players;
using Game.Common.Entities.Units;

namespace Game.Common.Scripting.Interfaces;

public interface ISpellScript : IBaseSpellScript
{
	Difficulty CastDifficulty { get; }
	Unit Caster { get; }

	Item CastItem { get; }
	SpellEffectInfo EffectInfo { get; }
	double EffectValue { get; set; }

	double EffectVariance { get; set; }
	WorldLocation ExplTargetDest { get; set; }

	GameObject ExplTargetGObj { get; }

	Item ExplTargetItem { get; }
	Unit ExplTargetUnit { get; }
	WorldObject ExplTargetWorldObject { get; }
	GameObject GObjCaster { get; }
	Corpse HitCorpse { get; }
	Creature HitCreature { get; }

	double HitDamage { get; set; }
	WorldLocation HitDest { get; }
	GameObject HitGObj { get; }

	double HitHeal { get; set; }
	Item HitItem { get; }
	Player HitPlayer { get; }
	Unit HitUnit { get; }
	Unit OriginalCaster { get; }

	Spell Spell { get; }
	SpellInfo SpellInfo { get; }

	SpellValue SpellValue { get; }

	SpellInfo TriggeringSpell { get; }
	bool IsHitCrit { get; }
	bool IsInCheckCastHook { get; }
	bool IsInEffectHook { get; }
	bool IsInHitPhase { get; }

	bool IsInTargetHook { get; }
	void CreateItem(uint itemId, ItemContext context);
	void FinishCast(SpellCastResult result, int? param1 = null, int? param2 = null);

	long GetCorpseTargetCountForEffect(int effect);

	SpellEffectInfo GetEffectInfo(int effIndex);

	long GetGameObjectTargetCountForEffect(int effect);

	Aura GetHitAura(bool dynObjAura = false);

	long GetItemTargetCountForEffect(int effect);

	long GetUnitTargetCountForEffect(int effect);

	void PreventHitAura();
	void PreventHitDamage();
	void PreventHitDefaultEffect(int effIndex);
	void PreventHitEffect(int effIndex);
	void SelectRandomInjuredTargets(List<WorldObject> targets, uint maxTargets, bool prioritizePlayers);
	void SetCustomCastResultMessage(SpellCustomErrors result);
	void _FinishScriptCall();
	void _InitHit();
	bool _IsDefaultEffectPrevented(int effIndex);
	bool _IsEffectPrevented(int effIndex);
	bool _Load(Spell spell);
	void _PrepareScriptCall(SpellScriptHookType hookType);
}
