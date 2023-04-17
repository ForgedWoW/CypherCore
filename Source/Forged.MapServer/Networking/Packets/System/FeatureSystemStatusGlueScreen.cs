// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.System;

public class FeatureSystemStatusGlueScreen : ServerPacket
{
    public int ActiveCharacterUpgradeBoostType;

    // NYI
    public int ActiveClassTrialBoostType;

    public int ActiveSeason;
    public bool AddonsDisabled;
    public bool BpayStoreAvailable;                  // NYI
    public bool BpayStoreDisabledByParentalControls; // NYI
    public bool BpayStoreEnabled;
    public uint BpayStoreProductDeliveryDelay;

    public bool CharUndeleteEnabled;

    // NYI
    public bool CommerceSystemEnabled; // NYI
    public bool CompetitiveModeEnabled;

    public EuropaTicketConfig? EuropaTicketSystemStatus;

    // Currently active Classic season
    public List<GameRuleValuePair> GameRuleValues = new();

    public bool IsExpansionPreorderInStore;

    // NYI
    public bool KioskModeEnabled;

    public uint KioskSessionMinutes;
    public int? LaunchETA;
    public bool LiveRegionAccountCopyEnabled;
    public bool LiveRegionCharacterCopyEnabled;
    public List<int> LiveRegionCharacterCopySourceRegions = new();

    public bool LiveRegionCharacterListEnabled;

    // NYI
    // NYI
    // NYI
    public bool LiveRegionKeyBindingsCopyEnabled;

    public int MaxCharactersPerRealm;
    public int MaximumExpansionLevel;

    public short MaxPlayerNameQueriesPerPacket = 50;

    // NYI
    // NYI
    public int MinimumExpansionLevel;

    public TimeSpan PlayerNameQueryInterval = TimeSpan.FromSeconds(10);
    public short PlayerNameQueryTelemetryInterval = 600;
    public long TokenBalanceAmount;
    public bool TokenBalanceEnabled;

    public uint TokenPollTimeSeconds;

    // NYI
    // NYI
    public bool TrialBoostEnabled;

    public bool Unk14; // NYI

    // NYI
    // NYI
    public bool Unknown901CheckoutRelated;

    // NYI
    public bool Unused1000;

    public bool WillKickFromWorld; // NYI

    // NYI
    // NYI 
    public FeatureSystemStatusGlueScreen() : base(ServerOpcodes.FeatureSystemStatusGlueScreen) { }

    public override void Write()
    {
        WorldPacket.WriteBit(BpayStoreEnabled);
        WorldPacket.WriteBit(BpayStoreAvailable);
        WorldPacket.WriteBit(BpayStoreDisabledByParentalControls);
        WorldPacket.WriteBit(CharUndeleteEnabled);
        WorldPacket.WriteBit(CommerceSystemEnabled);
        WorldPacket.WriteBit(Unk14);
        WorldPacket.WriteBit(WillKickFromWorld);
        WorldPacket.WriteBit(IsExpansionPreorderInStore);

        WorldPacket.WriteBit(KioskModeEnabled);
        WorldPacket.WriteBit(CompetitiveModeEnabled);
        WorldPacket.WriteBit(false); // unused, 10.0.2
        WorldPacket.WriteBit(TrialBoostEnabled);
        WorldPacket.WriteBit(TokenBalanceEnabled);
        WorldPacket.WriteBit(LiveRegionCharacterListEnabled);
        WorldPacket.WriteBit(LiveRegionCharacterCopyEnabled);
        WorldPacket.WriteBit(LiveRegionAccountCopyEnabled);

        WorldPacket.WriteBit(LiveRegionKeyBindingsCopyEnabled);
        WorldPacket.WriteBit(Unknown901CheckoutRelated);
        WorldPacket.WriteBit(false); // unused, 10.0.2
        WorldPacket.WriteBit(EuropaTicketSystemStatus.HasValue);
        WorldPacket.WriteBit(false); // unused, 10.0.2
        WorldPacket.WriteBit(LaunchETA.HasValue);
        WorldPacket.WriteBit(AddonsDisabled);
        WorldPacket.WriteBit(Unused1000);
        WorldPacket.FlushBits();

        EuropaTicketSystemStatus?.Write(WorldPacket);

        WorldPacket.WriteUInt32(TokenPollTimeSeconds);
        WorldPacket.WriteUInt32(KioskSessionMinutes);
        WorldPacket.WriteInt64(TokenBalanceAmount);
        WorldPacket.WriteInt32(MaxCharactersPerRealm);
        WorldPacket.WriteInt32(LiveRegionCharacterCopySourceRegions.Count);
        WorldPacket.WriteUInt32(BpayStoreProductDeliveryDelay);
        WorldPacket.WriteInt32(ActiveCharacterUpgradeBoostType);
        WorldPacket.WriteInt32(ActiveClassTrialBoostType);
        WorldPacket.WriteInt32(MinimumExpansionLevel);
        WorldPacket.WriteInt32(MaximumExpansionLevel);
        WorldPacket.WriteInt32(ActiveSeason);
        WorldPacket.WriteInt32(GameRuleValues.Count);
        WorldPacket.WriteInt16(MaxPlayerNameQueriesPerPacket);
        WorldPacket.WriteInt16(PlayerNameQueryTelemetryInterval);
        WorldPacket.WriteUInt32((uint)PlayerNameQueryInterval.TotalSeconds);

        if (LaunchETA.HasValue)
            WorldPacket.WriteInt32(LaunchETA.Value);

        foreach (var sourceRegion in LiveRegionCharacterCopySourceRegions)
            WorldPacket.WriteInt32(sourceRegion);

        foreach (var gameRuleValue in GameRuleValues)
            gameRuleValue.Write(WorldPacket);
    }
}