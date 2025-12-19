using ChessChallenge.API;
using ChessChallenge.Application;
using System;

public class EvalAndMoveStorage {
    public float eval;
    public Move move;

    public EvalAndMoveStorage(float eval, Move move) {
        this.eval = eval;
        this.move = move;
    }

    public EvalAndMoveStorage(float eval) {
        this.eval = eval;
    }
}

public class MyBot : IChessBot
{

    public int[] CountMaterial(Board board)
    {
        int[] pieceValues = { 1, 3, 3, 5, 9, 0 };
        int[] pointsOfMaterial = new int[2];
        PieceList[] allPieces = board.GetAllPieceLists();

        foreach (PieceList pieceList in allPieces)
        {
            for (int i = 0; i < pieceList.Count; i++)
            {
                Piece currentPiece = pieceList.GetPiece(i);
                int pieceType = (int)currentPiece.PieceType;
                int forWhichColourDoesMaterialGetCounted = 1;

                if (currentPiece.IsWhite)
                {
                    forWhichColourDoesMaterialGetCounted = 0;
                }

                pointsOfMaterial[forWhichColourDoesMaterialGetCounted] += pieceValues[pieceType - 1];
            }
        }

        return pointsOfMaterial;
    }

    public float EvaluateFromMinorPieceDevelopment(Board board)
    {
        bool isBotWhite = board.IsWhiteToMove;
        float evaluation = 0.0F;
        int threshold = 8;
        int whoseTurnMultiplier = BoolToMultiplier(isBotWhite);
        float noDevelopmentPunishment = 0.4F;
        PieceList horsesList = board.GetPieceList(PieceType.Knight, isBotWhite);
        PieceList bishopsList = board.GetPieceList(PieceType.Bishop, isBotWhite);
        PieceList queenList = board.GetPieceList(PieceType.Queen, isBotWhite);
        //PieceList horsesListBlack = board.GetPieceList(PieceType.Knight, false);
        //PieceList bishopsListBlack = board.GetPieceList(PieceType.Bishop, false);

        if (!isBotWhite) {
            threshold = 55;
        }
        
        for (int i = 0; i < horsesList.Count; i++)
        {
            if (horsesList.GetPiece(i).Square.Index * whoseTurnMultiplier > threshold * whoseTurnMultiplier)
            {
                evaluation += whoseTurnMultiplier * noDevelopmentPunishment;
            }
        }

        for (int i = 0; i < bishopsList.Count; i++)
        {
            if (bishopsList.GetPiece(i).Square.Index * whoseTurnMultiplier > threshold * whoseTurnMultiplier)
            {
                evaluation += whoseTurnMultiplier * noDevelopmentPunishment;
            }
        } 
/*
        for (int i = 0; i < horsesListBlack.Count; i++)
        {
            if (horsesListBlack.GetPiece(i).Square.Index > 55)
            {
                evaluation += noDevelopmentPunishment;
            }
        }

        for (int i = 0; i < bishopsListBlack.Count; i++)
        {
            if (bishopsListBlack.GetPiece(i).Square.Index > 55)
            {
                evaluation += noDevelopmentPunishment;
            }
        }
*/
        return evaluation;
    }

    public int CountSpaceOneColour(Board board, bool isWhite)
    {
        int numOfSquares = 0;
        int squareShift = 0;

        if (!isWhite)
        {
            squareShift = 32;
        }

        for (int i = 0 + squareShift; i < 32 + squareShift; i++)
        {
            if (board.SquareIsAttackedByOpponent(new Square(i)))
            {
                numOfSquares += 1;
            }
        }

        return numOfSquares;
    }

    public float EvaluateFromSpace(Board board)
    {
        float spaceEval = 0.0F;
        float singleSquareEvalSwing = 0.08F;
        bool input = board.IsWhiteToMove;

        if (!input)
        {
            singleSquareEvalSwing *= -1;
        }

        spaceEval -= singleSquareEvalSwing * CountSpaceOneColour(board, input);

        board.ForceSkipTurn();

        spaceEval += singleSquareEvalSwing * CountSpaceOneColour(board, !input);

        board.UndoSkipTurn();

        return spaceEval;
    }

    public float NoLegalMovesEvaluator(Board board) {
        float eval = 0.0F;

        if (!board.IsDraw()) {
            if (board.IsWhiteToMove) {
                eval = -1000.0F;
            } else {
                eval = 1000.0F;
            }
        }

        return eval;
    }

    public float EvaluatePosition(Board board)
    {
        int[] materialForBothSides = CountMaterial(board);
        int materialEval = materialForBothSides[0] - materialForBothSides[1];
        float eval = (float)materialEval;

        eval += EvaluateFromSpace(board);
        eval += EvaluateFromMinorPieceDevelopment(board);

        /*if (board.GetKingSquare(true).Index > 15)
        {
            if (CountMaterial(board)[1] > 20)
            {
                eval -= 5;
            }
        }

        if (board.GetKingSquare(false).Index < 48)
        {
            if (CountMaterial(board)[0] > 20)
            {
                eval += 5;
            }
        }*/

        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0) {
            eval = NoLegalMovesEvaluator(board);
        }

