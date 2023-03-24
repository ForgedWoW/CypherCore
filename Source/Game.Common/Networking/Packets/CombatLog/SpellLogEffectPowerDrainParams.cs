// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.CombatLog;

public struct SpellLogEffectPowerDrainParams
{
	public ObjectGuid Victim;
	public uint Points;
	public uint PowerType;
	public float Amplitude;
}
