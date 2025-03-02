﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Text.RegularExpressions;

namespace Framework.IO
{
    public sealed class StringArguments
    {
        public StringArguments(string args)
        {
            if (!args.IsEmpty())
                activestring = args.TrimStart(' ');
            activeposition = -1;
        }

        public StringArguments(StringArguments args)
        {
            activestring = args.activestring;
            activeposition = args.activeposition;
            Current = args.Current;
        }

        public bool Empty()
        {
            return activestring.IsEmpty();
        }

        public void MoveToNextChar(char c)
        {
            for (var i = activeposition; i < activestring.Length; ++i)
                if (activestring[i] == c)
                    break;
        }

        public string NextString(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return "";

            return Current;
        }

        public bool NextBoolean(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return false;

            if (bool.TryParse(Current, out bool value))
                return value;

            if ((Current == "1") || Current.Equals("y", StringComparison.OrdinalIgnoreCase) || Current.Equals("on", StringComparison.OrdinalIgnoreCase) || Current.Equals("yes", StringComparison.OrdinalIgnoreCase) || Current.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return true;
            if ((Current == "0") || Current.Equals("n", StringComparison.OrdinalIgnoreCase) || Current.Equals("off", StringComparison.OrdinalIgnoreCase) || Current.Equals("no", StringComparison.OrdinalIgnoreCase) || Current.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;

            return false;
        }

        public char NextChar(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (char.TryParse(Current, out char value))
                return value;

            return default;
        }

        public byte NextByte(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (byte.TryParse(Current, out byte value))
                return value;

            return default;
        }

        public sbyte NextSByte(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (sbyte.TryParse(Current, out sbyte value))
                return value;

            return default;
        }

        public ushort NextUInt16(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (ushort.TryParse(Current, out ushort value))
                return value;

            return default;
        }

        public short NextInt16(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (short.TryParse(Current, out short value))
                return value;

            return default;
        }

        public uint NextUInt32(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (uint.TryParse(Current, out uint value))
                return value;

            return default;
        }

        public int NextInt32(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (int.TryParse(Current, out int value))
                return value;

            return default;
        }

        public ulong NextUInt64(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (ulong.TryParse(Current, out ulong value))
                return value;

            return default;
        }

        public long NextInt64(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (long.TryParse(Current, out long value))
                return value;

            return default;
        }

        public float NextSingle(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (float.TryParse(Current, out float value))
                return value;

            return default;
        }

        public double NextDouble(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (double.TryParse(Current, out double value))
                return value;

            return default;
        }

        public decimal NextDecimal(string delimiters = " ")
        {
            if (!MoveNext(delimiters))
                return default;

            if (decimal.TryParse(Current, out decimal value))
                return value;

            return default;
        }

        public void AlignToNextChar()
        {
            while (activeposition < activestring.Length && activestring[activeposition] != ' ')            
                activeposition++;            
        }

        public char this[int index]
        {
            get { return activestring[index]; }
        }

        public string GetString()
        {
            return activestring;
        }

        public void Reset()
        {
            activeposition = -1;
            Current = null;
        }

        public bool IsAtEnd()
        {
            return activestring.IsEmpty() || activeposition == activestring.Length;
        }

        public int GetCurrentPosition()
        {
            return activeposition;
        }

        public void SetCurrentPosition(int currentPosition)
        {
            activeposition = currentPosition;
        }

        bool MoveNext(string delimiters)
        {
            //the stringtotokenize was never set:
            if (activestring == null)
                return false;

            //all tokens have already been extracted:
            if (activeposition == activestring.Length)
                return false;

            //bypass delimiters:
            activeposition++;
            while (activeposition < activestring.Length && delimiters.IndexOf(activestring[activeposition]) > -1)
            {
                activeposition++;
            }

            //only delimiters were left, so return null:
            if (activeposition == activestring.Length)
                return false;

            //get starting position of string to return:
            int startingposition = activeposition;

            //read until next delimiter:
            do
            {
                activeposition++;
            } while (activeposition < activestring.Length && delimiters.IndexOf(activestring[activeposition]) == -1);

            Current = activestring.Substring(startingposition, activeposition - startingposition);
            return true;
        }

        bool Match(string pattern, out Match m)
        {
            Regex r = new(pattern);
            m = r.Match(activestring);
            return m.Success;
        }

        private readonly string activestring;
        private int activeposition;
        private string Current;
    }
}
