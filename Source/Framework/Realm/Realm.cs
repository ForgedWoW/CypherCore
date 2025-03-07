﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net;
using System.Net.Sockets;
using Framework.Constants;
using Framework.Realm;

public class Realm : IEquatable<Realm>
{
	public RealmId Id;
	public uint Build;
	public IPAddress ExternalAddress;
	public IPAddress LocalAddress;
	public IPAddress LocalSubnetMask;
	public ushort Port;
	public string Name;
	public string NormalizedName;
	public byte Type;
	public RealmFlags Flags;
	public byte Timezone;
	public AccountTypes AllowedSecurityLevel;
	public float PopulationLevel;

	readonly uint[] ConfigIdByType =
	{
		1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14
	};

	public bool Equals(Realm other)
	{
		return other.ExternalAddress.Equals(ExternalAddress) && other.LocalAddress.Equals(LocalAddress) && other.LocalSubnetMask.Equals(LocalSubnetMask) && other.Port == Port && other.Name == Name && other.Type == Type && other.Flags == Flags && other.Timezone == Timezone && other.AllowedSecurityLevel == AllowedSecurityLevel && other.PopulationLevel == PopulationLevel;
	}

	public void SetName(string name)
	{
		Name = name;
		NormalizedName = name;
		NormalizedName = NormalizedName.Replace(" ", "");
	}

	public IPEndPoint GetAddressForClient(IPAddress clientAddr)
	{
		IPAddress realmIp;

		// Attempt to send best address for client
		if (IPAddress.IsLoopback(clientAddr))
		{
			// Try guessing if realm is also connected locally
			if (IPAddress.IsLoopback(LocalAddress) || IPAddress.IsLoopback(ExternalAddress))
				realmIp = clientAddr;
			else
				// Assume that user connecting from the machine that authserver is located on
				// has all realms available in his local network
				realmIp = LocalAddress;
		}
		else
		{
			if (clientAddr.AddressFamily == AddressFamily.InterNetwork && clientAddr.GetNetworkAddress(LocalSubnetMask).Equals(LocalAddress.GetNetworkAddress(LocalSubnetMask)))
				realmIp = LocalAddress;
			else
				realmIp = ExternalAddress;
		}

		IPEndPoint endpoint = new(realmIp, Port);

		// Return external IP
		return endpoint;
	}

	public uint GetConfigId()
	{
		return ConfigIdByType[Type];
	}

	public override bool Equals(object obj)
	{
		return obj != null && obj is Realm && Equals((Realm)obj);
	}

	public override int GetHashCode()
	{
		return new
		{
			ExternalAddress,
			LocalAddress,
			LocalSubnetMask,
			Port,
			Name,
			Type,
			Flags,
			Timezone,
			AllowedSecurityLevel,
			PopulationLevel
		}.GetHashCode();
	}
}