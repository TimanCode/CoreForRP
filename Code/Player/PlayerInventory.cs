using Sandbox;
using System;
using System.Linq;

public struct InventoryItemState
{
	public Guid InstanceId { get; set; } // Уникальный ID конкретно этой вещи в рюкзаке
	public string ItemId { get; set; }   // Текстовый ID ("water_bottle")
	public string IconPath { get; set; } // Путь к иконке или модели
	public int Width { get; set; }
	public int Height { get; set; }
	public int X { get; set; }
	public int Y { get; set; }
	public bool Rotated { get; set; }

	public static InventoryItemState Create( string itemId, string icon, int width, int height )
	{
		return new InventoryItemState
		{
			InstanceId = Guid.NewGuid(),
			ItemId = itemId,
			IconPath = icon,
			Width = width,
			Height = height,
			X = 0,
			Y = 0,
			Rotated = false
		};
	}
}

public sealed partial class PlayerInventory : Component
{
	[Property] public int GridWidth { get; set; } = 8;
	[Property] public int GridHeight { get; set; } = 8;

	// Синхронизируем инвентарь от хоста к клиенту
	[Sync(SyncFlags.FromHost)] public NetList<InventoryItemState> Items { get; set; } = new();

	public bool TryAddItem( InventoryItemState item )
	{
		if ( !Networking.IsHost )
		{
			RequestAddItem( item );
			return false;
		}

		var foundSpot = FindFreeSpace( item.Width, item.Height );
		if ( foundSpot == null ) return false;

		item.X = foundSpot.Value.x;
		item.Y = foundSpot.Value.y;

		Items.Add( item );
		return true;
	}

	public bool TryMoveItem( Guid instanceId, int newX, int newY, bool rotated = false )
	{
		if ( !Networking.IsHost )
		{
			RequestMoveItem( instanceId, newX, newY, rotated );
			return false;
		}

		var itemIndex = GetItemIndex( instanceId );
		if ( itemIndex == -1 ) return false;

		var item = Items[itemIndex];
		int width = rotated ? item.Height : item.Width;
		int height = rotated ? item.Width : item.Height;

		if ( !CanPlaceItem( instanceId, newX, newY, width, height ) )
			return false;

		item.X = newX;
		item.Y = newY;
		item.Rotated = rotated;

		Items[itemIndex] = item; // Обновляем структуру в NetList
		return true;
	}

	// === Вспомогательные методы ===

	private (int x, int y)? FindFreeSpace( int width, int height )
	{
		for ( int y = 0; y <= GridHeight - height; y++ )
		{
			for ( int x = 0; x <= GridWidth - width; x++ )
			{
				if ( CanPlaceItem( Guid.Empty, x, y, width, height ) )
					return (x, y);
			}
		}
		return null;
	}

	private bool CanPlaceItem( Guid ignoredId, int x, int y, int width, int height )
	{
		if ( x < 0 || y < 0 || x + width > GridWidth || y + height > GridHeight )
			return false;

		foreach ( var item in Items )
		{
			if ( item.InstanceId == ignoredId ) continue;

			int itemW = item.Rotated ? item.Height : item.Width;
			int itemH = item.Rotated ? item.Width : item.Height;

			bool overlaps = x < item.X + itemW && x + width > item.X &&
							y < item.Y + itemH && y + height > item.Y;

			if ( overlaps ) return false;
		}
		return true;
	}

	private int GetItemIndex( Guid instanceId )
	{
		for ( int i = 0; i < Items.Count; i++ )
		{
			if ( Items[i].InstanceId == instanceId ) return i;
		}
		return -1;
	}

	// === RPC Методы ===
	[Rpc.Host] private void RequestAddItem( InventoryItemState item ) => TryAddItem( item );
	[Rpc.Host] private void RequestMoveItem( Guid instanceId, int x, int y, bool rotated ) => TryMoveItem( instanceId, x, y, rotated );
}