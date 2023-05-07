// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.D;

public sealed record DestructibleModelDataRecord
{
    public byte DoNotHighlight;
    public byte EjectDirection;
    public byte HealEffect;
    public ushort HealEffectSpeed;
    public uint Id;
    public byte State0AmbientDoodadSet;
    public sbyte State0ImpactEffectDoodadSet;
    public byte State0NameSet;
    public uint State0Wmo;
    public byte State1AmbientDoodadSet;
    public sbyte State1DestructionDoodadSet;
    public sbyte State1ImpactEffectDoodadSet;
    public byte State1NameSet;
    public uint State1Wmo;
    public byte State2AmbientDoodadSet;
    public sbyte State2DestructionDoodadSet;
    public sbyte State2ImpactEffectDoodadSet;
    public byte State2NameSet;
    public uint State2Wmo;
    public byte State3AmbientDoodadSet;
    public byte State3InitDoodadSet;
    public byte State3NameSet;
    public uint State3Wmo;
}