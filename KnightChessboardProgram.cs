using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//This program is written in C# for a console application in Microsoft Visual C# 2008 Express

namespace ConsoleApplication1
{
    class Program
    {
        //At first I didn't think it was possible for a knight to cover a chessboard while touching each square
        //only once, so my program is set up to break up the board into "clusters" of squares that the knight could
        //cover in sequences without overlapping any squares. My plan was to create a method that found the most efficient way 
        //to then link those clusters together. I created four different methods for navigating the board to form 
        //clusters. The method that's now called by the Main program, pickSquarewithLeastOpenMovesMethod, is able
        //to cover the board in one continuous cluster starting from almost any square on the board. Once I found 
        //that out, developing a method to link different clusters together seemed unneccesary.
        
        static bool[,] touchedSquares = new bool[8, 8]; //This is an array that is used to keep track of which squares have been "touched,"
                                                        //or added to a cluster. This is reset to all false at the beginning of each cluster
                                                        //finding method.

        static int[,] clusters = new int[64, 64]; //This keeps track of all clusters, and is initialized at -1 for all elements at the beginning of
                                                  //each cluster finding method. It's a 64x64 array so that there can be one cluster that holds all 
                                                  //squares, or there can be 64 clusters that hold each square in its own cluster if needed. The 
                                                  //squares in this array are represented by a single integer, in which the tens digit indicates the 
                                                  //square's row and the ones digit indicates the column.

        static int currentSquare = 0, currentCluster = 0, currentClusterRow = 0; //These are used to keep track of the program's current location
                                                                                 //on the board as it forms clusters, and the last location used
                                                                                 //in the clusters array.

