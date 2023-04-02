// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.L;

public sealed class LiquidTypeRecord
{
    public float AmbDarkenIntensity;
    public float[] Coefficient = new float[4];
    public int[] Color = new int[2];
    public float DirDarkenIntensity;
    public ushort Flags;
    public float[] Float = new float[18];
    public float FogDarkenIntensity;
    public byte[] FrameCountTexture = new byte[6];
    public uint Id;
    public uint[] Int = new uint[4];
    public ushort LightID;
    public byte MaterialID;
    public float MaxDarkenDepth;
    public int MinimapStaticCol;
    public string Name;
    public byte ParticleMovement;
    public float ParticleScale;
    public byte ParticleTexSlots;
    public byte SoundBank;
    // used to be "type", maybe needs fixing (works well for now)
    public uint SoundID;

    public uint SpellID;
    public string[] Texture = new string[6];
}