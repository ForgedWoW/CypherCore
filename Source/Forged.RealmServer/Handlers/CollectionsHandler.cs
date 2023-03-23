// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Collection;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.CollectionItemSetFavorite)]
	void HandleCollectionItemSetFavorite(CollectionItemSetFavorite collectionItemSetFavorite)
	{
		switch (collectionItemSetFavorite.Type)
		{
			case CollectionType.Toybox:
				CollectionMgr.ToySetFavorite(collectionItemSetFavorite.Id, collectionItemSetFavorite.IsFavorite);

				break;
			case CollectionType.Appearance:
			{
				var pair = CollectionMgr.HasItemAppearance(collectionItemSetFavorite.Id);

				if (!pair.Item1 || pair.Item2)
					return;

				CollectionMgr.SetAppearanceIsFavorite(collectionItemSetFavorite.Id, collectionItemSetFavorite.IsFavorite);

				break;
			}
			case CollectionType.TransmogSet:
				break;
			default:
				break;
		}
	}
}