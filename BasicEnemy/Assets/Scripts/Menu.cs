using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public GameObject menuUI;       //  Panel gameobject to show in the screen either the pause or the lost menu
    public Text menuText;           //  Text of the menu title
    public Text enemyState;         //  Text to show the state of the enemy on screen

    bool gamePause = false;

    void Awake()
    {
        Time.timeScale = 1f;
        enemyState.text = "Patrol"; //  The enemy start patroling
        menuUI.SetActive(false);
    }

    private void Update()
    {
        /*
         * Pause the game
         * */
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (!gamePause)
            {
                menuText.text = "GAME IN PAUSE";
                menuUI.SetActive(true);
                gamePause = true;
                Time.timeScale = 0f;
            }
            else
            {
                menuUI.SetActive(false);
                gamePause = false;
                Time.timeScale = 1f;
            }
            
        }
    }

    public void LostPlayer()
    {
        /*
         * Show the pause menu but this say that the player lost
         * */
        menuText.text = "YOU LOST";
        menuUI.SetActive(true);
        gamePause = true;
        Time.timeScale = 0f;
    }

    public void Again()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // Reset the level
    }

    public void Quit()
    {
        Application.Quit();     // Quit the application when is executed by any operating system
        Debug.Log("Quit");
    }

    public void EnemyState(int state)
    {
        /*
         * Fucntion to update the state of the message in the screen
         * To be able to see what the state of the enemy is
         * 1 = Chasing State
         * 2 = Hearing State
         * 3 = Patrol State
         * */

        switch (state)
        {
            case 1:
                enemyState.text = "Chasing";
                enemyState.color = Color.red;
                break;
            case 2:
                enemyState.text = "Hearing";
                enemyState.color = Color.green;
                break;
            case 3:
                enemyState.text = "Patrol";
                enemyState.color = Color.blue;
                break;
            default:
                enemyState.text = "";
                break;
        }
    }
}
