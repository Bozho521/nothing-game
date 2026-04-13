using Interfaces;
using UnityEngine;

public class Damageable : MonoBehaviour, IDestructable
{
    [Header("Destructable settings")]
    public float Health { get; set; } = 50;
    public int Armor { get; set; } = 0;
    
    [SerializeField] private AK.Wwise.Event wallDestroyedSound;
    [SerializeField] private AudioClip wallDestroyedClip;
    [SerializeField] private AudioSource webAudioSource;
    public void TakeDamage(float damage)
    {
        float finalDamage = Mathf.Max(0, damage - Armor);
        
        Health -= finalDamage;
        if (Health <= 0)
        {
            DestroyObject();
        }
    }

    public void DestroyObject()
    {
        PlayDestroyedSound();
        Destroy(gameObject); 
    }

    private void PlayDestroyedSound()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (wallDestroyedClip == null)
        {
            return;
        }

        if (webAudioSource == null)
        {
            webAudioSource = GetComponent<AudioSource>();
            if (webAudioSource == null)
            {
                webAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (webAudioSource != null)
        {
            webAudioSource.PlayOneShot(wallDestroyedClip);
        }
#else
        if (wallDestroyedSound != null)
        {
            wallDestroyedSound.Post(gameObject);
        }
#endif
    }
}