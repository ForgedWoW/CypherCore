// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.E;

public sealed record ExpectedStatModRecord
{
    public float ArmorConstantMod;
    public float CreatureArmorMod;
    public float CreatureAutoAttackDPSMod;
    public float CreatureHealthMod;
    public float CreatureSpellDamageMod;
    public uint Id;
    public float PlayerHealthMod;
    public float PlayerManaMod;
    public float PlayerPrimaryStatMod;
    public float PlayerSecondaryStatMod;
}