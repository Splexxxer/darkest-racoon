using Godot;

public partial class PlayerHurtbox : Area2D
{
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
        }
    }
}