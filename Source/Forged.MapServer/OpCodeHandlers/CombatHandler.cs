// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Combat;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.OpCodeHandlers;

public class CombatHandler : IWorldSessionHandler
{
    [WorldPacketHandler(ClientOpcodes.AttackStop, Processing = PacketProcessing.Inplace)]
    private void HandleAttackStop(AttackStop packet)
    {
        Player.AttackStop();
    }

    [WorldPacketHandler(ClientOpcodes.AttackSwing, Processing = PacketProcessing.Inplace)]
    private void HandleAttackSwing(AttackSwing packet)
    {
        var enemy = Global.ObjAccessor.GetUnit(Player, packet.Victim);

        if (!enemy)
        {
            // stop attack state at client
            SendAttackStop(null);

            return;
        }

        if (!Player.WorldObjectCombat.IsValidAttackTarget(enemy))
        {
            // stop attack state at client
            SendAttackStop(enemy);

            return;
        }

        //! Client explicitly checks the following before sending CMSG_ATTACKSWING packet,
        //! so we'll place the same check here. Note that it might be possible to reuse this snippet
        //! in other places as well.
        var vehicle = Player.Vehicle;

        if (vehicle)
        {
            var seat = vehicle.GetSeatForPassenger(Player);

            if (!seat.HasFlag(VehicleSeatFlags.CanAttack))
            {
                SendAttackStop(enemy);

                return;
            }
        }

        Player.Attack(enemy, true);
    }

    [WorldPacketHandler(ClientOpcodes.SetSheathed, Processing = PacketProcessing.Inplace)]
    private void HandleSetSheathed(SetSheathed packet)
    {
        if (packet.CurrentSheathState >= (int)SheathState.Max)
        {
            Log.Logger.Error("Unknown sheath state {0} ??", packet.CurrentSheathState);

            return;
        }

        Player.Sheath = (SheathState)packet.CurrentSheathState;
    }

    private void SendAttackStop(Unit enemy)
    {
        SendPacket(new SAttackStop(Player, enemy));
    }
}