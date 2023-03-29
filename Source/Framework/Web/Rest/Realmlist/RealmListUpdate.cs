﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Runtime.Serialization;

namespace Framework.Web;

[DataContract]
public class RealmListUpdate
{
    [DataMember(Name = "update")] public RealmEntry Update { get; set; } = new();

    [DataMember(Name = "deleting")] public bool Deleting { get; set; }
}