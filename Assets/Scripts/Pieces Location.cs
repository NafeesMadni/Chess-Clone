using System;
using UnityEngine;

public class PiecesLocation : MonoBehaviour
{
    #region Pieces Data
    [HideInInspector] public GameObject piece;
    [HideInInspector] public int x;
    [HideInInspector] public int y;
    [HideInInspector] public int point;
    [HideInInspector] public string name;
    [HideInInspector] public char color;

    #endregion

    public PiecesLocation(int x, int y, string name, char color, GameObject piece)
    {
        this.x = x;
        this.y = y;
        this.name = name;
        this.color = color;
        this.point = GetPoints(name);
        this.piece = piece;
    }

    private int GetPoints(string name)
    {
        switch (name) 
        {
            case "Queen":
                return 9;
            case "Rook":
                return 5;
            case "Bishop":
                return 3;
            case "Knight":
                return 3;
            case "Pawn":
                return 1;

                // King InValudable
            default :
                return 0;

        }
    }
}
