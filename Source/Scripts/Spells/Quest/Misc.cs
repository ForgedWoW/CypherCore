// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Scripts.Spells.Quest;

internal struct Misc
{
    //Quests6124 6129
    public static TimeSpan DespawnTime = TimeSpan.FromSeconds(30);

    //HodirsHelm
    public const byte SAY1 = 1;
    public const byte SAY2 = 2;

    //Acleansingsong
    public const uint AREA_ID_BITTERTIDELAKE = 4385;
    public const uint AREA_ID_RIVERSHEART = 4290;
    public const uint AREA_ID_WINTERGRASPRIVER = 4388;

    //Quest12372
    public const uint WHISPER_ON_HIT_BY_FORCE_WHISPER = 1;

    //BurstAtTheSeams
    public const uint AREA_THE_BROKEN_FRONT = 4507;
    public const uint AREA_MORD_RETHAR_THE_DEATH_GATE = 4508;
    public const uint QUEST_FUEL_FOR_THE_FIRE = 12690;
}