using UnityEngine;

public class BloodRainManager : MonoBehaviour
{
    [Header("Blood Rain Settings")]
    public ParticleSystem bloodRainParticles;
    public int startKills = 10;
    public int maxKills = 50;
    
    [Header("Dynamic Scaling")]
    public Vector3 startScale = new Vector3(200f, 1f, 200f);
    public Vector3 maxScale = new Vector3(10f, 1f, 10f);
    public float scaleTransitionSpeed = 2f;

    [Header("Optional Atmosphere")]
    public Light directionalLight;
    public Color bloodLightColor = new Color(0.6f, 0f, 0f); 
    public float lightTransitionSpeed = 0.5f;

    [SerializeField] private AK.Wwise.Event thunderSound; 
    
    private bool isRaining = false;
    private Color originalLightColor;

    private void Start()
    {
        if (bloodRainParticles != null)
        {
            bloodRainParticles.Stop();
            
            var shape = bloodRainParticles.shape;
            shape.scale = startScale; 
        }

        if (directionalLight != null)
        {
            originalLightColor = directionalLight.color;
        }
    }

    private void Update()
    {
        if (!isRaining && EnemyManager.killCount >= startKills)
        {
            StartBloodRain();
        }

        if (isRaining && bloodRainParticles != null)
        {
            float stormProgress = Mathf.Clamp01((float)(EnemyManager.killCount - startKills) / (maxKills - startKills));
            
            Vector3 targetScale = Vector3.Lerp(startScale, maxScale, stormProgress);

            var shape = bloodRainParticles.shape;
            shape.scale = Vector3.Lerp(shape.scale, targetScale, Time.deltaTime * scaleTransitionSpeed);
            
            if (directionalLight != null)
            {
                directionalLight.color = Color.Lerp(directionalLight.color, bloodLightColor, Time.deltaTime * lightTransitionSpeed);
            }
        }
    }

    private void StartBloodRain()
    {
        isRaining = true;
        
        if (bloodRainParticles != null)
        {
            bloodRainParticles.Play();
        }

        if (thunderSound != null)
        {
            thunderSound.Post(gameObject);
        }
    }
}