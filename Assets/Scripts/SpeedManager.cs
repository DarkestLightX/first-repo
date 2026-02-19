using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpeedManager : MonoBehaviour
{
    public Transform carTransform;
    public Rigidbody carRb;
    public TMP_Text text;
    public TMP_Text fps;

    // Update is called once per frame
    void LateUpdate()
    {
        float speed = Vector3.Dot(carTransform.right, carRb.linearVelocity);
        speed *= 5;
        speed = Mathf.RoundToInt(Mathf.Abs(speed));
        float current = 0;
        current = Time.frameCount / Time.time;
        float avgFrameRate = (int)current;
        text.SetText("Speed: " + speed.ToString() + " km/h");
        fps.SetText(avgFrameRate + " FPS");
    }
}
