// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Database;

public class AppliedFileEntry
{
	public string Name;
	public string Hash;
	public State State;
	public ulong Timestamp;

	public AppliedFileEntry(string name, string hash, State state, ulong timestamp)
	{
		Name = name;
		Hash = hash;
		State = state;
		Timestamp = timestamp;
	}
}