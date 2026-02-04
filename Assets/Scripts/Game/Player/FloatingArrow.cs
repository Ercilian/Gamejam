using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    public float amplitude = 0.25f;
    public float Speed = 2f;
    private Vector3 startLocalPosition;

    void Start()
    {
        startLocalPosition = transform.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * Speed) * amplitude;
        transform.localPosition = startLocalPosition + new Vector3(0, offset, 0);
    }
}
