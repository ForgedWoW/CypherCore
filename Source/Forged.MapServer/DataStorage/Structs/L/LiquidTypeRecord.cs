// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.L;

public sealed class LiquidTypeRecord
{
	public uint Id;
	public string Name;
	public string[] Texture = new string[6];
	public ushort Flags;
	public byte SoundBank; // used to be "type", maybe needs fixing (works well for now)
	public uint SoundID;
	public uint SpellID;
	public float MaxDarkenDepth;
	public float FogDarkenIntensity;
	public float AmbDarkenIntensity;
	public float DirDarkenIntensity;
	public ushort LightID;
	public float ParticleScale;
	public byte ParticleMovement;
	public byte ParticleTexSlots;
	public byte MaterialID;
	public int MinimapStaticCol;
	public byte[] FrameCountTexture = new byte[6];
	public int[] Color = new int[2];
	public float[] Float = new float[18];
	public uint[] Int = new uint[4];
	public float[] Coefficient = new float[4];
}