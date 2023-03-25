// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class CriteriaRecord
{
	public uint Id;
	public CriteriaType Type;
	public uint Asset;
	public uint ModifierTreeId;
	public byte StartEvent;
	public uint StartAsset;
	public ushort StartTimer;
	public byte FailEvent;
	public uint FailAsset;
	public byte Flags;
	public ushort EligibilityWorldStateID;
	public byte EligibilityWorldStateValue;

	public CriteriaFlags GetFlags() => (CriteriaFlags)Flags;
}