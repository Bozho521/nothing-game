using UnityEngine;

public class FloatingText : MonoBehaviour
{
    public float moveSpeed = 1.5f;
    public float destroyTime = 1.5f;

    private void Start()
    {
        Destroy(gameObject, destroyTime);
    }

    private void Update()
    {
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180, 0);
    }
}