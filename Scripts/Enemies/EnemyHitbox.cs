using Godot;

public partial class EnemyHitbox : Area2D, IDamageSource
{
    [Export] public int DamageValue = 1;
    public int Damage => DamageValue;
}