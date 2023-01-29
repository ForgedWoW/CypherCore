﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game
{
    public class TerrainSwapInfo
    {
        public uint Id { get; set; }
        public List<uint> UiMapPhaseIDs { get; set; } = new();

        public TerrainSwapInfo()
        {
        }

        public TerrainSwapInfo(uint id)
        {
            Id = id;
        }
    }
}