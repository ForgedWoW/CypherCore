// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
using Forged.RealmServer.Groups;
using Forged.RealmServer.Entities.Objects;

namespace Forged.RealmServer.Scripting.Interfaces.IGroup;

public interface IGroupOnInviteMember : IScriptObject
{
	void OnInviteMember(PlayerGroup group, ObjectGuid guid);
}