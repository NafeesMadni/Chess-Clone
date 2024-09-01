using System;
using System.Collections.Generic;
using UnityEngine;
public class placePieces : MonoBehaviour
{
    #region Variables
    [Header("Accessing Other Classes")]

    [SerializeField] private ChessBoard chessBoard;
    [SerializeField] private Timer timer;
    [SerializeField] private GameManager gameManager;

    [Header("Audio")]

    [SerializeField] private AudioClip check;
    [SerializeField] private AudioClip move;
    [SerializeField] private AudioClip castle;
    [SerializeField] private AudioClip capture;
    [SerializeField] private AudioClip promotion;
    [SerializeField] private AudioSource audioSource;

    [Space]
    
    [Header("Pieces Prefabs")]

    [SerializeField] private GameObject W_Pawn;
    [SerializeField] private GameObject B_Pawn;
    [SerializeField] private GameObject W_King;
    [SerializeField] private GameObject B_King;
    [SerializeField] private GameObject W_Queen;
    [SerializeField] private GameObject B_Queen;
    [SerializeField] private GameObject W_Rook;
    [SerializeField] private GameObject B_Rook;
    [SerializeField] private GameObject W_Bishop;
    [SerializeField] private GameObject B_Bishop;
    [SerializeField] private GameObject W_Knight;
    [SerializeField] private GameObject B_Knight;
    [SerializeField] private GameObject Dot_Prefab;
    
    [Space]
    
    [Header("Tiles Color")]
    
    [SerializeField] private Material clickedTile;
    [SerializeField] private Material checkedTile;

    private char playerTurn; // first white turn
    
    private List<GameObject> highlightedMoves; // array of Dot_prefab
    private Vector2Int prevClickedPosition;
    private Material prevTileColor;
    private Material prevCheckedTile;

    private Dictionary<Vector2Int, PiecesLocation> piecesData;
    private Dictionary<Vector2Int, List<Vector2Int>> availableMoves;

    private Vector2Int w_kingPos;
    private Vector2Int b_kingPos;
    private Vector2Int enPassantPiece;
    private List<Vector2Int> enPassantMoves;

    // block location for both kings 
    private List<Vector2Int> b_KingBlockSq;
    private List<Vector2Int> w_KingBlockSq;

    //Castling Moves
    private Dictionary<Vector2Int, List<Vector2Int>> castleMove;

    bool kingIsUnderAttack;
    float delaySeconds;
    int movesLeft;

    #endregion

    void Awake()
    {
        castleMove = new Dictionary<Vector2Int, List<Vector2Int>>();
        piecesData = new Dictionary<Vector2Int, PiecesLocation>();
        availableMoves = new Dictionary<Vector2Int, List<Vector2Int>>();
        highlightedMoves = new List<GameObject>();
        enPassantMoves = new List<Vector2Int>();
        b_KingBlockSq = new List<Vector2Int>();
        w_KingBlockSq = new List<Vector2Int>();
        prevClickedPosition = -Vector2Int.one;
        enPassantPiece = -Vector2Int.one;
        prevTileColor = null;
        prevCheckedTile = null;
        kingIsUnderAttack = false;
        delaySeconds = 0.5f;
        playerTurn = 'W';
        movesLeft = -1;

        List<Vector2Int> whiteRook = new List<Vector2Int> 
        { 
            new Vector2Int(0, 0), 
            new Vector2Int(0, 7)
        };
        castleMove[new Vector2Int(0, 4)] = whiteRook;

        List<Vector2Int> blackRook = new List<Vector2Int>
        {
            new Vector2Int(7, 0), 
            new Vector2Int(7, 7)
        };
        castleMove[new Vector2Int(7, 4)] = blackRook;

        _placePieces();

        calculateAvailableMoves();
    }

