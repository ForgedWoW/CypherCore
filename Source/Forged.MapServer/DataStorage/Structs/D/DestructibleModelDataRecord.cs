// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.D;

public sealed class DestructibleModelDataRecord
{
	public uint Id;
	public sbyte State0ImpactEffectDoodadSet;
	public byte State0AmbientDoodadSet;
	public uint State1Wmo;
	public sbyte State1DestructionDoodadSet;
	public sbyte State1ImpactEffectDoodadSet;
	public byte State1AmbientDoodadSet;
	public uint State2Wmo;
	public sbyte State2DestructionDoodadSet;
	public sbyte State2ImpactEffectDoodadSet;
	public byte State2AmbientDoodadSet;
	public uint State3Wmo;
	public byte State3InitDoodadSet;
	public byte State3AmbientDoodadSet;
	public byte EjectDirection;
	public byte DoNotHighlight;
	public uint State0Wmo;
	public byte HealEffect;
	public ushort HealEffectSpeed;
	public byte State0NameSet;
	public byte State1NameSet;
	public byte State2NameSet;
	public byte State3NameSet;
}