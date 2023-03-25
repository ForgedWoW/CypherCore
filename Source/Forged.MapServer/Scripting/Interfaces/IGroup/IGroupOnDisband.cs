// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Groups;

namespace Forged.MapServer.Scripting.Interfaces.IGroup;

public interface IGroupOnDisband : IScriptObject
{
	void OnDisband(PlayerGroup group);
}