        return eval;
    }

    public int BoolToMultiplier(bool value)
    {
        int multiplier = 1;

        if (value)
        {
            multiplier = -1;
        }

        return multiplier;
    }

    public int EndgameDepthUpdater(Board board, int materialThreshold, int depthUpdate) {
        int[] totalMaterialArray = CountMaterial(board);
        int totalMaterial = totalMaterialArray[0] + totalMaterialArray[1];

        if (totalMaterial < materialThreshold) {
            return depthUpdate;
        } else {
            return 0;
        }
    }

    public EvalAndMoveStorage Calculate(Board board, int depth)
    {
        if (depth < 2) {
            EvalAndMoveStorage staticEvaluation = new EvalAndMoveStorage(EvaluatePosition(board));

            return staticEvaluation;
        } else {
            Move[] moves = board.GetLegalMoves();

            if (moves.Length == 0) {
                EvalAndMoveStorage endOfGameEvaluation = new EvalAndMoveStorage(NoLegalMovesEvaluator(board));

                return endOfGameEvaluation;
            } else {
                Move candidateMove = moves[0];

                int whoseTurnMultiplier = BoolToMultiplier(board.IsWhiteToMove);

                //starts as bad as possible; 1000.0 is just an arbitrarily large eval
                float bestOutcome = 1000.0F * whoseTurnMultiplier;

                foreach (Move move in moves) {
                    board.MakeMove(move);
                    float earlyQueenPunishment = 0.0F;
                    
                    if (move.MovePieceType == PieceType.Queen && board.PlyCount < 16) {
                        earlyQueenPunishment = 1.0F;
                    }

                    depth--;
                    float dynamicEvaluation = Calculate(board, depth).eval + whoseTurnMultiplier * earlyQueenPunishment;
                    depth++;

                    if (dynamicEvaluation * whoseTurnMultiplier <= bestOutcome * whoseTurnMultiplier) {
                        candidateMove = move;
                        bestOutcome = dynamicEvaluation;
                    }

                    board.UndoMove(move);
                }

                //Console.WriteLine(bestOutcome + " current depth: " );
                EvalAndMoveStorage moveAndEval = new EvalAndMoveStorage(bestOutcome, candidateMove);

                return moveAndEval;
            }
        }
    }

    public Move Think(Board board, Timer timer)
    {
        int depth = 5;
        depth += EndgameDepthUpdater(board, 20, 1);
        Move moveToPlay = new();
            
        moveToPlay = Calculate(board, depth).move;

        float eval = EvaluatePosition(board);
        String FENString = board.GetFenString();

        Console.WriteLine("Evaluation: " + eval);
        Console.WriteLine("FEN: " + FENString);
        Console.WriteLine();

        return moveToPlay;
    }
}





//Old OnlyPawns Bot Code
/*public Move Think(Board board, Timer timer)
{
    Move moveToPlay = new();
    Move[] moves = board.GetLegalMoves();

    PieceList botPawns = board.GetPieceList(PieceType.Pawn, true);
    moveToPlay = moves[0];

    if (botPawns.Count > 0)
    {
        foreach (Move move in moves)
        {
            if (move.MovePieceType == PieceType.Pawn)
            {
                moveToPlay = move;
                break;
            }
        }
    }

    return moveToPlay;
}*/






/*public float Calculate(Board board, int depth, float originalEval, int initialDepth) {
    if (depth < 2) {
        return EvaluatePosition(board);
    } else {
        Move[] moves = board.GetLegalMoves();
        int whoseTurnMultiplier = BoolToMultiplier(board.IsWhiteToMove);

        //starts as bad as possible
        float bestOutcome = 1000.0F * whoseTurnMultiplier;

        foreach (Move move in moves) {
            board.MakeMove(move);

            float evaluation = EvaluatePosition(board);

            if (evaluation * whoseTurnMultiplier <= bestOutcome * whoseTurnMultiplier || depth > 3) {
                depth--;
                evaluation = Calculate(board, depth, originalEval, initialDepth);
                depth++;

                if (evaluation * whoseTurnMultiplier < bestOutcome * whoseTurnMultiplier) {
                    bestOutcome = evaluation;
                }
            }

            board.UndoMove(move);
        }

        //Console.WriteLine(bestOutcome + " current depth: " );
        return bestOutcome;
    }
}

public Move Variation(Board board, int depth) {
    Move moveToPlay = new();
    Move[] moves = board.GetLegalMoves();
    int whoseTurnMultiplier = BoolToMultiplier(board.IsWhiteToMove);

    depth += EndgameDepthUpdater(board, 30, 1);

    //starts as bad as possible for the bot, 1000.0 is just an arbitrarily large eval
    float bestOutcome = 1000.0F * whoseTurnMultiplier;

    foreach (Move move in moves) {
        if (move.IsCastles || move.MovePieceType == PieceType.King) {
            board.MakeMove(move);

            float currentEval = EvaluatePosition(board);

            if (whoseTurnMultiplier * EvaluatePosition(board) < whoseTurnMultiplier * currentEval - 2) {
                return move;
            }

            board.UndoMove(move);
        }

        if (move.MovePieceType != PieceType.King || board.IsInCheck()) {
            if (move.MovePieceType != PieceType.Queen || board.PlyCount > 16) {
                board.MakeMove(move);

                depth--;
                float evaluation = Calculate(board, depth, EvaluatePosition(board), depth);
                depth++;

                if (evaluation * whoseTurnMultiplier < bestOutcome * whoseTurnMultiplier) {
                    /*if (move.MovePieceType != PieceType.King)
                    {
                        moveToPlay = move;
                    }*//*

                    moveToPlay = move;
                    bestOutcome = evaluation;
                }

                board.UndoMove(move);
            }
        }

    }

    return moveToPlay;
}*/