public interface IDamageable
{
    public float Health { get; set; }
    public void TakeDamage(float amount, float damageMultiplier = 1);
    public void Heal(float amount);
}