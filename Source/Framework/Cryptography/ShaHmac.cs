// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Framework.Cryptography;

public class Sha256
{
    private readonly SHA256 sha;
    public byte[] Digest { get; private set; }

    public Sha256()
    {
        sha = SHA256.Create();
        sha.Initialize();
    }

    public void Process(byte[] data, int length)
    {
        sha.TransformBlock(data, 0, length, data, 0);
    }

    public void Process(uint data)
    {
        var bytes = BitConverter.GetBytes(data);

        sha.TransformBlock(bytes, 0, 4, bytes, 0);
    }

    public void Process(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);

        sha.TransformBlock(bytes, 0, bytes.Length, bytes, 0);
    }

    public void Finish(byte[] data)
    {
        sha.TransformFinalBlock(data, 0, data.Length);

        Digest = sha.Hash;
    }

    public void Finish(byte[] data, int offset, int length)
    {
        sha.TransformFinalBlock(data, offset, length);

        Digest = sha.Hash;
    }
}

public class HmacHash : HMACSHA1
{
    public byte[] Digest { get; private set; }

    public HmacHash(byte[] key) : base(key)
    {
        Initialize();
    }

    public void Process(byte[] data, int length)
    {
        TransformBlock(data, 0, length, data, 0);
    }

    public void Process(uint data)
    {
        var bytes = BitConverter.GetBytes(data);

        TransformBlock(bytes, 0, bytes.Length, bytes, 0);
    }

    public void Process(string data)
    {
        var bytes = Encoding.ASCII.GetBytes(data);

        TransformBlock(bytes, 0, bytes.Length, bytes, 0);
    }

    public void Finish(byte[] data, int length)
    {
        TransformFinalBlock(data, 0, length);

        Digest = Hash;
    }

    public void Finish(string data)
    {
        var bytes = Encoding.ASCII.GetBytes(data);

        TransformFinalBlock(bytes, 0, bytes.Length);

        Digest = Hash;
    }
}

public class HmacSha256 : HMACSHA256
{
    public byte[] Digest { get; private set; }

    public HmacSha256(byte[] key) : base(key)
    {
        Initialize();
    }

    public void Process(byte[] data, int length)
    {
        TransformBlock(data, 0, length, data, 0);
    }

    public void Process(uint data)
    {
        var bytes = BitConverter.GetBytes(data);

        TransformBlock(bytes, 0, bytes.Length, bytes, 0);
    }

    public void Process(string data)
    {
        var bytes = Encoding.ASCII.GetBytes(data);

        TransformBlock(bytes, 0, bytes.Length, bytes, 0);
    }

    public void Finish(byte[] data, int length)
    {
        TransformFinalBlock(data, 0, length);

        Digest = Hash;
    }
}