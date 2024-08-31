using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject gameObject;
    [SerializeField] private TextMeshProUGUI winnerName;
    [SerializeField] private TextMeshProUGUI winningStyle;

    public void activateGameObject(string name, string style)
    {
        gameObject.SetActive(true);
        StartCoroutine(WaitForASec());
        winnerName.text = name;
        winningStyle.text = style;
    }
    public void Rematch()
    {
        StartCoroutine(WaitForASec());
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    
    // Start is called before the first frame update
    public void exit()
    {
        Application.Quit();
    }

    public void close ()
    {
        gameObject.SetActive(false);
    }
    IEnumerator WaitForASec()
    {
        yield return new WaitForSeconds(5);
    }
}