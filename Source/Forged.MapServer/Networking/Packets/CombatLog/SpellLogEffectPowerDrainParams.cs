// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.CombatLog;

public struct SpellLogEffectPowerDrainParams
{
    public float Amplitude;
    public uint Points;
    public uint PowerType;
    public ObjectGuid Victim;
}