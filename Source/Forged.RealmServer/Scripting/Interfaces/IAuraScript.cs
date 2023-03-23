// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Spells;
using Game.Common.Entities.GameObjects;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.Scripting.Interfaces;

public interface IAuraScript : IBaseSpellScript
{
	Aura Aura { get; }

	Difficulty CastDifficulty { get; }
	Unit Caster { get; }
	ObjectGuid CasterGUID { get; }

	int Duration { get; }
	GameObject GObjCaster { get; }
	uint Id { get; }

	int MaxDuration { get; set; }
	WorldObject Owner { get; }
	SpellInfo SpellInfo { get; }

	byte StackAmount { get; }

	Unit Target { get; }

	AuraApplication TargetApplication { get; }
	Unit OwnerAsUnit { get; }
	bool IsExpired { get; }

	AuraEffect GetEffect(byte effIndex);
	SpellEffectInfo GetEffectInfo(int effIndex);

	bool HasEffect(byte effIndex);

	bool ModStackAmount(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default);
	void PreventDefaultAction();
	void Remove(AuraRemoveMode removeMode = AuraRemoveMode.None);
	void SetDuration(int duration, bool withMods = false);
	void _FinishScriptCall();
	bool _IsDefaultActionPrevented();
	bool _Load(Aura aura);
	void _PrepareScriptCall(AuraScriptHookType hookType, AuraApplication aurApp = null);
}