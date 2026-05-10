using Sandbox;
using System;

public class BasePickup : Component, IInteractable 
{
	[Property, Group("Основное")] public string ItemId { get; set; } = "test_item";
	[Property, Group("Основное")] public string DisplayName { get; set; } = "Неизвестный предмет";
	[Property, Group("Основное")] public int Amount { get; set; } = 1;

	// Новые свойства для Тетрис-Инвентаря
	[Property, Group("Инвентарь")] public int GridWidth { get; set; } = 1;
	[Property, Group("Инвентарь")] public int GridHeight { get; set; } = 1;
	// Иконка (можно использовать путь к модели, тогда в UI мы будем рендерить thumb:models/...vmdl)
	[Property, Group("Инвентарь")] public string IconPath { get; set; } = "models/editor/axis_helper.vmdl_c"; 

	public virtual void OnUse( GameObject player )
	{
		// 1. Ищем инвентарь на игроке
		var inventory = player.Components.Get<PlayerInventory>();
		
		if ( inventory != null )
		{
			// 2. Создаем структуру предмета
			var itemToAdd = InventoryItemState.Create( ItemId, IconPath, GridWidth, GridHeight );

			// 3. Пытаемся добавить. Метод вернет false, если нет места
			if ( inventory.TryAddItem( itemToAdd ) )
			{
				Log.Info( $"Игрок {player.Name} подобрал {DisplayName}!" );
				DestroyItemRpc(); // Удаляем только если предмет влез!
			}
			else
			{
				// Сюда можно добавить звук ошибки или UI-уведомление "Инвентарь полон"
				Log.Warning( "Нет места в инвентаре!" );
			}
		}
	}

	[Rpc.Broadcast]
	protected void DestroyItemRpc()
	{
		if ( GameObject.IsValid() )
			GameObject.Destroy();
	}
}