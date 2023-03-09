// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.IO;

namespace Game.DataStorage;

public interface IDB2Storage
{
	bool HasRecord(uint id);

	void WriteRecord(uint id, Locale locale, ByteBuffer buffer);

	void EraseRecord(uint id);

	string GetName();
}