using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public AudioManager audioManager;
    
    public void StartGame()
    {
        StartCoroutine(StartGameCo());
    }

    private IEnumerator StartGameCo()
    {
        // Play UI FadeIn Anim
        yield return new WaitForSeconds(4f);
        SceneManager.LoadScene("_prototypeSceneStage1");
    }

    private IEnumerator QuitGameCo()
    {
        // Play UI FadeIn Anim
        yield return new WaitForSeconds(3f);
        Application.Quit();
    }

    public void QuitGame()
    {
        Debug.Log("Bye.");
        StartCoroutine(QuitGameCo());
    }
}
