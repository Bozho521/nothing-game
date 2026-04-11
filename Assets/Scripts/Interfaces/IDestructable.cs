namespace Interfaces
{
    public interface IDestructable
    {
        float Health { get; set; }
        
        // Protects higher tier destructables from lower tier weapons
        int Armor { get; set; }
        void TakeDamage(float damage);
        void DestroyObject();
    }
}