    void Update()
    {
        // Draw by stalemate
        if(!kingIsUnderAttack && movesLeft == 0)
        {
            timer.gameStop = true;
            availableMoves.Clear();
            gameManager.activateGameObject("Draw", "by stalemate");
            return;
        }

        // Draw by insufficient material
        if (!kingIsUnderAttack && piecesData.Count == 2)
        {
            timer.gameStop = true;
            availableMoves.Clear();
            gameManager.activateGameObject("Draw", "by insufficient material");
            return;
        }

        // won by check mate
        if(kingIsUnderAttack && movesLeft == 0)
        {
            timer.gameStop = true;
            string winner = playerTurn == 'W' ? "Black Won" : "White Won";
            availableMoves.Clear();
            gameManager.activateGameObject(winner, "by checkmate");
            return;
        }

        // to change Tile color
        if (kingIsUnderAttack)
        {
            Vector2Int attackedKingTileIndex = playerTurn == 'W' ? w_kingPos : b_kingPos;
            delaySeconds -= Time.deltaTime;
            if(delaySeconds <= 0)
            {
                chessBoard.board[attackedKingTileIndex.x, attackedKingTileIndex.y].GetComponent<MeshRenderer>().material = prevCheckedTile;
                prevCheckedTile = null;
                kingIsUnderAttack = false;
            }
        }

        RaycastHit hitInfo;
        Ray ray = chessBoard.curCamera.ScreenPointToRay(Input.mousePosition);

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out hitInfo, 100))
            {
                Vector2Int position = chessBoard.GetTileIndex(hitInfo.collider.gameObject);

                // if playerTurn color not equal to the clicked position color then return.
                
                if (prevClickedPosition != -Vector2Int.one && piecesData.ContainsKey(position)) {
                    if(playerTurn != piecesData[position].color)
                    {
                        bool isValidMove = false;
                        for (int i = 0; i < availableMoves[prevClickedPosition].Count; i++)
                        {
                            if (piecesData.ContainsKey(availableMoves[prevClickedPosition][i]))
                            {
                                if (availableMoves[prevClickedPosition][i] == position) isValidMove = true;
                            }
                        }
                        if (!isValidMove) {

                            chessBoard.board[prevClickedPosition.x, prevClickedPosition.y].GetComponent<MeshRenderer>().material = prevTileColor;
                            deleteCircles();
                            prevClickedPosition = -Vector2Int.one;
                            return;
                        }
                    }
                }

               
                if (prevClickedPosition == -Vector2Int.one && piecesData.ContainsKey(position))
                {
                    if (playerTurn != piecesData[position].color) return;
                }


                if (isValidMove(position, prevClickedPosition))
                {
                    audioSource.clip = move; // first maybe just a simple move

                    // Castling
                    if(castleMove.ContainsKey(prevClickedPosition) && position.x == prevClickedPosition.x && piecesData[prevClickedPosition].name == "King")
                    {
                        // left side castling
                        if(prevClickedPosition.y - 2 == position.y)
                        {
                            audioSource.clip = castle;
                            Vector2Int rookPrevPos = new Vector2Int(prevClickedPosition.x, 0);
                            Vector2Int rookNewPos = new Vector2Int(prevClickedPosition.x, prevClickedPosition.y - 1);

                            piecesData[rookNewPos] = piecesData[rookPrevPos];

                            foreach (Transform child in chessBoard.board[rookPrevPos.x, rookPrevPos.y].transform)
                                Destroy(child.gameObject);

                            _place(rookNewPos.x, rookNewPos.y, piecesData[rookPrevPos].name, piecesData[rookPrevPos].color, piecesData[rookPrevPos].piece);

                            piecesData.Remove(rookPrevPos);

                            availableMoves.Remove(rookPrevPos);

                            castleMove.Remove(prevClickedPosition);
                        }
                        // right side Castle
                        else if(prevClickedPosition.y + 2 == position.y)
                        {
                            audioSource.clip = castle;
                            Vector2Int rookPrevPos = new Vector2Int(prevClickedPosition.x, 7);
                            Vector2Int rookNewPos = new Vector2Int(prevClickedPosition.x, prevClickedPosition.y + 1);

                            piecesData[rookNewPos] = piecesData[rookPrevPos];

                            foreach (Transform child in chessBoard.board[rookPrevPos.x, rookPrevPos.y].transform)
                                Destroy(child.gameObject);

                            _place(rookNewPos.x, rookNewPos.y, piecesData[rookPrevPos].name, piecesData[rookPrevPos].color, piecesData[rookPrevPos].piece);

                            piecesData.Remove(rookPrevPos);

                            availableMoves.Remove(rookPrevPos);

                            // Now, Remove the king Castling Ability
                            castleMove.Remove(prevClickedPosition);
                        }
                    }
                    
                    if (piecesData.ContainsKey(position))
                    {
                        if (piecesData[position].color != piecesData[prevClickedPosition].color)
                        {
                            audioSource.clip = capture;
                            foreach (Transform child in chessBoard.board[position.x, position.y].transform)
                                Destroy(child.gameObject);
                            piecesData.Remove(position);
                            availableMoves.Remove(position);
                        }   
                    }

                    piecesData[position] = piecesData[prevClickedPosition]; 
                    
                    playerTurn = (playerTurn == 'B') ? 'W' : 'B';
                    
                    timer.SwitchTurn();

                    // enPassant 
                    if (enPassantPiece != -Vector2Int.one && piecesData[position].name == "Pawn" && enPassantMoves.Contains(prevClickedPosition) && position.y != prevClickedPosition.y)
                    {
                        audioSource.clip = capture;

                        foreach (Transform child in chessBoard.board[enPassantPiece.x, enPassantPiece.y].transform)
                            Destroy(child.gameObject);

                        piecesData.Remove(enPassantPiece);
                        availableMoves.Remove(enPassantPiece);
                        enPassantPiece = -Vector2Int.one;
                    }
                    
                    else // if enPassant move is not made instantly then reset it 
                    {
                        enPassantPiece = -Vector2Int.one;
                        enPassantMoves.Clear();
                    }

                    foreach (Transform child in chessBoard.board[prevClickedPosition.x, prevClickedPosition.y].transform)
                        Destroy(child.gameObject);

                    chessBoard.board[prevClickedPosition.x, prevClickedPosition.y].GetComponent<MeshRenderer>().material = prevTileColor;
                    deleteCircles();
                    
                    _place(position.x, position.y, piecesData[prevClickedPosition].name, piecesData[prevClickedPosition].color, piecesData[prevClickedPosition].piece);

                    // Promotion move
                    if (playerTurn != 'W' && piecesData[position].name == "Pawn" && piecesData[position].x == 7)
                        piecePromotion(position, W_Queen, 'W');
                    
                    else if (playerTurn != 'B' && piecesData[position].name == "Pawn" && piecesData[position].x == 0)
                        piecePromotion(position, B_Queen, 'B');
                    

                    piecesData.Remove(prevClickedPosition);

                    calculateAvailableMoves();

                    piecesProtectingKing();

                    calculateEnPassant(position);
                    
                    prevClickedPosition = -Vector2Int.one;
                    
                    checkIsKingAttacked();
                    
                    calculateCastlingMoves();

                    audioSource.Play();
                }

                else if (prevClickedPosition != -Vector2Int.one && !piecesData.ContainsKey(position) || prevClickedPosition == position)
                {
                    deleteCircles();
                    chessBoard.board[prevClickedPosition.x, prevClickedPosition.y].GetComponent<MeshRenderer>().material = prevTileColor;
                    prevClickedPosition = -Vector2Int.one;
                }

                else if (piecesData.ContainsKey(position))
                {
                    if (prevClickedPosition != -Vector2Int.one)
                    {
                        deleteCircles();
                        chessBoard.board[prevClickedPosition.x, prevClickedPosition.y].GetComponent<MeshRenderer>().material = prevTileColor;
                    }

                    drawDots(position);
                    prevTileColor = chessBoard.board[position.x, position.y].GetComponent<MeshRenderer>().material;
                    chessBoard.board[position.x, position.y].GetComponent<MeshRenderer>().material = clickedTile;
                    prevClickedPosition = position;
                }

            }
        }
    }

    private bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < 8 && y >= 0 && y < 8;
    }

    private bool isValidMove(Vector2Int movePos, Vector2Int piecePos)
    {
        if (piecePos == -Vector2Int.one || piecePos == movePos) return false;

        for (int i = 0; i < availableMoves[piecePos].Count; i++)
        {
            if (availableMoves[piecePos][i] == movePos)
                return true;
        }

        return false;
    }

    #region Calculating Castling Move

    private void calculateCastlingMoves()
    {
        Vector2Int king = playerTurn == 'W' ? new Vector2Int(0, 4) : new Vector2Int(7, 4);

        // king made move
        if (!castleMove.ContainsKey(king)) return;

        //if both rooks moved
        if (castleMove[king].Count == 0)
        {
            castleMove.Remove(king);
            return;
        }

        // checking whether Rooks are there or not
        bool leftRook = false;
        bool rightRook = false;

        foreach (Vector2Int item in castleMove[king])
        {
            if (item.y < king.y) leftRook = true; 
            else if(item.y > king.y) rightRook = true; 
        }

        if(leftRook)
        {
            for (int y = king.y - 1; y > 0; y--) // king and rook excluded
            {
                // first check whether the fellow or opponent pieces blocking the king from castling or not
                if (piecesData.ContainsKey(new Vector2Int(king.x, y)))
                {
                    // random piece block
                    leftRook = false;
                    break;
                }
            }
            // if no random piece found then check opponent pieces attacks
            if(leftRook)
            {
                foreach(Vector2Int oppoPiece in piecesData.Keys)
                {
                    if (piecesData[oppoPiece].color != playerTurn)
                    {
                        // comparing opponent pieces moves 
                        if (availableMoves[oppoPiece].Contains(new Vector2Int(king.x, king.y - 1)) || availableMoves[oppoPiece].Contains(new Vector2Int(king.x, king.y - 2)))
                        {
                            leftRook = false;
                            break;
                        }
                    }
                }
                if(leftRook)
                    // Add the extra move to the king on the left
                    availableMoves[king].Add(new Vector2Int(king.x, king.y - 2));
               
            } 
        }
        if (rightRook)
        {
            for (int y = king.y + 1; y < 7; y++) 
            {
                if (piecesData.ContainsKey(new Vector2Int(king.x, y)))
                {
                    rightRook = false;
                    break;
                }
            }
            
            if (rightRook)
            {
                foreach (Vector2Int oppoPiece in piecesData.Keys)
                {
                    if (piecesData[oppoPiece].color != playerTurn)
                    {
                        if (availableMoves[oppoPiece].Contains(new Vector2Int(king.x, king.y + 1)) || availableMoves[oppoPiece].Contains(new Vector2Int(king.x, king.y + 2)))
                        {
                            rightRook = false;
                            break;
                        }
                    }
                }
                if (rightRook)
                    availableMoves[king].Add(new Vector2Int(king.x, king.y + 2));
                
            }
        }
    }

    #endregion

    #region King Protection
    private void piecesProtectingKing()
    {
        char color = playerTurn == 'W' ? 'W' : 'B';
        foreach (Vector2Int attackerPos in piecesData.Keys)
        {
            string attackerName = piecesData[attackerPos].name;

            if (piecesData[attackerPos].color != playerTurn && (attackerName == "Rook" || attackerName == "Queen" || attackerName == "Bishop"))  // checking opponent pieces attacks
            {
                foreach (Vector2Int kingGuardPiece in piecesData.Keys) // kingGuardPos will be the location of the piece that protect the King
                {
                    if (piecesData[kingGuardPiece].color == playerTurn)
                    {
                        if (availableMoves[attackerPos].Contains(kingGuardPiece) && piecesData[kingGuardPiece].name != "King")
                        {
                            if (attackerPos.x == kingGuardPiece.x)
                                horizontallyKingProtection(attackerPos, kingGuardPiece);

                            else if (attackerPos.y == kingGuardPiece.y)
                                verticallyKingProtection(attackerPos, kingGuardPiece);

                            else if (isDiagonal(kingGuardPiece, attackerPos))
                                diagonallyKingProtection(attackerPos, kingGuardPiece);

                        }
                    }
                }
            }
        }
    }

    private void horizontallyKingProtection(Vector2Int attackerPos, Vector2Int kingGuardPiece)
    {
        bool isKingBehind = false;

        if (attackerPos.y < kingGuardPiece.y) // attacker on the left 
        {
            for (int y = kingGuardPiece.y + 1; y < 8; y++)
            {
                Vector2Int myNxtKey = new Vector2Int(kingGuardPiece.x, y);

                if (piecesData.ContainsKey(myNxtKey))
                {
                    if (piecesData[myNxtKey].name != "King") break;

                    if (piecesData[myNxtKey].color == playerTurn && piecesData[myNxtKey].name == "King")
                    {
                        isKingBehind = true;
                        break;
                    }
                }
            }
        }

        else if (kingGuardPiece.y < attackerPos.y) // attacker on the right
        {
            for (int y = kingGuardPiece.y - 1; y >= 0; y--)
            {
                Vector2Int myNxtKey = new Vector2Int(kingGuardPiece.x, y);
                if (piecesData.ContainsKey(myNxtKey))
                {
                    if (piecesData[myNxtKey].name != "King") break;

                    if (piecesData[myNxtKey].color == playerTurn && piecesData[myNxtKey].name == "King")
                    {
                        isKingBehind = true;
                        break;
                    }
                }
            }
        }

        if (isKingBehind)
        {
            for (int i = 0; i < availableMoves[kingGuardPiece].Count; i++)
            {
                // Removing Invalid Moves
                if (availableMoves[kingGuardPiece][i].x != attackerPos.x)
                    availableMoves[kingGuardPiece].Remove(availableMoves[kingGuardPiece][i--]);
            }
        }
    }

    private void verticallyKingProtection(Vector2Int attackerPos, Vector2Int kingGuardPiece)
    {
        bool isKingBehind = false;

        if (attackerPos.x < kingGuardPiece.x) // attacker at the bottom 
        {
            for (int x = kingGuardPiece.x + 1; x < 8; x++)
            {
                Vector2Int myNxtKey = new Vector2Int(x, kingGuardPiece.y);
                if (piecesData.ContainsKey(myNxtKey))
                {
                    if (piecesData[myNxtKey].name != "King") break;

                    if (piecesData[myNxtKey].color == playerTurn && piecesData[myNxtKey].name == "King")
                    {
                        isKingBehind = true;
                        break;
                    }
                }
            }
        }

        else if (kingGuardPiece.x < attackerPos.x) // attacker at the top
        {
            for (int x = kingGuardPiece.x - 1; x >= 0; x--)
            {
                Vector2Int myNxtKey = new Vector2Int(x, kingGuardPiece.y);
                if (piecesData.ContainsKey(myNxtKey))
                {
                    if (piecesData[myNxtKey].name != "King") break;

                    if (piecesData[myNxtKey].color == playerTurn && piecesData[myNxtKey].name == "King")
                    {
                        isKingBehind = true;
                        break;
                    }
                }
            }
        }

        if (isKingBehind)
        {
            for (int i = 0; i < availableMoves[kingGuardPiece].Count; i++)
            {
                // Removing Invalid Moves
                if (availableMoves[kingGuardPiece][i].y != attackerPos.y)
                    availableMoves[kingGuardPiece].Remove(availableMoves[kingGuardPiece][i--]);
            }
            isKingBehind = false;
        }
    }

    private void diagonallyKingProtection(Vector2Int attackerPos, Vector2Int kingGuardPiece)
    {
        bool isKingBehind = false;

        // Top-Right Diagonal -> Attacker <= Protector < King (king top)
        if (greater(kingGuardPiece, attackerPos))
        {
            Vector2Int tempKing = new Vector2Int();
            for (int i = 1; i < 8; i++)
            {
                Vector2Int myNxtKey = new Vector2Int(kingGuardPiece.x + i, kingGuardPiece.y + i);

                if (myNxtKey.x > 7 || myNxtKey.y > 7) break;

                if (piecesData.ContainsKey(myNxtKey))
                {
                    if (piecesData[myNxtKey].name != "King" || piecesData[myNxtKey].color != playerTurn)
                        break;

                    if (piecesData[myNxtKey].name == "King")
                    {
                        tempKing = myNxtKey;
                        isKingBehind = true;
                        break;
                    }

                }
            }

            if (isKingBehind)
            {
                for (int i = 0; i < availableMoves[kingGuardPiece].Count; i++)
                {
                    if (greater(availableMoves[kingGuardPiece][i], attackerPos) && isDiagonal(availableMoves[kingGuardPiece][i], attackerPos) || less(tempKing, availableMoves[kingGuardPiece][i]) && isDiagonal(tempKing, availableMoves[kingGuardPiece][i]))
                        continue;
                    if (availableMoves[kingGuardPiece][i] != attackerPos)
                        availableMoves[kingGuardPiece].Remove(availableMoves[kingGuardPiece][i--]);
                }
            }
        }

        // Bottom-Left Diagonal -> King < Protector <= Attacker (attacker top)
        else if (less(kingGuardPiece, attackerPos))
        {
            Vector2Int tempKing = new Vector2Int();
            for (int i = 1; i < 8; i++)
            {
                Vector2Int myNxtKey = new Vector2Int(kingGuardPiece.x - i, kingGuardPiece.y - i);

                if (myNxtKey.x < 0 || myNxtKey.y < 0) break;

                if (piecesData.ContainsKey(myNxtKey))
                {
                    if (piecesData[myNxtKey].name != "King" || piecesData[myNxtKey].color != playerTurn)
                        break;
                    if (piecesData[myNxtKey].name == "King")
                    {
                        tempKing = myNxtKey;
                        isKingBehind = true;
                        break;
                    }
                }
            }
            if (isKingBehind)
            {
                for (int i = 0; i < availableMoves[kingGuardPiece].Count; i++)
                {
                    if (greater(attackerPos, availableMoves[kingGuardPiece][i]) && isDiagonal(attackerPos, availableMoves[kingGuardPiece][i]) || less(availableMoves[kingGuardPiece][i], tempKing) && isDiagonal(availableMoves[kingGuardPiece][i], tempKing))
                        continue;
                    if (availableMoves[kingGuardPiece][i] != attackerPos)
                        availableMoves[kingGuardPiece].Remove(availableMoves[kingGuardPiece][i--]);
                }
            }
        }

        // Top-Left Diagonal -> King < Protector <= Attacker (attacker Top)
        else if (kingGuardPiece.x > attackerPos.x && kingGuardPiece.y < attackerPos.y)
        {
            Vector2Int tempKing = new Vector2Int();
            for (int i = 1; i < 8; i++)
            {
                Vector2Int myNxtKey = new Vector2Int(kingGuardPiece.x + i, kingGuardPiece.y - i);

                if (myNxtKey.x > 7 || myNxtKey.y < 0) break;

                if (piecesData.ContainsKey(myNxtKey))
                {
                    if (piecesData[myNxtKey].name != "King" || piecesData[myNxtKey].color != playerTurn)
                        break;

                    if (piecesData[myNxtKey].name == "King")
                    {
                        isKingBehind = true;
                        tempKing = myNxtKey;
                        break;
                    }
                }
            }
            if (isKingBehind)
            {
                for (int i = 0; i < availableMoves[kingGuardPiece].Count; i++)
                {
                    if ((availableMoves[kingGuardPiece][i].x < tempKing.x && availableMoves[kingGuardPiece][i].y > tempKing.y) && isDiagonal(availableMoves[kingGuardPiece][i], tempKing) || (availableMoves[kingGuardPiece][i].x >= attackerPos.x && availableMoves[kingGuardPiece][i].y <= attackerPos.y) && isDiagonal(availableMoves[kingGuardPiece][i], attackerPos))
                        continue;
                    availableMoves[kingGuardPiece].Remove(availableMoves[kingGuardPiece][i--]);
                }
            }
        }

        // Bottom-Right Diagonal -> Attacker <= Protector < King (king top)
        else if (kingGuardPiece.x < attackerPos.x && kingGuardPiece.y > attackerPos.y)
        {
            Vector2Int tempKing = new Vector2Int();
            for (int i = 1; i < 8; i++)
            {
                Vector2Int myNxtKey = new Vector2Int(kingGuardPiece.x - i, kingGuardPiece.y + i);

                if (myNxtKey.x < 0 || myNxtKey.y > 7) break;

                if (piecesData.ContainsKey(myNxtKey))
                {
                    if (piecesData[myNxtKey].name != "King" || piecesData[myNxtKey].color != playerTurn)
                        break;

                    if (piecesData[myNxtKey].name == "King")
                    {
                        isKingBehind = true;
                        tempKing = myNxtKey;
                        break;
                    }
                }
            }
            if (isKingBehind)
            {
                for (int i = 0; i < availableMoves[kingGuardPiece].Count; i++)
                {
                    if ((availableMoves[kingGuardPiece][i].x > tempKing.x && availableMoves[kingGuardPiece][i].y < tempKing.y) && isDiagonal(availableMoves[kingGuardPiece][i], tempKing) || (availableMoves[kingGuardPiece][i].x <= attackerPos.x && availableMoves[kingGuardPiece][i].y >= attackerPos.y) && isDiagonal(availableMoves[kingGuardPiece][i], attackerPos))
                        continue;
                    availableMoves[kingGuardPiece].Remove(availableMoves[kingGuardPiece][i--]);
                }
            }
        }

    }

    #endregion

    #region Piece Promotion

    private void piecePromotion(Vector2Int curPos, GameObject queen, char color)
    {
        audioSource.clip = promotion;
        foreach (Transform child in chessBoard.board[curPos.x, curPos.y].transform)
            Destroy(child.gameObject);

        piecesData.Remove(curPos);
        availableMoves.Remove(curPos);

        _place(curPos.x, curPos.y, "Queen", color, queen);
    }

    #endregion

    #region EnPassant Calculation

    private void calculateEnPassant(Vector2Int curPos)
    {
        enPassantMoves.Clear();
        if (piecesData[curPos].name == "Pawn")
        {
            // enPassant move for White -> if Black moves 2 sq down
            if (playerTurn == 'W' && prevClickedPosition.x - 2 == curPos.x)
            {
                // Black made an enPassant Possible for White
                if (curPos.y > 0)
                    whiteEnPassant(new Vector2Int(curPos.x, curPos.y - 1), curPos); // enPassant for the left attacker 
                
                if (curPos.y < 7)
                    whiteEnPassant(new Vector2Int(curPos.x, curPos.y + 1), curPos); // enPassant for the right attacker
                
            }

            // enPassant move for Black -> White moves 2 sq up
            else if (playerTurn == 'B' && prevClickedPosition.x + 2 == curPos.x)
            {
                if (curPos.y > 0)
                    blackEnPassant(new Vector2Int(curPos.x, curPos.y - 1), curPos); 
                
                if (curPos.y < 7)
                    blackEnPassant(new Vector2Int(curPos.x, curPos.y + 1), curPos);
                
            }
        }
    }

    private void whiteEnPassant(Vector2Int move, Vector2Int curPos)
    {
        if (piecesData.ContainsKey(move)) // checking whether opponent piece is present on the left or right
        {
            if (piecesData[move].name == "Pawn" && piecesData[move].color == playerTurn)
            {
                enPassantPiece = curPos;
                availableMoves[move].Add(new Vector2Int(curPos.x + 1, curPos.y)); // EnPassant Move
                enPassantMoves.Add(move);
            }
        }
    }

    private void blackEnPassant(Vector2Int move, Vector2Int curPos)
    {
        if (piecesData.ContainsKey(move)) // checking whether opponent piece is present on the left or right
        {
            if (piecesData[move].name == "Pawn" && piecesData[move].color == playerTurn)
            {
                enPassantPiece = curPos;
                availableMoves[move].Add(new Vector2Int(curPos.x - 1, curPos.y)); // EnPassant Move
                enPassantMoves.Add(move);
            }
        }
    }

    #endregion

    #region Check Threats
    void checkIsKingAttacked()
    {
        Vector2Int kingPos = (playerTurn == 'W') ? w_kingPos : b_kingPos;
        List<Vector2Int> attackerPos = new List<Vector2Int>();
        

        // get the attacker position
        foreach(Vector2Int key in piecesData.Keys)
        {
            if (piecesData[key].color != playerTurn)
            {
                for (int i = 0; i < availableMoves[key].Count; i++)
                {
                    if (availableMoves[key][i] == kingPos)
                    { 
                        audioSource.clip = check;
                        kingIsUnderAttack = true;
                        attackerPos.Add(key);  
                    }
                }
            }
        }
        
        if (kingIsUnderAttack)
        {
            delaySeconds = 0.75f;
            if(chessBoard.board[kingPos.x, kingPos.y].GetComponent<MeshRenderer>().material != checkedTile)
                prevCheckedTile = chessBoard.board[kingPos.x, kingPos.y].GetComponent<MeshRenderer>().material;
            chessBoard.board[kingPos.x, kingPos.y].GetComponent<MeshRenderer>().material = checkedTile;

            #region Discovery + Direct Attack 

            if (attackerPos.Count > 1)
            {

                // if discovery + direct attack then remove all the available moves except king moves (escaping Moves)
                foreach (Vector2Int key in piecesData.Keys)
                {
                    if (piecesData[key].color == playerTurn)
                    {
                        for (int i = 0; i < availableMoves[key].Count; i++)
                        {
                            if (piecesData[key].name != "King")
                            {
                                availableMoves[key].Remove(availableMoves[key][i--]);
                            }
                        }
                    }
                }

                // Queen comebine of both

                Vector2Int rookAttack = piecesData[attackerPos[0]].name == "Bishop" ? attackerPos[1] : attackerPos[0];

                Vector2Int bishopAttack = piecesData[attackerPos[0]].name == "Bishop" ? attackerPos[0] : attackerPos[1];

                #region Horizontal: Same rRow 

                // attacker on the left of king -> remove the right side moves of the king
                if (rookAttack.y < kingPos.y)
                {
                    for (int i = 0; i < availableMoves[kingPos].Count; i++)
                    {
                        if (availableMoves[kingPos][i].y > kingPos.y && availableMoves[kingPos][i].x == kingPos.x)
                            availableMoves[kingPos].Remove(availableMoves[kingPos][i--]);
                    }
                }

                // attacker on the right side of king -> remove the left side moves of the king
                else if (rookAttack.y > kingPos.y)
                {
                    for (int i = 0; i < availableMoves[kingPos].Count; i++)
                    {
                        if (availableMoves[kingPos][i].y < kingPos.y && availableMoves[kingPos][i].x == kingPos.x)
                            availableMoves[kingPos].Remove(availableMoves[kingPos][i--]);
                    }
                }

                #endregion

                #region Vertical Attakcs: Same Column

                // attacker above the king -> remove the king's bottom moves on the same Column
                else if (rookAttack.x > kingPos.x)
                {
                    for (int i = 0; i < availableMoves[kingPos].Count; i++)
                    {
                        if (availableMoves[kingPos][i].x < kingPos.x && availableMoves[kingPos][i].y == kingPos.y)
                            availableMoves[kingPos].Remove(availableMoves[kingPos][i--]);
                    }
                }

                // attacker below the king -> remove all the king's top moves on the same Column
                else if (rookAttack.x < kingPos.x)
                {
                    for (int i = 0; i < availableMoves[kingPos].Count; i++)
                    {
                        if (availableMoves[kingPos][i].x > kingPos.x && availableMoves[kingPos][i].y == kingPos.y)
                            availableMoves[kingPos].Remove(availableMoves[kingPos][i--]);
                    }
                }

                #endregion

                #region Diagonal Attacks 

                if (isDiagonal(kingPos, bishopAttack))
                {
                    // Top-Right Diagonal -> Attacker below
                    if (greater(kingPos, bishopAttack))
                    {
                        for (int i = 0; i < availableMoves[kingPos].Count; i++)
                        {
                            // Remove the king Unsafe Moves
                            if (isDiagonal(availableMoves[kingPos][i], kingPos) && greater(availableMoves[kingPos][i], kingPos))
                            {
                                availableMoves[kingPos].Remove(availableMoves[kingPos][i--]);
                                break;
                            }
                        }
                    }

                    // Bottom-Left Diagonal -> Attacker up
                    else if (less(kingPos, bishopAttack))
                    {
                        for (int i = 0; i < availableMoves[kingPos].Count; i++)
                        {
                            // Remove the king Unsafe Moves
                            if (isDiagonal(availableMoves[kingPos][i], kingPos) && less(availableMoves[kingPos][i], kingPos))
                            {
                                availableMoves[kingPos].Remove(availableMoves[kingPos][i--]);
                                break;
                            }
                        }
                    }

                    // Top-Left Diagonal -> Attacker below
                    else if (kingPos.x > bishopAttack.x && kingPos.y < bishopAttack.y)
                    {
                        for (int i = 0; i < availableMoves[kingPos].Count; i++)
                        {
                            // Remove the king Unsafe Moves
                            if (isDiagonal(availableMoves[kingPos][i], kingPos) && kingPos.x < availableMoves[kingPos][i].x && kingPos.y > availableMoves[kingPos][i].y)
                            {
                                availableMoves[kingPos].Remove(availableMoves[kingPos][i--]);
                                break;
                            }
                        }
                    }

                    // Bottom-Right Diagonal -> Attacker up
                    else if (kingPos.x < bishopAttack.x && kingPos.y > bishopAttack.y)
                    {
                        for (int i = 0; i < availableMoves[kingPos].Count; i++)
                        {
                            // Remove the king Unsafe Moves
                            if (isDiagonal(availableMoves[kingPos][i], kingPos) && kingPos.x > availableMoves[kingPos][i].x && kingPos.y < availableMoves[kingPos][i].y)
                            {
                                availableMoves[kingPos].Remove(availableMoves[kingPos][i--]);
                                break;
                            }
                        }
                    }

                }
                #endregion

            }
            #endregion

            #region Single Attacker
            // Single Attacker: Moving the king to an unattacked square, interposing a piece between the threatening piece and the king, or capturing the threatening piece.
            if (attackerPos.Count == 1)
            {
                // kill the knight or escape 
                if (piecesData[attackerPos[0]].name == "Knight")
                {
                    foreach (Vector2Int key in piecesData.Keys)
                    {
                        if (piecesData[key].color == playerTurn)
                        {
                            for (int i = 0; i < availableMoves[key].Count; i++)
                            {
                                if (availableMoves[key][i] != attackerPos[0] && piecesData[key].name != "King")
                                {
                                    availableMoves[key].Remove(availableMoves[key][i--]);
                                }
                            }
                        }
                    }
                }


                /*
                 * ? Same Row (Horizontal Attack): If the queen and the king are on the same row (same rank).
                 * Same Column (Vertical Attack): If the queen and the king are on the same column (same file)
                 * Same Diagonal (Diagonal Attack): If the queen and the king are on the same diagonal.
                   Diagonal attacks occur if the difference in their ranks equals the difference in their files 
                   (e.g., |rank_queen - rank_king| == |file_queen - file_king|).
                */

                // Hotizontal Attack
                else if (attackerPos[0].x == kingPos.x)
                {
                    // Attacker is on the left of king
                    if (attackerPos[0].y < kingPos.y)
                    {
                        // first remove all the available moves which are not on the same x-axis to block the attack or capture the attacker
                        foreach (Vector2Int key in piecesData.Keys)
                        {
                            if (piecesData[key].color == playerTurn)
                            {
                                for (int i = 0; i < availableMoves[key].Count; i++)
                                {
                                    // availableMoves[key][i].x != attackerPos[0].x && piecesData[key].name != "King"   => remove all the available which aren't on the attacker row iff the piece is not a king (save king escape moves)

                                    // availableMoves[key][i].y < attackerPos[0].y   => remove all the king's side pieces moves which are < attacker.y (moves on left of the attacker not required)

                                    // availableMoves[key][i].y > kingPos.y && availableMoves[key][i].x == kingPos.x   => remove all the king and other pieces moves which are > king.y (moves on right of the king not required) 

                                    if (availableMoves[key][i].x != attackerPos[0].x && piecesData[key].name != "King" || availableMoves[key][i].y < attackerPos[0].y || (availableMoves[key][i].y > kingPos.y && availableMoves[key][i].x == kingPos.x))
                                        availableMoves[key].Remove(availableMoves[key][i--]);

                                }
                            }
                        }
                    }

                    // Attacker is on the right of king
                    else if (attackerPos[0].y > kingPos.y)
                    {
                        foreach (Vector2Int key in piecesData.Keys)
                        {
                            if (piecesData[key].color == playerTurn)
                            {
                                for (int i = 0; i < availableMoves[key].Count; i++)
                                {
                                    if (availableMoves[key][i].x != attackerPos[0].x && piecesData[key].name != "King" || availableMoves[key][i].y > attackerPos[0].y || (availableMoves[key][i].y < kingPos.y && availableMoves[key][i].x == kingPos.x))
                                        availableMoves[key].Remove(availableMoves[key][i--]);
                                }
                            }
                        }
                    }

                }

                // Vertical Attack
                else if (attackerPos[0].y == kingPos.y)
                {
                    // attacker present below the king
                    if (attackerPos[0].x < kingPos.x)
                    {
                        // remove all the available moves which are not on the same row to block the attack or capture the attacker
                        foreach (Vector2Int key in piecesData.Keys)
                        {
                            if (piecesData[key].color == playerTurn)
                            {
                                for (int i = 0; i < availableMoves[key].Count; i++)
                                {
                                    //1. remove all the possible moves below the attacker
                                    //2. remove all the king's or it's fellow pieces moves which is on top of the king
                                    //3. remove all other pieces moves which aren't on the same column iff the piece is not a king
                                    if (availableMoves[key][i].x < attackerPos[0].x || availableMoves[key][i].x > kingPos.x && availableMoves[key][i].y == kingPos.y || availableMoves[key][i].y != kingPos.y && piecesData[key].name != "King")
                                        availableMoves[key].Remove(availableMoves[key][i--]);
                                }
                            }
                        }
                    }
                    // attacker above the king
                    else if (attackerPos[0].x > kingPos.x)
                    {
                        foreach (Vector2Int key in piecesData.Keys)
                        {
                            if (piecesData[key].color == playerTurn)
                            {
                                for (int i = 0; i < availableMoves[key].Count; i++)
                                {
                                    //1. remove all the possible moves above the attacker
                                    //2. remove all the king's or it's fellow pieces moves which are present below the king but lies on the same column
                                    //3. remove all other pieces moves which aren't on the same column iff the piece is not a king
                                    if (availableMoves[key][i].x > attackerPos[0].x || availableMoves[key][i].x < kingPos.x && availableMoves[key][i].y == kingPos.y || availableMoves[key][i].y != kingPos.y && piecesData[key].name != "King")
                                        availableMoves[key].Remove(availableMoves[key][i--]);
                                }
                            }
                        }
                    }
                }

                // Diagonal Attack
                else if (isDiagonal(attackerPos[0], kingPos))
                {
                    // Top-Right Diagonal Attack: Queen (3, 2) King (5, 4)
                    if (greater(kingPos, attackerPos[0]))
                    {
                        foreach (Vector2Int key in piecesData.Keys)
                        {
                            if (piecesData[key].color == playerTurn)
                            {
                                for (int i = 0; i < availableMoves[key].Count; i++)
                                {
                                    // Remove the king Unsafe Moves
                                    if (piecesData[key].name == "King" && isDiagonal(availableMoves[key][i], kingPos) && greater(availableMoves[key][i], kingPos))
                                        availableMoves[key].Remove(availableMoves[key][i--]);

                                    // Valid Moves: Attacker <= king's protection Pieces(Attack Blocker or Attacker capturer) < King
                                    // Remove all the other invalid Moves
                                    else if (!(isDiagonal(availableMoves[key][i], kingPos) && (attackerPos[0].x <= availableMoves[key][i].x && attackerPos[0].y <= availableMoves[key][i].y) && less(availableMoves[key][i], kingPos)))
                                    {
                                        if (piecesData[key].name == "King")
                                            continue;
                                        availableMoves[key].Remove(availableMoves[key][i--]);
                                    }
                                }
                            }
                        }
                    }

                    //Bottom-Left Diagonal Attack
                    else if (less(kingPos, attackerPos[0]))
                    {
                        foreach (Vector2Int key in piecesData.Keys)
                        {
                            if (piecesData[key].color == playerTurn)
                            {
                                for (int i = 0; i < availableMoves[key].Count; i++)
                                {
                                    // Remove the king Unsafe Moves
                                    if (piecesData[key].name == "King" && isDiagonal(availableMoves[key][i], kingPos) && less(availableMoves[key][i], kingPos))
                                        availableMoves[key].Remove(availableMoves[key][i--]);

                                    // Valid Moves: king < king's protection Pieces(Attack Blocker or Attacker capturer) <= Attacker
                                    // Remove all the other invalid Moves
                                    else if (!(isDiagonal(availableMoves[key][i], kingPos) && (availableMoves[key][i].x <= attackerPos[0].x && availableMoves[key][i].y <= attackerPos[0].y) && greater(availableMoves[key][i], kingPos)))
                                    {
                                        if (piecesData[key].name == "King")
                                            continue;
                                        availableMoves[key].Remove(availableMoves[key][i--]);
                                    }
                                }
                            }
                        }
                    }

                    // Top-Left Diagonal Attack
                    else if (kingPos.x > attackerPos[0].x && kingPos.y < attackerPos[0].y)
                    {
                        // Valid Moves: king < king's protection Pieces(Attack Blocker or Attacker capturer) <= Attacker
                        foreach (Vector2Int key in piecesData.Keys)
                        {
                            if (piecesData[key].color == playerTurn)
                            {
                                for (int i = 0; i < availableMoves[key].Count; i++)
                                {
                                    // Remove the king Unsafe Moves
                                    if (piecesData[key].name == "King" && isDiagonal(availableMoves[key][i], kingPos) && (kingPos.x < availableMoves[key][i].x && kingPos.y > availableMoves[key][i].y))
                                        availableMoves[key].Remove(availableMoves[key][i--]);

                                    // Remove all the other invalid Moves
                                    //valid:   isDiagonal && (king.x > piece.x && king.y < piece.y) && (Attacker.x <= piece.x && Attacker.y >= piece.y)
                                    else if (!(isDiagonal(availableMoves[key][i], kingPos) && (availableMoves[key][i].x < kingPos.x && availableMoves[key][i].y > kingPos.y)) && (availableMoves[key][i].x >= attackerPos[0].x && availableMoves[key][i].y <= attackerPos[0].y))
                                    {
                                        if (piecesData[key].name == "King")
                                            continue;
                                        availableMoves[key].Remove(availableMoves[key][i--]);
                                    }
                                    // illustration: draw a box on the edges of attacker and king
                                    // remove outsider pieces moves
                                    else if (availableMoves[key][i].y > attackerPos[0].y || availableMoves[key][i].x < attackerPos[0].x || availableMoves[key][i].y < kingPos.y || availableMoves[key][i].x > kingPos.x)
                                    {
                                        if (piecesData[key].name == "King")
                                            continue;
                                        availableMoves[key].Remove(availableMoves[key][i--]);
                                    }
                                }
                            }
                        }
                    }

                    // Bottom-Right Diagonal
                    else if (kingPos.x < attackerPos[0].x && kingPos.y > attackerPos[0].y)
                    {
                        // Valid Moves: Attacker <= king's protection Pieces(Attack Blocker or Attacker capturer) < king
                        foreach (Vector2Int key in piecesData.Keys)
                        {
                            if (piecesData[key].color == playerTurn)
                            {
                                for (int i = 0; i < availableMoves[key].Count; i++)
                                {
                                    // Remove the king Unsafe Moves
                                    if (piecesData[key].name == "King" && isDiagonal(availableMoves[key][i], kingPos) && kingPos.x > availableMoves[key][i].x && kingPos.y < availableMoves[key][i].y)
                                        availableMoves[key].Remove(availableMoves[key][i--]);

                                    // Remove all the other invalid Moves
                                    //valid:   isDiagonal && (piece.x > king.x && piece.y < king.y) && (Attacker.x >= piece.x && Attacker.y <= piece.y)
                                    else if (!(isDiagonal(availableMoves[key][i], kingPos) && (availableMoves[key][i].x > kingPos.x && availableMoves[key][i].y < kingPos.y)) && (availableMoves[key][i].x <= attackerPos[0].x && availableMoves[key][i].y >= attackerPos[0].y))
                                    {
                                        if (piecesData[key].name == "King")
                                            continue;
                                        availableMoves[key].Remove(availableMoves[key][i--]);
                                    }

                                    // remove outsider pieces moves
                                    else if (availableMoves[key][i].y < attackerPos[0].y || availableMoves[key][i].x > attackerPos[0].x || availableMoves[key][i].y > kingPos.y || availableMoves[key][i].x < kingPos.x)
                                    {
                                        if (piecesData[key].name == "King")
                                            continue;
                                        availableMoves[key].Remove(availableMoves[key][i--]);
                                    }
                                }
                            }
                        }
                    }

                }
            }
            #endregion

        }

        movesLeft = 0;
        
        // count pieces
        foreach (var key in piecesData.Keys)
        {
            if (piecesData[key].color == playerTurn)
            {
                for (int i = 0; i < availableMoves[key].Count; i++)
                    movesLeft++;
                
            }
        }
    }

    bool greater(Vector2Int v1, Vector2Int v2)
    {
        return v1.x > v2.x && v1.y > v2.y;
    }

    bool less(Vector2Int v1, Vector2Int v2)
    {
        return v1.x < v2.x && v1.y < v2.y;
    }

    bool isDiagonal(Vector2Int v1, Vector2Int v2)
    {
        return Math.Abs(v1.x - v2.x) == Math.Abs(v1.y - v2.y);
    }

    #endregion

    #region Highlighting Available Moves 
    void drawDots(Vector2Int pos)
    {
        for (int i = 0; i < availableMoves[pos].Count; i++)
        {
            int xMove = availableMoves[pos][i].x;
            int yMove = availableMoves[pos][i].y;

            GameObject Dot = Instantiate(Dot_Prefab, GetTileCenter(xMove, yMove), Quaternion.identity);
            Dot.name = String.Format("Available Move"); //  to give name to the GameObject
            Dot.GetComponent<SpriteRenderer>().sortingOrder = 1;
            Dot.transform.parent = chessBoard.board[xMove, yMove].transform;
            Dot.transform.position = chessBoard.board[xMove, yMove].transform.position;
            highlightedMoves.Add(Dot);
        }
    }

    private void deleteCircles()
    {
        for (int i = 0; i < highlightedMoves.Count; i++) 
            Destroy(highlightedMoves[i]);
    }

    #endregion

    #region Calculating Available Moves
    private void calculateAvailableMoves()
    {
        w_KingBlockSq.Clear();
        b_KingBlockSq.Clear();

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Vector2Int piecePos = new Vector2Int(x, y);

                if (piecesData.ContainsKey(piecePos))
                {
                    string Name = piecesData[piecePos].name;
                    char color = piecesData[piecePos].color;
                    switch (Name)
                    {
                        case "King":
                            if(color == 'W') {
                                availableMoves[piecePos] = KingMoves(x, y, color, b_KingBlockSq);
                                w_kingPos = piecePos;
                            } 
                            else
                            {
                                availableMoves[piecePos] = KingMoves(x, y, color, w_KingBlockSq);
                                b_kingPos = piecePos;
                            }
                            break;

                        case "Queen":
                            if(color == 'W')
                                availableMoves[piecePos] = QueenMoves(x, y, color, b_KingBlockSq);
                            else
                                availableMoves[piecePos] = QueenMoves(x, y, color, w_KingBlockSq);
                            break;
                        
                        case "Rook":
                            if (color == 'W')
                                availableMoves[piecePos] = RookMoves(x, y, color, b_KingBlockSq);
                            else
                                availableMoves[piecePos] = RookMoves(x, y, color, w_KingBlockSq);
                            break;

                        case "Bishop":
                            if (color == 'W')
                                availableMoves[piecePos] = BishopMoves(x, y, color, b_KingBlockSq);
                            else 
                                availableMoves[piecePos] = BishopMoves(x, y, color, w_KingBlockSq);
                            break;

                        case "Knight":
                            if (color == 'W')
                                availableMoves[piecePos] = KnightMoves(x, y, color, b_KingBlockSq);
                            else 
                                availableMoves[piecePos] = KnightMoves(x, y, color, w_KingBlockSq);
                            break;

                        case "Pawn":
                            if (color == 'W')
                                availableMoves[piecePos] = PawnMoves(x, y, color, b_KingBlockSq);
                            else
                                availableMoves[piecePos] = PawnMoves(x, y, color, w_KingBlockSq);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        //king possible squares and castling moves always change after each move
        if (playerTurn == 'W'){
            makeSqaureForKing(w_kingPos, b_kingPos, w_KingBlockSq);

            Vector2Int castleKing = new Vector2Int(0, 4); // king first position
            Vector2Int leftRook = new Vector2Int(0, 0);
            Vector2Int rightRook = new Vector2Int(0, 7);

            updateCastleMoves(castleKing, w_kingPos, leftRook, rightRook);
        }

        else 
        {
            makeSqaureForKing(b_kingPos, w_kingPos, b_KingBlockSq);

            Vector2Int castleKing = new Vector2Int(7, 4);
            Vector2Int leftRook = new Vector2Int(7, 0);
            Vector2Int rightRook = new Vector2Int(7, 7);

            updateCastleMoves(castleKing, b_kingPos, leftRook, rightRook);
        }
    }

    #region Updating Castling Possibilities
    private void updateCastleMoves(Vector2Int castleKing, Vector2Int king, Vector2Int leftRook, Vector2Int rightRook)
    {
        if (!castleMove.ContainsKey(castleKing) || castleKing != king)
        {
            // castleKing != king => if king moved
            if (castleMove.ContainsKey(castleKing))
                castleMove.Remove(castleKing);

            return;
        }


        CheckAndRemoveRook(leftRook, castleKing);
        CheckAndRemoveRook(rightRook, castleKing);
    }

    private void CheckAndRemoveRook(Vector2Int rookPos, Vector2Int castleKing)
    {
        // !piecesData.ContainsKey(rookPos) => Rook Moved
        // piecesData[rookPos].color != playerTurn => Rook Captured by Opponent piece

        if (!piecesData.ContainsKey(rookPos) || piecesData[rookPos].color != playerTurn)
        {
            if (castleMove[castleKing].Contains(rookPos))
                castleMove[castleKing].Remove(rookPos);
            
        }
    }

    #endregion

    void makeSqaureForKing(Vector2Int myKing, Vector2Int oppoKing, List<Vector2Int> banSquare)
    {
        List<Vector2Int> oppoKingMoves = availableMoves[oppoKing];

        for (int i = 0; i < availableMoves[myKing].Count; i++)
        {
            // oppoKingMoves.Contains(availableMoves[myKing][i]) -> Removing all the moves that're occupied by opponent king
            if (banSquare.Contains(availableMoves[myKing][i]) || oppoKingMoves.Contains(availableMoves[myKing][i])) 
                availableMoves[myKing].Remove(availableMoves[myKing][i--]);
            
        }

        
    }

    private void dangerSquareForKing(List<Vector2Int> list, List<Vector2Int> kingBlockSq)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (!kingBlockSq.Contains(list[i]))
                kingBlockSq.Add(list[i]);
        }
    }

    #region Knight Moves
    private List<Vector2Int> KnightMoves(int x, int y, char color, List<Vector2Int> kingBlockSq)
    {
        List<Vector2Int> list = new List<Vector2Int>();

        if(IsWithinBounds(x + 2, y - 1))
            childOfKnight(list, kingBlockSq, new Vector2Int(x + 2, y - 1), color);        

        if (IsWithinBounds(x + 1, y - 2))
            childOfKnight(list, kingBlockSq, new Vector2Int(x + 1, y - 2), color);
        
        if (IsWithinBounds(x - 1, y - 2))
            childOfKnight(list, kingBlockSq, new Vector2Int(x - 1, y - 2), color);
        
        if (IsWithinBounds(x - 2, y - 1))
            childOfKnight(list, kingBlockSq, new Vector2Int(x - 2, y - 1), color);
        
        if (IsWithinBounds(x - 1, y + 2))
            childOfKnight(list, kingBlockSq, new Vector2Int(x - 1, y + 2), color);
        
        if (IsWithinBounds(x - 2, y + 1))
            childOfKnight(list, kingBlockSq, new Vector2Int(x - 2, y + 1), color);
        
        if (IsWithinBounds(x + 2, y + 1))
            childOfKnight(list, kingBlockSq, new Vector2Int(x + 2, y + 1), color);

        if (IsWithinBounds(x + 1, y + 2))
            childOfKnight(list, kingBlockSq, new Vector2Int(x + 1, y + 2), color);
        
        dangerSquareForKing(list, kingBlockSq);
        return list;
    }

    private void childOfKnight(List<Vector2Int> list, List<Vector2Int> kingBlockSq, Vector2Int move, char color)
    {
        if (piecesData.ContainsKey(move))
        {
            if (piecesData[move].color != color)
                list.Add(move);

            // giving protection to fellow piece from opponent king
            else if (!kingBlockSq.Contains(move))
                kingBlockSq.Add(move);
        }
        else
            list.Add(move);
    }
    #endregion 

    #region King Moves
    private List<Vector2Int> KingMoves(int x, int y, char color, List<Vector2Int> kingBlockSq)
    {
        List<Vector2Int> list = new List<Vector2Int>();

        // => Moves on the same Row

        // Vector2Int(0, -1)
        if(IsWithinBounds(x, y - 1))
            childOfKing(list, kingBlockSq, new Vector2Int(x, y - 1), color);

        // Vector2Int(0, 1));
        if (IsWithinBounds(x, y + 1))
            childOfKing(list, kingBlockSq, new Vector2Int(x, y + 1), color);
        

        // => Upper Row Moves

        // Vector2Int(1, 0)
        if (IsWithinBounds(x + 1, y))
            childOfKing(list, kingBlockSq, new Vector2Int(x + 1, y), color);

        // Vector2Int(1, -1)
        if (IsWithinBounds(x + 1, y - 1 ))
            childOfKing(list, kingBlockSq, new Vector2Int(x + 1, y - 1), color);

        //Vector2Int(1, 1)
        if (IsWithinBounds(x + 1, y + 1))
            childOfKing(list, kingBlockSq, new Vector2Int(x + 1, y + 1), color);
        

        // => Bottom Row Moves

        // Vector2Int(-1, 0)
        if(IsWithinBounds(x - 1, y))
            childOfKing(list, kingBlockSq, new Vector2Int(x - 1, y), color);

        // Vector2Int(-1, -1)
        if (IsWithinBounds(x - 1, y - 1))
            childOfKing(list, kingBlockSq, new Vector2Int(x - 1, y - 1), color);
        
        // Vector2Int(-1, 1)
        if(IsWithinBounds(x - 1, y + 1))
            childOfKing(list, kingBlockSq, new Vector2Int(x - 1, y + 1), color);
        
        return list;
    }

    private void childOfKing(List<Vector2Int> list, List<Vector2Int> kingBlockSq, Vector2Int move, char color)
    {
        if (piecesData.ContainsKey(move))
        {
            if (piecesData[move].color != color)
                list.Add(move);

            // giving protection to fellow piece from opponent king
            else if (!kingBlockSq.Contains(move))
                kingBlockSq.Add(move);
        }
        else
            list.Add(move);
    }

    #endregion

    #region Queen Moves

    private List<Vector2Int> QueenMoves(int x, int y, char color, List<Vector2Int> KingBlockSq)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        list.AddRange(BishopMoves(x, y, color, KingBlockSq));
        list.AddRange(RookMoves(x, y, color, KingBlockSq));
        
        dangerSquareForKing(list, KingBlockSq);
        return list;
    }

    #endregion

    #region Bishop Moves
    private List<Vector2Int> BishopMoves(int x, int y, char color, List<Vector2Int> kingBlockSq)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        Vector2Int move;

        #region Simple Code Version 
        // piece with the same color then break 
        //for (int i = 1; i < 8; i++)
        //{
        //    move = new Vector2Int(x + i, y + i);
        //    if (move.x >= 8 || move.y >= 8) break;

        //if (piecesData.ContainsKey(move))
        //{

        //    if (piecesData[move].color == color){

        //        // fellow piece give protection from opponent king
        //        if (!KingBlockSq.Contains(move))
        //            KingBlockSq.Add(move);

        //        break;
        //    }

        //    // if the piece with opposite color & then break
        //    list.Add(move);

        //    break;
        //}
        //list.Add(move);
        //}
        #endregion

        // Top-Right Diagonal => Both increases

        for (int i = 1; i < 8; i++)
        {
            move = new Vector2Int(x + i, y + i);
            
            if (move.x >= 8 || move.y >= 8) break;

            bool breakTheLoop = childOfBishop(list, kingBlockSq, move, color);
            if (breakTheLoop) break;

        }

        // Top-Left Diagonal => x increases & y decreses
        for (int i = 1; i < 8; i++)
        {
            move = new Vector2Int(x + i, y - i);

            if (move.x >= 8 || move.y < 0) break;

            bool breakTheLoop = childOfBishop(list, kingBlockSq, move, color);
            if (breakTheLoop) break;
        }
        // Bottom-Right Diagonal => x decreases & y increases
        for (int i = 1; i < 8; i++)
        {
            move = new Vector2Int(x - i, y + i);
            
            if (move.x < 0 || move.y >= 8) break;

            bool breakTheLoop = childOfBishop(list, kingBlockSq, move, color);
            if (breakTheLoop) break;
        }
        // Bottom-Left Diagonal => Both decreses
        for (int i = 1; i < 8; i++)
        {
            move = new Vector2Int(x - i, y - i);
            
            if (move.x < 0 || move.y < 0) break;
            
            bool breakTheLoop = childOfBishop(list, kingBlockSq, move, color);
            if (breakTheLoop) break;
        }

        dangerSquareForKing(list, kingBlockSq);
        return list;
    }

    private bool childOfBishop(List<Vector2Int> list, List<Vector2Int> kingBlockSq, Vector2Int move, char color) 
    {
        if (piecesData.ContainsKey(move))
        {
            if (piecesData[move].color == color)
            {
                // fellow piece give protection from opponent king
                if (!kingBlockSq.Contains(move))
                    kingBlockSq.Add(move);

                return true; // break the loop same color piece there
            }

            // Add move to the list if the piece with opposite color & then break the loop
            list.Add(move);
            return true; 
        }
        list.Add(move);
        return false; // don't break the loop
    }

    #endregion

    #region Rook Moves
    
    private List<Vector2Int> RookMoves(int x, int y, char color, List<Vector2Int> kingBlockSq)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        Vector2Int move;

        // Calculating Upward Moves => y - Always Same
        for (int i = x + 1; i < 8; i++)
        {
            move = new Vector2Int(i, y);

            bool breakTheLoop = childOfRook(list, kingBlockSq, move, color);
            if(breakTheLoop) break;
        }

        // Calculating Downward Moves => y - Always Same
        for (int i = x - 1; i >= 0; i--)
        {
            move = new Vector2Int(i, y);
            
            bool breakTheLoop = childOfRook(list, kingBlockSq, move, color);
            if (breakTheLoop) break;
        }


        // Calculating LeftSide Moves => x - Always Same
        for (int i = y - 1; i >= 0; i--)
        {
            move = new Vector2Int(x, i);
            
            bool breakTheLoop = childOfRook(list, kingBlockSq, move, color);
            if (breakTheLoop) break;
        }


        // Calculating RightSide Moves => x - Always Same
        for (int i = y + 1; i < 8; i++)
        {
            move = new Vector2Int(x, i);

            bool breakTheLoop = childOfRook(list, kingBlockSq, move, color);
            if (breakTheLoop) break;
        }

        dangerSquareForKing(list, kingBlockSq);
        return list;
    }

    private bool childOfRook(List<Vector2Int> list, List<Vector2Int> kingBlockSq, Vector2Int move, char color)
    {
        if (piecesData.ContainsKey(move))
        {
            if (piecesData[move].color == color) // same color piece, then stop the loop
            {
                if (!kingBlockSq.Contains(move)) // blocking opponent king 
                    kingBlockSq.Add(move);

                return true;
            }

            list.Add(move); // opposite color save the position of enemy piece and then break
            return true;
        }
        list.Add(move);
        return false;
    }

    #endregion

    #region Pawns

    private List<Vector2Int> PawnMoves(int x, int y, char color, List<Vector2Int> kingBlockSq)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        Vector2Int move;
        int direction = (color == 'W') ? 1 : -1; // White moves up, Black moves down

        // Forward moves
        if (IsWithinBounds(x + direction, y) && !piecesData.ContainsKey(new Vector2Int(x + direction, y)))
        {
            if (IsWithinBounds(x + 2 * direction, y) && !piecesData.ContainsKey(new Vector2Int(x + 2 * direction, y)) && ((color == 'W' && x == 1) || (color == 'B' && x == 6)))
                list.Add(new Vector2Int(x + 2 * direction, y));

            list.Add(new Vector2Int(x + direction, y));
        }

        // Killing moves
        foreach (var (dx, dy) in new[] { (-1, -1), (-1, 1) })
        {
            move = new Vector2Int(x + direction, y + dy);
            if (IsWithinBounds(move.x, move.y) && piecesData.ContainsKey(move) && piecesData[move].color != color)
                list.Add(move);

            if (!kingBlockSq.Contains(move))
                kingBlockSq.Add(move);
        }
        return list;
    }


    #endregion

    #endregion

    #region Locating Pieces

    void _placePieces()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (x == 1)
                    _place(x, y, "Pawn", 'W', W_Pawn);

                else if (x == 6)
                    _place(x, y, "Pawn", 'B', B_Pawn);

                else if (x == 0)
                {
                    if (y == 4)
                        _place(x, y, "King", 'W', W_King);

                    if (y == 3)
                        _place(x, y, "Queen", 'W', W_Queen);

                    if (y == 0 || y == 7)
                        _place(x, y, "Rook", 'W', W_Rook);

                    if (y == 1 || y == 6)
                        _place(x, y, "Knight", 'W', W_Knight);

                    if (y == 2 || y == 5)
                        _place(x, y, "Bishop", 'W', W_Bishop);
                }

                else if (x == 7)
                {
                    if (y == 4)
                        _place(x, y, "King", 'B', B_King);

                    if (y == 3)
                        _place(x, y, "Queen", 'B', B_Queen);

                    if (y == 0 || y == 7)
                        _place(x, y, "Rook", 'B', B_Rook);

                    if (y == 1 || y == 6)
                        _place(x, y, "Knight", 'B', B_Knight);

                    if (y == 2 || y == 5)
                        _place(x, y, "Bishop", 'B', B_Bishop);

                }
                else break;

            }
        }
    }

    private void _place(int x, int y, string name, char color, GameObject piece)
    {
        GameObject Obj = Instantiate(piece, GetTileCenter(x, y), Quaternion.identity);
        Obj.transform.parent = chessBoard.board[x, y].transform;
        Obj.transform.position = chessBoard.board[x, y].transform.position;
        piecesData[new Vector2Int(x, y)] = new PiecesLocation(x, y, name, color, piece);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        Vector3 tilePosition = chessBoard.board[x, y].transform.position;
        float tileSize = 1f;
        return new Vector3((tilePosition.x + tileSize) / 2, (tilePosition.y + tileSize) / 2, 0);
    }
    #endregion

}
