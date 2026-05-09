using Sandbox;

public sealed class ArmorPickup : BasePickup
{
	[Property] public int ArmorAmount { get; set; } = 100;

	public override void OnUse( GameObject player )
	{
		// 1. Пытаемся найти компонент характеристик на игроке
		// Предположим, у вас есть или будет компонент PlayerStats или HealthComponent
		var stats = player.Components.Get<PlayerStats>();
		
		if ( stats != null )
		{
			// Добавляем броню (логика ограничения максимума обычно внутри AddArmor)
			stats.AddArmor( ArmorAmount );
			
			Log.Info( $"Игрок {player.Name} надел бронежилет: +{ArmorAmount} брони" );

			// 2. Вызываем базовое сетевое удаление предмета из BasePickup
			DestroyItemRpc();
		}
		else
		{
			Log.Warning( "Не удалось найти компонент статистики на игроке!" );
		}
	}
}