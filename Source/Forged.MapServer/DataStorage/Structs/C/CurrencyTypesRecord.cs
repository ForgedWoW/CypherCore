// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class CurrencyTypesRecord
{
    public int AwardConditionID;
    public int CategoryID;
    public string Description;
    public int FactionID;
    public int[] Flags = new int[2];
    public uint Id;
    public int InventoryIconFileID;
    public int ItemGroupSoundsID;
    public uint MaxEarnablePerWeek;
    public uint MaxQty;
    public int MaxQtyWorldStateID;
    public string Name;
    public sbyte Quality;
    public uint RechargingAmountPerCycle;
    public uint RechargingCycleDurationMS;
    public byte SpellCategory;
    public uint SpellWeight;
    public int XpQuestDifficulty;

    public CurrencyTypesFlags GetFlags()
    {
        return (CurrencyTypesFlags)Flags[0];
    }

    public CurrencyTypesFlagsB GetFlagsB()
    {
        return (CurrencyTypesFlagsB)Flags[1];
    }

    // Helpers
    public int GetScaler()
    {
        return GetFlags().HasFlag(CurrencyTypesFlags._100_Scaler) ? 100 : 1;
    }

    public bool HasMaxEarnablePerWeek()
    {
        return MaxEarnablePerWeek != 0 || GetFlags().HasFlag(CurrencyTypesFlags.ComputedWeeklyMaximum);
    }

    public bool HasMaxQuantity(bool onLoad = false, bool onUpdateVersion = false)
    {
        if (onLoad && GetFlags().HasFlag(CurrencyTypesFlags.IgnoreMaxQtyOnLoad))
            return false;

        if (onUpdateVersion && GetFlags().HasFlag(CurrencyTypesFlags.UpdateVersionIgnoreMax))
            return false;

        return MaxQty != 0 || MaxQtyWorldStateID != 0 || GetFlags().HasFlag(CurrencyTypesFlags.DynamicMaximum);
    }

    public bool HasTotalEarned()
    {
        return GetFlagsB().HasFlag(CurrencyTypesFlagsB.UseTotalEarnedForEarned);
    }

    public bool IsAlliance()
    {
        return GetFlags().HasFlag(CurrencyTypesFlags.IsAllianceOnly);
    }

    public bool IsHorde()
    {
        return GetFlags().HasFlag(CurrencyTypesFlags.IsHordeOnly);
    }

    public bool IsSuppressingChatLog(bool onUpdateVersion = false)
    {
        if ((onUpdateVersion && GetFlags().HasFlag(CurrencyTypesFlags.SuppressChatMessageOnVersionChange)) ||
            GetFlags().HasFlag(CurrencyTypesFlags.SuppressChatMessages))
            return true;

        return false;
    }

    public bool IsTrackingQuantity()
    {
        return GetFlags().HasFlag(CurrencyTypesFlags.TrackQuantity);
    }
}