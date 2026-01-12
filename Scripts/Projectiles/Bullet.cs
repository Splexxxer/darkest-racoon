using Godot;

public partial class Bullet : Node2D
{
	[Export] public int BaseDamage = 1;
	public int Damage { get; set; }
	public CharacterController Shooter { get; set; }

	private Area2D _hitbox;
	private bool _hasHit;

	public override void _Ready()
	{
		Damage = Damage > 0 ? Damage : Mathf.Max(1, BaseDamage);
		_hitbox = GetNodeOrNull<Area2D>("Area2D");
		if (_hitbox != null)
		{
			_hitbox.AreaEntered += OnAreaEntered;
			_hitbox.BodyEntered += OnBodyEntered;
		}
	}

	private void OnAreaEntered(Area2D area)
	{
		TryDealDamage(area);
	}

	private void OnBodyEntered(Node2D body)
	{
		TryDealDamage(body);
	}

	private void TryDealDamage(Node collider)
	{
		if (_hasHit || collider == null)
			return;

		EnemyHealth health = FindEnemyHealth(collider);
		if (health == null)
			return;

		health.TakeDamage(Damage);
		_hasHit = true;
		Shooter?.NotifyBulletRemoved(this);
		QueueFree();
	}

	private EnemyHealth FindEnemyHealth(Node node)
	{
		Node current = node;
		int steps = 0;
		while (current != null && steps++ < 4)
		{
			if (current is EnemyHealth health)
				return health;

			if (current is BasicEnemyController enemy)
			{
				var enemyHealth = enemy.GetNodeOrNull<EnemyHealth>("Health");
				if (enemyHealth != null)
					return enemyHealth;
			}

			current = current.GetParent();
		}

		return null;
	}
}
