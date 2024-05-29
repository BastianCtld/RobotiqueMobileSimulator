using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    static bool sceneChangerAlreadyHere = false;
    int sceneIndex = 0;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        if(sceneChangerAlreadyHere)
        {
            Destroy(this.gameObject);
        } else
        {
            sceneChangerAlreadyHere = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))
        {
            sceneIndex = (sceneIndex + 1) % SceneManager.sceneCountInBuildSettings;
            SceneManager.LoadScene(sceneIndex);
        }
    }
}
