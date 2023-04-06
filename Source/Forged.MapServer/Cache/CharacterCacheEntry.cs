// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Cache;

public class CharacterCacheEntry
{
    public uint AccountId { get; set; }
    public uint[] ArenaTeamId { get; set; } = new uint[SharedConst.MaxArenaSlot];
    public PlayerClass ClassId { get; set; }
    public ObjectGuid Guid { get; set; }
    public ulong GuildId { get; set; }
    public bool IsDeleted { get; set; }
    public byte Level { get; set; }
    public string Name { get; set; }
    public Race RaceId { get; set; }
    public Gender Sex { get; set; }
}