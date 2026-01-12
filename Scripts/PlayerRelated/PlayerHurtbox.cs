using Godot;

public partial class PlayerHurtbox : Area2D
{
    // -1 uses the enemy's configured KnockbackDistance, 0 disables knockback entirely
    [Export(PropertyHint.Range, "-1,500,1")] public float EnemyKnockbackDistance = -1f;

    private Health _health;

    public override void _Ready()
    {
        _health = GetParent().GetNodeOrNull<Health>("Health");
        if (_health == null)
        {
            GD.PrintErr("PlayerHurtbox: Could not find Health node at Player/Health.");
            return;
        }

        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area2D area)
    {
        // Generic: any Area2D that implements IDamageSource can damage the player
        if (area is IDamageSource dmg)
        {
            _health.TakeDamage(dmg.Damage);
            KnockbackEnemy(area);
        }
    }

    private void KnockbackEnemy(Area2D area)
    {
        Node parent = GetParent();
        if (parent is not Node2D playerBody)
            return;

        BasicEnemyController enemy = area.GetParent() as BasicEnemyController
            ?? area.Owner as BasicEnemyController;
        if (enemy == null)
            return;

        Vector2 dir = enemy.GlobalPosition - playerBody.GlobalPosition;
        if (dir.LengthSquared() < 0.0001f)
            return;

        if (EnemyKnockbackDistance == 0f)
            return;

        float distanceOverride = EnemyKnockbackDistance > 0f ? EnemyKnockbackDistance : -1f;
        enemy.ApplyKnockback(dir, distanceOverride);
    }
}
