using Sandbox;

public sealed class ArmorPickup : BasePickup
{
	[Property] public int ArmorAmount { get; set; } = 100;

	// Ссылка на сам ресурс одежды (.clothing), который мы наденем
	// Вы сможете перетащить файл бронежилета сюда прямо в инспекторе редактора
	[Property] public Clothing ArmorClothingItem { get; set; }

	public override void OnUse( GameObject player )
	{
		// 1. Пытаемся найти компонент характеристик на игроке
		var stats = player.Components.Get<PlayerStats>();
		
		if ( stats != null )
		{
			// Добавляем броню
			stats.AddArmor( ArmorAmount );
			
			Log.Info( $"Игрок {player.Name} надел бронежилет: +{ArmorAmount} брони" );

			// 2. Вызываем сетевое надевание одежды на игрока
			if ( ArmorClothingItem != null )
			{
				EquipArmorRpc( player );
			}

			// 3. Вызываем базовое сетевое удаление предмета из BasePickup
			DestroyItemRpc();
		}
		else
		{
			Log.Warning( "Не удалось найти компонент статистики на игроке!" );
		}
	}

	/// <summary>
	/// RPC-метод, который заставит всех игроков на сервере увидеть, 
	/// что этот персонаж надел бронежилет.
	/// </summary>
	[Rpc.Broadcast]
	private void EquipArmorRpc( GameObject targetPlayer )
	{
		// RPC может принимать GameObject как параметр, так что мы легко находим нужного игрока
		if ( targetPlayer == null ) return;

		// Ищем компонент Dresser на префабе игрока (на самом объекте или его детях)
		var dresser = targetPlayer.Components.Get<Dresser>( FindMode.EverythingInSelfAndDescendants );
		
		if ( dresser != null && ArmorClothingItem != null )
		{
			// Создаем новую запись для списка одежды
			var newEntry = new Sandbox.ClothingContainer.ClothingEntry 
			{ 
				Clothing = ArmorClothingItem 
			};

			// Добавляем бронежилет в список надетых вещей
			dresser.Clothing.Add( newEntry );

			// Метод Apply() заново собирает меши и натягивает новую одежду на модель
			dresser.Apply();
		}
	}
}