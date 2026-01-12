using Godot;
using Godot.Collections;

public partial class CollisonChecker : CollisionShape2D
{
	[Export(PropertyHint.Range, "0,200,1")] public float SeparationDistance = 40f;
	[Export(PropertyHint.Range, "0.1,1,0.05")] public float SeparationFactor = 0.5f;
	[Export(PropertyHint.Range, "1,32,1")] public int MaxResults = 8;

	private BasicEnemyController _enemy;

	public override void _Ready()
	{
		_enemy = GetParent() as BasicEnemyController;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_enemy == null || Shape == null || SeparationDistance <= 0f)
			return;

		if (_enemy.IsExperiencingKnockback)
			return;

		PhysicsDirectSpaceState2D space = GetWorld2D()?.DirectSpaceState;
		if (space == null)
			return;

		var parameters = new PhysicsShapeQueryParameters2D
		{
			Shape = Shape,
			Transform = GlobalTransform,
			CollisionMask = _enemy.CollisionMask,
			Margin = 0.01f,
			Exclude = new Array<Rid> { _enemy.GetRid() }
		};

		var results = space.IntersectShape(parameters, MaxResults);
		Vector2 separationMotion = Vector2.Zero;

		foreach (Dictionary hit in results)
		{
			if (!hit.TryGetValue("collider", out var colliderVariant))
				continue;

			GodotObject colliderObj = colliderVariant.AsGodotObject();
			if (colliderObj is not BasicEnemyController other || other == _enemy)
				continue;

			if (other.IsExperiencingKnockback)
				continue;

			Vector2 diff = _enemy.GlobalPosition - other.GlobalPosition;
			float distance = diff.Length();
			if (distance < 0.001f)
			{
				diff = Vector2.Right;
				distance = 0.001f;
			}

			if (distance >= SeparationDistance)
				continue;

			float penetration = SeparationDistance - distance;
			separationMotion += diff.Normalized() * (penetration * SeparationFactor);
		}

		if (separationMotion != Vector2.Zero)
			_enemy.ApplySeparation(separationMotion);
	}
}
