using UnityEngine;

public class PiecesLocation : MonoBehaviour
{
    #region Pieces Data
    [HideInInspector] public int x;
    [HideInInspector] public GameObject piece;
    [HideInInspector] public int y;
    [HideInInspector] public string name;
    [HideInInspector] public char color;

    #endregion

    public PiecesLocation(int x, int y, string name, char color, GameObject piece)
    {
        this.x = x;
        this.y = y;
        this.name = name;
        this.color = color;
        this.piece = piece;
    }

}
