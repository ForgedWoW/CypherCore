// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Scripting.Interfaces.IUnit;

public interface IUnitOnMeleeAttack : IScriptObject
{
	void OnMeleeAttack(CalcDamageInfo damageInfo, WeaponAttackType attType, bool extra);
}