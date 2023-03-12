// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.IO;

namespace Framework.Database;

public class FileEntry
{
	public string path;
	public State state;

	public FileEntry(string _path, State _state)
	{
		path = _path.Replace(@"\", @"/");
		state = _state;
	}

	public string GetFileName()
	{
		return Path.GetFileName(path);
	}
}