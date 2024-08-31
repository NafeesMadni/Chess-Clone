using System.Collections.Generic;
using UnityEngine;

public class ValidMoves : MonoBehaviour
{
    public static List<Vector2Int> KingMoves()
    {
        List<Vector2Int> possibleMoves = new List<Vector2Int>();

        // Moves on the same Row
        possibleMoves.Add(new Vector2Int(0, -1));
        possibleMoves.Add(new Vector2Int(0, 1));

        // Top Row Moves
        possibleMoves.Add(new Vector2Int(1, 0));
        possibleMoves.Add(new Vector2Int(1, -1));
        possibleMoves.Add(new Vector2Int(1, 1));

        // Bottom Row Moves
        possibleMoves.Add(new Vector2Int(-1, 0));
        possibleMoves.Add(new Vector2Int(-1, -1));
        possibleMoves.Add(new Vector2Int(-1, 1));

        return possibleMoves;
    }

    public static List<Vector2Int> QueenMoves()
    {
        List<Vector2Int> possibleMoves = new List<Vector2Int>();
        for (int i = 1; i < 8; i++)
        {
            possibleMoves.Add(new Vector2Int(i, i)); // Top-Right Diagonal => Both increases
            possibleMoves.Add(new Vector2Int(i, -i)); // Top-Left Diagonal => x increases & y decreses
            possibleMoves.Add(new Vector2Int(-i, i)); // Bottom-Right Diagonal => x decreases & y increases
            possibleMoves.Add(new Vector2Int(-i, -i)); // Bottom-Left Diagonal => Both decreses
        }

        // ignore itself

        for (int i = 1; i < 8; i++)
        {

            // Upward & Downward moves on the same Column
            possibleMoves.Add(new Vector2Int(i, 0));
            possibleMoves.Add(new Vector2Int(-i, 0));

            // Left & Right moves on the same Row
            possibleMoves.Add(new Vector2Int(0, i));
            possibleMoves.Add(new Vector2Int(0, -i));
        }
        return possibleMoves;
    }
        
    public static List<Vector2Int> RookMoves()
    {
        List<Vector2Int> possibleMoves = new List<Vector2Int>();

        // ignore itself

        for (int i = 1; i < 8; i++)
            possibleMoves.Add(new Vector2Int(i, 0)); // Upward moves on the same Column

        for (int i = 1; i < 8; i++)
            possibleMoves.Add(new Vector2Int(-i, 0)); // Downward moves on the same Column

        for (int i = 1; i < 8; i++)
            possibleMoves.Add(new Vector2Int(0, -i)); // Left moves on the same Row

        for (int i = 1; i < 8; i++)
            possibleMoves.Add(new Vector2Int(0, i)); // Right moves on the same Row

        return possibleMoves;
    }

    public static List<Vector2Int> FirstRowPawnMoves()
    {
        List<Vector2Int> possibleMoves = new List<Vector2Int>();

        // do vector addition to calculate all the possible moves

        possibleMoves.Add(new Vector2Int(1, 0));
        possibleMoves.Add(new Vector2Int(2, 0));

        // This is valid if there is an opponent's piece on the square
        possibleMoves.Add(new Vector2Int(1, -1));
        possibleMoves.Add(new Vector2Int(1, 1));

        /* 
         * En-Passant Moves => on the same Row.
         * Best add this case while playing.
         * Opponent's Pawn Makes a Double-Step Move: The move can only be made immediately after an opponent's pawn moves two squares forward
         * from its starting position, landing beside one of your pawns.
         * possibleMoves.Add(new Vector2Int(0, -1));
         * possibleMoves.Add(new Vector2Int(0, 1));
        */
        return possibleMoves;
    }

    public static List<Vector2Int> SixRowPawnMoves()
    {
        List<Vector2Int> possibleMoves = new List<Vector2Int>();


        possibleMoves.Add(new Vector2Int(-1, 0));
        possibleMoves.Add(new Vector2Int(-2, 0));

        // This is valid if there is an opponent's piece on the square
        possibleMoves.Add(new Vector2Int(-1, -1));
        possibleMoves.Add(new Vector2Int(-1, 1));
        

        return possibleMoves;
    }

    public static List<Vector2Int> KnightMoves()
    {
        List<Vector2Int> possibleMoves = new List<Vector2Int>();

        // Top-Left
        possibleMoves.Add(new Vector2Int(2, -1));
        possibleMoves.Add(new Vector2Int(1, -2));

        // Bottom-Left
        possibleMoves.Add(new Vector2Int(-1, -2));
        possibleMoves.Add(new Vector2Int(-2, -1));

        // Top-Right
        possibleMoves.Add(new Vector2Int(-1, 2));
        possibleMoves.Add(new Vector2Int(-2, 1));

        // Bottom-Right
        possibleMoves.Add(new Vector2Int(2, 1));
        possibleMoves.Add(new Vector2Int(1, 2));

        return possibleMoves;
    }

    public static List<Vector2Int> BishopMoves()
    {
        List<Vector2Int> possibleMoves = new List<Vector2Int>();

        for (int i = 1; i < 8; i++)
        {
            possibleMoves.Add(new Vector2Int(i, i)); // Top-Right Diagonal => Both increases
            possibleMoves.Add(new Vector2Int(i, -i)); // Top-Left Diagonal => x increases & y decreses
            possibleMoves.Add(new Vector2Int(-i, i)); // Bottom-Right Diagonal => x decreases & y increases
            possibleMoves.Add(new Vector2Int(-i, -i)); // Bottom-Left Diagonal => Both decreses
        }

        return possibleMoves;
    }

}
