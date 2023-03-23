﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Forged.RealmServer.Groups;
using Game.Common.Entities.Objects;

namespace Forged.RealmServer.Scripting.Interfaces.IGroup;

public interface IGroupOnRemoveMember : IScriptObject
{
	void OnRemoveMember(PlayerGroup group, ObjectGuid guid, RemoveMethod method, ObjectGuid kicker, string reason);
}