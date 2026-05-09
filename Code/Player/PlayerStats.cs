using Sandbox;
using System;

public sealed class PlayerStats : Component
{
	[Property, Sync, Group( "Health" )] public float MaxHealth { get; set; } = 100f;
	[Property, Sync, Group( "Health" )] public float Health { get; set; } = 100f;

	[Property, Sync, Group( "Armor" )] public float MaxArmor { get; set; } = 100f;
	[Property, Sync, Group( "Armor" )] public float Armor { get; set; } = 0f;

	[Property, Sync, Group( "Survival" )] public float Hunger { get; set; } = 100f;
	[Property, Sync, Group( "Survival" )] public float Thirst { get; set; } = 100f;

	/// <summary>
	/// Событие смерти для вызова логики на клиенте (например, UI или звуки)
	/// </summary>
	public Action OnDeath { get; set; }

	protected override void OnUpdate()
	{
		// Логика постепенного голода/жажды только на стороне хоста
		if ( !IsProxy && Networking.IsHost )
		{
			TickSurvivalStats();
		}
	}

	private void TickSurvivalStats()
	{
		// Уменьшаем показатели со временем
		Hunger = (Hunger - 0.01f * Time.Delta).Clamp( 0, 100 );
		Thirst = (Thirst - 0.02f * Time.Delta).Clamp( 0, 100 );

		// Если игрок умирает от голода
		if ( Hunger <= 0 || Thirst <= 0 )
		{
			TakeDamage( 1f * Time.Delta );
		}
	}

	/// <summary>
	/// Безопасное получение урона. Влияет на броню, затем на здоровье.
	/// </summary>
	public void TakeDamage( float damage )
	{
		// Только хост имеет право изменять здоровье
		if ( IsProxy ) return;

		if ( Armor > 0 )
		{
			// 70% урона в броню, 30% в тело
			float armorDamage = damage * 0.7f;
			float healthDamage = damage * 0.3f;

			if ( Armor >= armorDamage )
			{
				Armor -= armorDamage;
				Health -= healthDamage;
			}
			else
			{
				// Если брони меньше, чем урона
				float leftover = armorDamage - Armor;
				Armor = 0;
				Health -= (healthDamage + leftover);
			}
		}
		else
		{
			Health -= damage;
		}

		Health = Health.Clamp( 0, MaxHealth );

		if ( Health <= 0 )
		{
			BroadcastDeath();
		}
	}

	public void AddArmor( float amount )
	{
		if ( IsProxy ) return;
		Armor = (Armor + amount).Clamp( 0, MaxArmor );
	}

	public void AddHealth( float amount )
	{
		if ( IsProxy ) return;
		Health = (Health + amount).Clamp( 0, MaxHealth );
	}

	/// <summary>
	/// Оповещаем всех о смерти игрока
	/// </summary>
	[Rpc.Broadcast]
	private void BroadcastDeath()
	{
		OnDeath?.Invoke();
		Log.Info( $"{GameObject.Name} погиб." );
		
		// Здесь можно вызвать анимацию смерти или смену модели
	}
}