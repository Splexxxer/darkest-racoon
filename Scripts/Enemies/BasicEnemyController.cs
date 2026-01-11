using Godot;

public partial class BasicEnemyController : CharacterBody2D
{
    [Export] public float Speed = 120f;           // slower than player
    [Export] public NodePath PlayerPath;          // drag your Player here in Inspector
    [Export] public float StopDistance = 0f;      // e.g. 8f if you want it to stop near player

    private Node2D _player;

    public override void _Ready()
    {
        if (PlayerPath != null && !PlayerPath.IsEmpty)
            _player = GetNode<Node2D>(PlayerPath);

        if (_player == null)
            GD.PrintErr("EnemyController: PlayerPath not set or player not found.");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_player == null)
        {
            Velocity = Vector2.Zero;
            return;
        }

        Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
        float dist = toPlayer.Length();

        if (StopDistance > 0f && dist <= StopDistance)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        Vector2 dir = dist > 0.001f ? toPlayer / dist : Vector2.Zero; // normalized
        Velocity = dir * Speed;

        MoveAndSlide();
    }
}