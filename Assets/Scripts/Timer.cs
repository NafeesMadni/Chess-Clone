using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Timer : MonoBehaviour
{
    private int player1Time = 180; // 3 minutes in seconds
    private int player2Time = 180; 

    [SerializeField]private Text player1TimeText; 
    [SerializeField]private Text player2TimeText;
    [SerializeField]private GameManager gameManager;
    private bool player1Turn = true; // Start with Player 1's turn
    public bool gameStop = false;
    void Start()
    {
        StartCoroutine(CountdownTimer());
        UpdateTimeDisplay();
        player2TimeText.color = new Color32(75, 75, 75, 255); 
    }

    IEnumerator CountdownTimer()
    {
        while (!gameStop && player1Time > 0 && player2Time > 0)
        {
            if (player1Turn)
                player1Time--;
            
            else
                player2Time--;

            UpdateTimeDisplay();

            if(player1Time == 0)
            {
                gameManager.activateGameObject("Black Won", "by time out");
                gameStop = true;
            }
            else if (player2Time == 0)
            {
                gameManager.activateGameObject("White Won", "by time out");
                gameStop = true;
            }

            yield return new WaitForSeconds(1); 
        }

        
    }

    public void SwitchTurn()
    {
        //swap color
        Color32 tempColor =  player1TimeText.color;
        player1TimeText.color = player2TimeText.color;
        player2TimeText.color = tempColor;
        
        player1Turn = !player1Turn;
    }

    void UpdateTimeDisplay()
    {
        player1TimeText.text = FormatTime(player1Time);
        player2TimeText.text = FormatTime(player2Time);
    }

    string FormatTime(int timeInSeconds)
    {
        int minutes = timeInSeconds / 60;
        int seconds = timeInSeconds % 60;
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

}