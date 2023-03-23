// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Forged.RealmServer.Groups;
using Game.Common.Entities.Objects;

namespace Forged.RealmServer.Scripting.Interfaces.IGroup;

public interface IGroupOnAddMember : IScriptObject
{
	void OnAddMember(PlayerGroup group, ObjectGuid guid);
}