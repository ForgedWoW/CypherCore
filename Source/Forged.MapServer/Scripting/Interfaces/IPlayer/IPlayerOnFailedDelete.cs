// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

// Called when a player delete failed
public interface IPlayerOnFailedDelete : IScriptObject
{
    void OnFailedDelete(ObjectGuid guid, uint accountId);
}