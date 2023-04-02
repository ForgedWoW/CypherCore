// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Cache;

public class CharacterCacheEntry
{
    public uint AccountId;
    public uint[] ArenaTeamId = new uint[SharedConst.MaxArenaSlot];
    public PlayerClass ClassId;
    public ObjectGuid Guid;
    public ulong GuildId;
    public bool IsDeleted;
    public byte Level;
    public string Name;
    public Race RaceId;
    public Gender Sex;
}