// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.AttackSwing, Processing = PacketProcessing.Inplace)]
        void HandleAttackSwing(AttackSwing packet)
        {
            Unit enemy = Global.ObjAccessor.GetUnit(Player, packet.Victim);
            if (!enemy)
            {
                // stop attack state at client
                SendAttackStop(null);
                return;
            }

            if (!Player.IsValidAttackTarget(enemy))
            {
                // stop attack state at client
                SendAttackStop(enemy);
                return;
            }

            //! Client explicitly checks the following before sending CMSG_ATTACKSWING packet,
            //! so we'll place the same check here. Note that it might be possible to reuse this snippet
            //! in other places as well.
            Vehicle vehicle = Player.Vehicle1;
            if (vehicle)
            {
                VehicleSeatRecord seat = vehicle.GetSeatForPassenger(Player);
                Cypher.Assert(seat != null);
                if (!seat.HasFlag(VehicleSeatFlags.CanAttack))
                {
                    SendAttackStop(enemy);
                    return;
                }
            }

            Player.Attack(enemy, true);
        }

        [WorldPacketHandler(ClientOpcodes.AttackStop, Processing = PacketProcessing.Inplace)]
        void HandleAttackStop(AttackStop packet)
        {
            Player.AttackStop();
        }

        [WorldPacketHandler(ClientOpcodes.SetSheathed, Processing = PacketProcessing.Inplace)]
        void HandleSetSheathed(SetSheathed packet)
        {
            if (packet.CurrentSheathState >= (int)SheathState.Max)
            {
                Log.outError(LogFilter.Network, "Unknown sheath state {0} ??", packet.CurrentSheathState);
                return;
            }

            Player.
            Sheath = (SheathState)packet.CurrentSheathState;
        }

        void SendAttackStop(Unit enemy)
        {
            SendPacket(new SAttackStop(Player, enemy));
        }
    }
}
