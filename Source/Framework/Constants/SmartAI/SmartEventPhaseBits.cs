// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SmartEventPhaseBits
{
    PhaseAlwaysBit = 0,
    Phase1Bit = 1,
    Phase2Bit = 2,
    Phase3Bit = 4,
    Phase4Bit = 8,
    Phase5Bit = 16,
    Phase6Bit = 32,
    Phase7Bit = 64,
    Phase8Bit = 128,
    Phase9Bit = 256,
    Phase10Bit = 512,
    Phase11Bit = 1024,
    Phase12Bit = 2048,

    All = Phase1Bit +
          Phase2Bit +
          Phase3Bit +
          Phase4Bit +
          Phase5Bit +
          Phase6Bit +
          Phase7Bit +
          Phase8Bit +
          Phase9Bit +
          Phase10Bit +
          Phase11Bit +
          Phase12Bit
}