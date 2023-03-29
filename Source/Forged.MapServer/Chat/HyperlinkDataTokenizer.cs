// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.IO;

namespace Forged.MapServer.Chat;

internal class HyperlinkDataTokenizer
{
    private readonly StringArguments _arg;
    private readonly bool _allowEmptyTokens;

    public bool IsEmpty => _arg.Empty();

    public HyperlinkDataTokenizer(string arg, bool allowEmptyTokens = false)
    {
        _arg = new StringArguments(arg);
        _allowEmptyTokens = allowEmptyTokens;
    }

    public bool TryConsumeTo(out byte val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextByte(":");

        return true;
    }

    public bool TryConsumeTo(out ushort val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextUInt16(":");

        return true;
    }

    public bool TryConsumeTo(out uint val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextUInt32(":");

        return true;
    }

    public bool TryConsumeTo(out ulong val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextUInt64(":");

        return true;
    }

    public bool TryConsumeTo(out sbyte val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextSByte(":");

        return true;
    }

    public bool TryConsumeTo(out short val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextInt16(":");

        return true;
    }

    public bool TryConsumeTo(out int val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextInt32(":");

        return true;
    }

    public bool TryConsumeTo(out long val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextInt64(":");

        return true;
    }

    public bool TryConsumeTo(out float val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextSingle(":");

        return true;
    }

    public bool TryConsumeTo(out ObjectGuid val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = ObjectGuid.FromString(_arg.NextString(":"));

        return true;
    }

    public bool TryConsumeTo(out string val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextString(":");

        return true;
    }

    public bool TryConsumeTo(out bool val)
    {
        if (IsEmpty)
        {
            val = default;

            return _allowEmptyTokens;
        }

        val = _arg.NextBoolean(":");

        return true;
    }
}