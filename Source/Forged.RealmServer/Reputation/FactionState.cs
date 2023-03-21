// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer;

public class FactionState
{
	public uint Id;
	public uint ReputationListID;
	public int Standing;
	public int VisualStandingIncrease;
	public ReputationFlags Flags;
	public bool needSend;
	public bool needSave;
}