using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameplayManager : MonoBehaviour
{
    public GameObject player {get; private set;}
    public ProceduralRoad road {get; private set;}
    public static GameplayManager Instance;
    private AsyncOperation asyncLoad;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        StartCoroutine(LoadSceneAsync("MainLevel"));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        if (asyncLoad.isDone)
        {
            player = GameObject.FindWithTag("Player");
            road = ProceduralRoad.Instance;

            while (road.neededPoints < road.roadPoints.Count - 3 || road.pointNormals.Count != road.roadPoints.Count)
            {
                yield return null;
            }

            player.transform.position = road.roadPoints[road.segmentsBehind];
            player.transform.rotation = Quaternion.FromToRotation(Vector3.right, road.pointNormals[road.segmentsBehind]);
        }
    }
}
