﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Cryptography.Ed25519.Internal
{
    // Array16<UInt32> Salsa20 state
    // Array16<UInt64> SHA-512 block
    internal struct Array16<T>
    {
        public T x0;
        public T x1;
        public T x2;
        public T x3;
        public T x4;
        public T x5;
        public T x6;
        public T x7;
        public T x8;
        public T x9;
        public T x10;
        public T x11;
        public T x12;
        public T x13;
        public T x14;
        public T x15;
    }
}
