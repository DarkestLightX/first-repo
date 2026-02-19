using System;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    public static CarManager Instance;
    public static event Action OnSync;

    public CarInfo info;

    private void Awake()
    {
      Instance = this;
    }
    private void Start()
    {
      OnSync?.Invoke();
    }
    public static void SyncTires() 
    {
      OnSync?.Invoke();
    }

}
