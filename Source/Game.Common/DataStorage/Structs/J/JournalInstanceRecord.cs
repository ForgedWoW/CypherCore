﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.ClientReader;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.J;

public sealed class JournalInstanceRecord
{
	public uint Id;
	public LocalizedString Name;
	public LocalizedString Description;
	public ushort MapID;
	public int BackgroundFileDataID;
	public int ButtonFileDataID;
	public int ButtonSmallFileDataID;
	public int LoreFileDataID;
	public int Flags;
	public ushort AreaID;
}
