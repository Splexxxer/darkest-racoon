using Godot;

public partial class BasicEnemyController : CharacterBody2D, IPlayerTargeted
{
	[Export] public float Speed = 120f;
	[Export] public float StopDistance = 0f;

	public Node2D Target { get; set; }

	public override void _PhysicsProcess(double delta)
	{
		if (Target == null)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 toPlayer = Target.GlobalPosition - GlobalPosition;
		float dist = toPlayer.Length();

		if (StopDistance > 0f && dist <= StopDistance)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		Vector2 dir = dist > 0.001f ? toPlayer / dist : Vector2.Zero;
		Velocity = dir * Speed;
		MoveAndSlide();
	}
}
