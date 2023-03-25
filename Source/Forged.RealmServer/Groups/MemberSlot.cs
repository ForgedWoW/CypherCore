﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Objects;

namespace Forged.RealmServer.Groups;

public class MemberSlot
{
	public ObjectGuid Guid;
	public string Name;
	public Race Race;
	public byte Class;
	public byte Group;
	public GroupMemberFlags Flags;
	public LfgRoles Roles;
	public bool ReadyChecked;
}