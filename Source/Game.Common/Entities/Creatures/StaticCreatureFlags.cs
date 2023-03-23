// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Common.Entities.Creatures;

public class StaticCreatureFlags
{
	CreatureStaticFlags _flags;
	CreatureStaticFlags2 _flags2;
	CreatureStaticFlags3 _flags3;
	CreatureStaticFlags4 _flags4;
	CreatureStaticFlags5 _flags5;
	CreatureStaticFlags6 _flags6;
	CreatureStaticFlags7 _flags7;
	CreatureStaticFlags8 _flags8;

	public bool HasFlag(CreatureStaticFlags flag) => _flags.HasFlag(flag);
	public bool HasFlag(CreatureStaticFlags2 flag) => _flags2.HasFlag(flag);
	public bool HasFlag(CreatureStaticFlags3 flag) => _flags3.HasFlag(flag);
	public bool HasFlag(CreatureStaticFlags4 flag) => _flags4.HasFlag(flag);
	public bool HasFlag(CreatureStaticFlags5 flag) => _flags5.HasFlag(flag);
	public bool HasFlag(CreatureStaticFlags6 flag) => _flags6.HasFlag(flag);
	public bool HasFlag(CreatureStaticFlags7 flag) => _flags7.HasFlag(flag);
	public bool HasFlag(CreatureStaticFlags8 flag) => _flags8.HasFlag(flag);

	public CreatureStaticFlags ModifyFlag(CreatureStaticFlags flag, bool apply = true) => apply ? _flags |= flag : _flags &= ~flag;
	public CreatureStaticFlags2 ModifyFlag(CreatureStaticFlags2 flag, bool apply = true) => apply ? _flags2 |= flag : _flags2 &= ~flag;
	public CreatureStaticFlags3 ModifyFlag(CreatureStaticFlags3 flag, bool apply = true) => apply ? _flags3 |= flag : _flags3 &= ~flag;
	public CreatureStaticFlags4 ModifyFlag(CreatureStaticFlags4 flag, bool apply = true) => apply ? _flags4 |= flag : _flags4 &= ~flag;
	public CreatureStaticFlags5 ModifyFlag(CreatureStaticFlags5 flag, bool apply = true) => apply ? _flags5 |= flag : _flags5 &= ~flag;
	public CreatureStaticFlags6 ModifyFlag(CreatureStaticFlags6 flag, bool apply = true) => apply ? _flags6 |= flag : _flags6 &= ~flag;
	public CreatureStaticFlags7 ModifyFlag(CreatureStaticFlags7 flag, bool apply = true) => apply ? _flags7 |= flag : _flags7 &= ~flag;
	public CreatureStaticFlags8 ModifyFlag(CreatureStaticFlags8 flag, bool apply = true) => apply ? _flags8 |= flag : _flags8 &= ~flag;
}
