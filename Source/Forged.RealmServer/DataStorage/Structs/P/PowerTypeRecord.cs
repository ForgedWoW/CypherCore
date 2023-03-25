// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.DataStorage;

public sealed class PowerTypeRecord
{
	public string NameGlobalStringTag;
	public string CostGlobalStringTag;
	public uint Id;
	public PowerType PowerTypeEnum;
	public int MinPower;
	public int MaxBasePower;
	public int CenterPower;
	public int DefaultPower;
	public int DisplayModifier;
	public int RegenInterruptTimeMS;
	public float RegenPeace;
	public float RegenCombat;
	public short Flags;
}