namespace Interfaces
{
    public interface IDestructable
    {
        float Health { get; set; }
        void TakeDamage(float damage);
        void DestroyObject();
    }
}
