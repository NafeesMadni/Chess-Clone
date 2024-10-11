//using System.Collections;
//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;

//public class Bot : MonoBehaviour
//{
//    private const int MaxDepth = 3;
    
//    const int pawnVal = 10;
//    const int bishopVal = 30;
//    const int rookVal = 50;
//    const int knightVal = 30;
//    const int queenVal = 90;

//    // Function to evaluate the board. Positive values favor the AI, negative values favor the player.
//    public int EvaluateBoard(char color)
//    {
//        int count = 0;

//        return count;
//        //return board.Evaluate();
//    }

//    // Function to get all possible moves for the current player
//    public List<Move> GetPossibleMoves(Board board, bool isMaximizing)
//    {
//        // Implement logic to generate all possible moves for the current player
//        return board.GenerateLegalMoves(isMaximizing);
//    }

//    // Function to make a move on the board
//    public void MakeMove(Board board, Move move)
//    {
//        // Implement the logic to apply a move to the board
//        board.ApplyMove(move);
//    }

//    // Minimax function
//    public int Minimax(Board board, int depth, bool isMaximizing)
//    {
//        if (depth == 0 || board.IsGameOver())
//        {
//            return EvaluateBoard(board);
//        }

//        List<Move> possibleMoves = GetPossibleMoves(board, isMaximizing);

//        if (isMaximizing)
//        {
//            int maxEval = int.MinValue;
//            foreach (Move move in possibleMoves)
//            {
//                Board newBoard = board.Clone(); // Clone the board to simulate the move
//                MakeMove(newBoard, move);
//                int eval = Minimax(newBoard, depth - 1, false);
//                maxEval = Math.Max(maxEval, eval);
//            }
//            return maxEval;
//        }
//        else
//        {
//            int minEval = int.MaxValue;
//            foreach (Move move in possibleMoves)
//            {
//                Board newBoard = board.Clone(); // Clone the board to simulate the move
//                MakeMove(newBoard, move);
//                int eval = Minimax(newBoard, depth - 1, true);
//                minEval = Math.Min(minEval, eval);
//            }
//            return minEval;
//        }
//    }

//    // Function to be called after the player's move to get the best move for the AI
//    public Move GetBestMove(Board board)
//    {
//        int bestValue = int.MinValue;
//        Move bestMove = null;

//        List<Move> possibleMoves = GetPossibleMoves(board, true);

//        foreach (Move move in possibleMoves)
//        {
//            Board newBoard = board.Clone();
//            MakeMove(newBoard, move);
//            int moveValue = Minimax(newBoard, MaxDepth - 1, false);

//            if (moveValue > bestValue)
//            {
//                bestValue = moveValue;
//                bestMove = move;
//            }
//        }

//        return bestMove;
//    }
//}
