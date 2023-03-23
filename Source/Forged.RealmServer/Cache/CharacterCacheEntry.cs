// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Forged.RealmServer.Cache;

public class CharacterCacheEntry
{
	public ObjectGuid Guid;
	public string Name;
	public uint AccountId;
	public PlayerClass ClassId;
	public Race RaceId;
	public Gender Sex;
	public byte Level;
	public ulong GuildId;
	public uint[] ArenaTeamId = new uint[SharedConst.MaxArenaSlot];
	public bool IsDeleted;
}