        //The main method steps through each square on the board and calls a cluster finding method to start from each square.
        //The program currently calls pickSquarewithLeastOpenMovesMethod because that (almost always) covers the board with just
        //one cluster, however, it may be replaced by one of the other cluster finding methods (pickRandomOpenSquareMethod, 
        //pickLastOddNumberMethod, or pickSquarewithMostOpenMovesMethod) for comparison.            
        static void Main()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    Console.Write("Starting at square ");
                    Console.WriteLine(i * 10 + j);
                    pickSquarewithLeastOpenMovesMethod(i * 10 + j);
                    Console.Clear();
                }
            }            
        }

        //This method navigates the board by advancing from the current square to a square randomly selected from among the legal moves
        //to untouched squares. This usually finds between 6 and 12 clusters. This method COULD have found a single cluster that covers the
        //entire board, but it's very unlikely.
        static void pickRandomOpenSquareMethod(int startingSquare)
        {
            Random rand = new Random();

            
            //Each time this method is called, the touchedSqaures and clusters arrays are re-initialized
            currentSquare = startingSquare;
            currentCluster = 0;
            currentClusterRow = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    touchedSquares[i, j] = false;
                }
            }
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    clusters[i, j] = -1;
                }
            }

            touchedSquares[Row(currentSquare), Column(currentSquare)] = true;
            clusters[currentCluster, currentClusterRow] = currentSquare;

            while (!allSquaresTouched())
            {
                //For each move, determine all legal moves from the current square and eliminate squares that have already been touched.
                //The possibleMoves array always has a length of 8, the maximum number of directions the knight can travel. Available
                //moves are represented by the index of the square that can be moved to, and illegal moves and moves to "used" squares
                //are represented by a -1. 
                int[] possibleMoves = determineLegalMoves(currentSquare);
                possibleMoves = eliminateUsedSquares(possibleMoves);

                if (countAvailableMoves(possibleMoves) > 0)
                {
                    //The program determines a random number between 0 and 7, and then increments this number until it matches the index of
                    //a valid move. This move becomes the next square in the current cluster.                
                    
                    int nextSquareIndex = (int)Math.Floor(8 * rand.NextDouble());
                    if (nextSquareIndex > 7)
                        nextSquareIndex = 7;
                    else if (nextSquareIndex < 0)
                        nextSquareIndex = 0;

                    while (possibleMoves[nextSquareIndex] < 0)
                    {
                        nextSquareIndex++;
                        if (nextSquareIndex > 7)
                            nextSquareIndex = 0;
                    }

                    currentSquare = possibleMoves[nextSquareIndex];
                    touchedSquares[currentSquare / 10, currentSquare % 10] = true;
                    currentClusterRow++;
                    clusters[currentCluster, currentClusterRow] = currentSquare;

                }
                else
                {
                    //If there are no moves to be made from the current square, then use the findNextOpenSquare method to
                    //find the next open square.
                    currentSquare = findNextOpenSquare();
                    touchedSquares[currentSquare / 10, currentSquare % 10] = true;
                    currentCluster++;
                    currentClusterRow = 0;
                    clusters[currentCluster, currentClusterRow] = currentSquare;
                }
            }

            //Once every square on the board has been put into a cluster, write out each cluster in sequence and
            //call Readline to pause the program until the user presses enter.
            for (int i = 0; i < 64; i++)
            {
                if (clusters[i, 0] >= 0)
                {
                    Console.Write("Cluster #");
                    Console.WriteLine(i.ToString());
                    for (int j = 0; j < 64; j++)
                    {
                        if (clusters[i, j] >= 0)
                        {
                            Console.Write(clusters[i, j]);
                            Console.Write(", ");
                        }
                    }
                    Console.WriteLine();
                }
            }
            Console.WriteLine("Done. Press enter to continue.");
            Console.ReadLine();
        }
                

        //This method navigates the board by advancing to the highest-indexed (which is arbitrary) possible move that has an odd number of possible
        //next moves. The idea behind this is that if the final path does need to cross itself, leaving an even number of unused paths across as many
        //squares as possible minimizes dead-ends that require back-tracking. This method is more efficient than the random wandering method, covering
        //the board in as few as 3, 4 or 5 clusters from some starting squares. Starting from square 25, this method creates one cluster that covers 62
        //squares, leaving two corner squares, 70 and 77, in their own clusters. These clusters can be joined to cover the board in as little as 67
        //moves. When I first found this I thought that 67 was either very close to the minimum number of moves required to cover the board, or that it
        //was the actual minimum number. I just couldn't figure out how I could PROVE it.
        static void pickLastOddNumberMethod(int startingSquare)
        {
            //Each time this method is called, the touchedSqaures and clusters arrays are re-initialized
            currentSquare = startingSquare;
            currentCluster = 0;
            currentClusterRow = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    touchedSquares[i, j] = false;
                }
            }
            
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    clusters[i, j] = -1;
                }
            }

            touchedSquares[currentSquare / 10, currentSquare % 10] = true;
            clusters[currentCluster, currentClusterRow] = currentSquare;

            while (!allSquaresTouched())
            {
                //For each move, determine all legal moves from the current square and eliminate squares that have already been touched.
                //The possibleMoves array always has a length of 8, the maximum number of directions the knight can travel. Available
                //moves are represented by the index of the square that can be moved to, and illegal moves and moves to "used" squares
                //are represented by a -1. 
                int[] possibleMoves = determineLegalMoves(currentSquare);
                possibleMoves = eliminateUsedSquares(possibleMoves);

                if (countAvailableMoves(possibleMoves) > 0)
                {

                    //For each possible move from the current square, these lines count the number of possible moves from each
                    //potential next square, storing the move in squareWithOddMoves if that number is odd. squareWithAnyMoves holds
                    //the number of the highest-index possible move, in case none of the potential moves have an odd number of possible
                    //moves that follow.                        
                    int squareWithOddMoves = -1;
                    int squareWithAnyMoves = -1;
                    for (int i = 0; i < possibleMoves.Length; i++)
                    {
                        if (possibleMoves[i] >= 0)
                        {
                            squareWithAnyMoves = i;
                            int squareMoveCount = countAvailableMoves(eliminateUsedSquares(determineLegalMoves(possibleMoves[i])));
                            if (squareMoveCount % 2 == 1)
                            {
                                squareWithOddMoves = i;
                            }
                        }
                    }

                    if (squareWithOddMoves >= 0)
                    {
                        currentSquare = possibleMoves[squareWithOddMoves];
                        touchedSquares[currentSquare / 10, currentSquare % 10] = true;
                        currentClusterRow++;
                        clusters[currentCluster, currentClusterRow] = currentSquare;
                    }
                    else
                    {
                        currentSquare = possibleMoves[squareWithAnyMoves];
                        touchedSquares[currentSquare / 10, currentSquare % 10] = true;
                        currentClusterRow++;
                        clusters[currentCluster, currentClusterRow] = currentSquare;
                    }
                }
                else
                {
                    //If there are no moves to be made from the current square, then use the findNextOpenSquare method to
                    //find the next open square.
                    currentSquare = findNextOpenSquare();
                    touchedSquares[currentSquare / 10, currentSquare % 10] = true;
                    currentCluster++;
                    currentClusterRow = 0;
                    clusters[currentCluster, currentClusterRow] = currentSquare;
                }
            }

            //Once every square on the board has been put into a cluster, write out each cluster in sequence and
            //call Readline to pause the program until the user presses enter.            
            for (int i = 0; i < 64; i++)
            {
                if (clusters[i, 0] >= 0)
                {
                    Console.Write("Cluster #");
                    Console.WriteLine(i.ToString());
                    for (int j = 0; j < 64; j++)
                    {
                        if (clusters[i, j] >= 0)
                        {
                            Console.Write(clusters[i, j]);
                            Console.Write(", ");
                        }
                    }
                    Console.WriteLine();
                }
            }
            Console.WriteLine("Done. Press enter to continue.");
            Console.ReadLine();
        }
        
        
        //This method navigates the board by advancing to the square that has the most possible next moves. The idea behind this was that by keeping
        //toward the squares that had the most open moves to look forward to, the program would prolong the time until it ran into a dead end for each
        //cluster. This method actually performs quite a bit worse than the random wandering method, breaking up the board into somewhere between
        //12 and 16 clusters, most of which consist of just one or two squares.
        static void pickSquarewithMostOpenMovesMethod(int startingSquare)
        {
            //Each time this method is called, the touchedSqaures and clusters arrays are re-initialized
            currentSquare = startingSquare;
            currentCluster = 0;
            currentClusterRow = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    touchedSquares[i, j] = false;
                }
            }
            
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    clusters[i, j] = -1;
                }
            }

            touchedSquares[currentSquare / 10, currentSquare % 10] = true;
            clusters[currentCluster, currentClusterRow] = currentSquare;

            while (!allSquaresTouched())
            {
                //For each move, determine all legal moves from the current square and eliminate squares that have already been touched.
                //The possibleMoves array always has a length of 8, the maximum number of directions the knight can travel. Available
                //moves are represented by the index of the square that can be moved to, and illegal moves and moves to "used" squares
                //are represented by a -1. 
                int[] possibleMoves = determineLegalMoves(currentSquare);
                possibleMoves = eliminateUsedSquares(possibleMoves);

                if (countAvailableMoves(possibleMoves) > 0)
                {
                    //For each move, the program finds the possible move that has the most potential next moves and moves to that square.
                    int squareWithMostMoves = -1;
                    int MostMoves = -1;
                    for (int i = 0; i < possibleMoves.Length; i++)
                    {
                        if (possibleMoves[i] >= 0)
                        {
                            int squareMoveCount = countAvailableMoves(eliminateUsedSquares(determineLegalMoves(possibleMoves[i])));
                            if (squareMoveCount > MostMoves)
                            {
                                squareWithMostMoves = i;
                                MostMoves = squareMoveCount;
                            }
                        }
                    }

                    
                    currentSquare = possibleMoves[squareWithMostMoves];
                    touchedSquares[currentSquare / 10, currentSquare % 10] = true;
                    currentClusterRow++;
                    clusters[currentCluster, currentClusterRow] = currentSquare;
                    
                }
                else
                {
                    //If there are no moves to be made from the current square, then use the findNextOpenSquare method to
                    //find the next open square.
                    currentSquare = findNextOpenSquare();
                    touchedSquares[currentSquare / 10, currentSquare % 10] = true;
                    currentCluster++;
                    currentClusterRow = 0;
                    clusters[currentCluster, currentClusterRow] = currentSquare;
                }
            }

            //Once every square on the board has been put into a cluster, write out each cluster in sequence and
            //call Readline to pause the program until the user presses enter.            
            for (int i = 0; i < 64; i++)
            {
                if (clusters[i, 0] >= 0)
                {
                    Console.Write("Cluster #");
                    Console.WriteLine(i.ToString());
                    for (int j = 0; j < 64; j++)
                    {
                        if (clusters[i, j] >= 0)
                        {
                            Console.Write(clusters[i, j]);
                            Console.Write(", ");
                        }
                    }
                    Console.WriteLine();
                }
            }
            Console.WriteLine("Done. Press enter to continue.");
            Console.ReadLine();
        }


        //After testing the three board navigation methods above, I thought that the path that covered the board in 67 moves (found by the 
        //pickLastOddNumberMethod) was either as efficient a path around the chess board as was possible, or it was pretty close. For verification
        //I poked around a bit on the Wolfram Alpha website and then on Wikipedia, where I found the article on the "Knight's Tour." The article
        //describes "Warnsdorff's algorithm," which navigates the board by selecting the move that has the FEWEST possible next moves.
        //This is bascially the opposite of the pickSquarewithMostOpenMovesMethod that I tested above, and it can find a path that navigates the
        //entire board while touching each square only once. I probably would have found this out eventually without any external research, as 
        //it makes sense that the opposite of the LEAST efficient pickSquarewithMostOpenMovesMethod would find the MOST efficient way around the 
        //chessboard. Regardless, this method is able to cover the board as one continuous cluster for almost any starting square, thus proving
        //that the minimum number of moves required for a knight to cover the chessboard is 63.
        static void pickSquarewithLeastOpenMovesMethod(int startingSquare)
        {
            //Each time this method is called, the touchedSqaures and clusters arrays are re-initialized
            currentSquare = startingSquare;
            currentCluster = 0;
            currentClusterRow = 0;

            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    touchedSquares[i, j] = false;
                }
            }
            
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    clusters[i, j] = -1;
                }
            }

            touchedSquares[currentSquare / 10, currentSquare % 10] = true;
            clusters[currentCluster, currentClusterRow] = currentSquare;

            while (!allSquaresTouched())
            {
                //For each move, determine all legal moves from the current square and eliminate squares that have already been touched.
                //The possibleMoves array always has a length of 8, the maximum number of directions the knight can travel. Available
                //moves are represented by the index of the square that can be moved to, and illegal moves and moves to "used" squares
                //are represented by a -1. 
                int[] possibleMoves = determineLegalMoves(currentSquare);
                possibleMoves = eliminateUsedSquares(possibleMoves);

                if (countAvailableMoves(possibleMoves) > 0)
                {
                    //For each move, the program finds the possible move that has the least potential next moves and moves to that square.
                    int squareWithLeastMoves = -1;
                    int LeastMoves = 10;
                    for (int i = 0; i < possibleMoves.Length; i++)
                    {
                        if (possibleMoves[i] >= 0)
                        {
                            int squareMoveCount = countAvailableMoves(eliminateUsedSquares(determineLegalMoves(possibleMoves[i])));
                            if (squareMoveCount < LeastMoves)
                            {
                                squareWithLeastMoves = i;
                                LeastMoves = squareMoveCount;
                            }
                        }
                    }


                    currentSquare = possibleMoves[squareWithLeastMoves];
                    touchedSquares[currentSquare / 10, currentSquare % 10] = true;
                    currentClusterRow++;
                    clusters[currentCluster, currentClusterRow] = currentSquare;

                }
                else
                {
                    //If there are no moves to be made from the current square, then use the findNextOpenSquare method to
                    //find the next open square.
                    currentSquare = findNextOpenSquare();
                    touchedSquares[currentSquare / 10, currentSquare % 10] = true;
                    currentCluster++;
                    currentClusterRow = 0;
                    clusters[currentCluster, currentClusterRow] = currentSquare;
                }
            }

            //Once every square on the board has been put into a cluster, write out each cluster in sequence and
            //call Readline to pause the program until the user presses enter.
            for (int i = 0; i < 64; i++)
            {
                if (clusters[i, 0] >= 0)
                {
                    Console.Write("Cluster #");
                    Console.WriteLine(i.ToString());
                    for (int j = 0; j < 64; j++)
                    {
                        if (clusters[i, j] >= 0)
                        {
                            Console.Write(clusters[i, j]);
                            Console.Write(", ");
                        }
                    }
                    Console.WriteLine();
                }
            }
            Console.WriteLine("Done. Press enter to continue.");
            Console.ReadLine();
        }
        
        //This method returns an array of 8 integers, listing the numbers of the squares that a knight can move to from the input CurrentSquare.
        //For moves that would place the knight outside the board, the value for that move is kept as -1.
        static int[] determineLegalMoves(int CurrentSquare)
        {
            int[] movesToReturn = new int[8];

            for (int i = 0; i < 8; i++)
            {
                movesToReturn[i] = -1;
            }

            if (Row(CurrentSquare) <= 5 & Column(CurrentSquare) >= 1)
            {
                movesToReturn[0] = (Row(CurrentSquare) + 2) * 10 + Column(CurrentSquare) - 1;
            }

            if (Row(CurrentSquare) <= 5 & Column(CurrentSquare) <= 6)
            {
                movesToReturn[1] = (Row(CurrentSquare) + 2) * 10 + Column(CurrentSquare) + 1;
            }

            if (Row(CurrentSquare) <= 6 & Column(CurrentSquare) >= 2)
            {
                movesToReturn[2] = (Row(CurrentSquare) + 1) * 10 + Column(CurrentSquare) - 2;
            }

            if (Row(CurrentSquare) <= 6 & Column(CurrentSquare) <= 5)
            {
                movesToReturn[3] = (Row(CurrentSquare) + 1) * 10 + Column(CurrentSquare) + 2;
            }

            if (Row(CurrentSquare) >= 1 & Column(CurrentSquare) >= 2)
            {
                movesToReturn[4] = (Row(CurrentSquare) - 1) * 10 + Column(CurrentSquare) - 2;
            }

            if (Row(CurrentSquare) >= 1 & Column(CurrentSquare) <= 5)
            {
                movesToReturn[5] = (Row(CurrentSquare) - 1) * 10 + Column(CurrentSquare) + 2;
            }

            if (Row(CurrentSquare) >= 2 & Column(CurrentSquare) >= 1)
            {
                movesToReturn[6] = (Row(CurrentSquare) - 2) * 10 + Column(CurrentSquare) - 1;
            }

            if (Row(CurrentSquare) >= 2 & Column(CurrentSquare) <= 6)
            {
                movesToReturn[7] = (Row(CurrentSquare) - 2) * 10 + Column(CurrentSquare) + 1;
            }

            return movesToReturn;
        }

        //This method goes through an array of square values (usually returned by the determineLegalMoves method) and compares them to the 
        //touchedSquares array, replacing squares that are recorded as having been touched with the value of -1.
        static int[] eliminateUsedSquares(int[] MovesToScreen)
        {
            int[] MovesToReturn = MovesToScreen;

            for (int i = 0; i < MovesToReturn.Length; i++)
            {
                if (MovesToReturn[i] >= 0)
                {
                    if (touchedSquares[MovesToReturn[i] / 10, MovesToReturn[i] % 10])
                    {
                        MovesToReturn[i] = -1;
                    }
                }
            }

            return MovesToReturn;
        }

        //This method goes through an array of moves, returned by the determineLegalMoves or eliminateUsedSquares method, and counts the number
        //of valid moves (that don't equal -1) in that array.
        static int countAvailableMoves(int[] MovesToCount)
        {
            int Count = 0;

            for (int i = 0; i < MovesToCount.Length; i++)
            {
                if (MovesToCount[i] >= 0)
                {
                    Count++;
                }
            }

            return Count;
        }

        //This method is used when starting a new cluster. This method steps through the chessboard and returns the first square it comes across
        //that has not been recorded as having been touched.
        static int findNextOpenSquare()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (!touchedSquares[i, j])
                    {
                        return i * 10 + j;
                    }
                }
            }
            
            return -1;
        }

        //This method pulls out the row number (the tens digit) of a square-identifying integer.
        static int Row(int SquareNumber)
        {
            return SquareNumber / 10;
        }

        //This method pulls out the column number (the ones digit) of a square-identifying integer.
        static int Column(int SquareNumber)
        {
            return SquareNumber % 10;
        }
                
        //This method checks whether all squares on the chess board are recorded as having been touched. It is used by the cluster finding arrays
        //to determine when to stop making clusters.
        static bool allSquaresTouched()
        {
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (!touchedSquares[i, j])
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    
}
