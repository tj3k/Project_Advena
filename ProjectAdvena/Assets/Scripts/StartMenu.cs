using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public AudioManager audioManager;
    
    public Animator transition;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }
    
    public void StartGame()
    {
        StartCoroutine(StartGameCo());
    }

    private IEnumerator StartGameCo()
    {
        // Play UI FadeIn Anim
        // transition.SetTrigger("Start");
        yield return new WaitForSeconds(4f);
        SceneManager.LoadScene("World1_PlayerStage1");
    }

    private IEnumerator QuitGameCo()
    {
        // Play UI FadeIn Anim
        yield return new WaitForSeconds(1.5f);
        Application.Quit();
    }

    public void QuitGame()
    {
        Debug.Log("Bye.");
        StartCoroutine(QuitGameCo());
    }
}
