using System;

public class MapBuilder
{
    private Random _Random = new Random();
    public bool[,] Maze;

    public void GetRandomMapOfSize(int height, int width)
    {
        Maze = new bool[height, width];

        for (int i = 0; i < height; i++)
            for (int j = 0; j < width; j++)
                Maze[i, j] = true;
        
        int r = _Random.Next(height);
        while (r % 2 == 0)
            r = _Random.Next(height);
        
        int c = _Random.Next(width);
        while (c % 2 == 0)
            c = _Random.Next(width);

        Maze[r, c] = false;

        recursion(r, c);
    }

    public void recursion(int r, int c)
    {
        int height = Maze.GetLength(0);
        int width = Maze.GetLength(1);
        
        int[] directions = new int[] { 1, 2, 3, 4 };

        Shuffle(directions);

        for (int i = 0; i < directions.Length; i++)
        {
            switch (directions[i])
            {
                case 1: // Up
                    if (r - 2 <= 0)
                        continue;
                    if (Maze[r - 2, c])
                    {
                        Maze[r - 2, c] = false;
                        Maze[r - 1, c] = false;
                        recursion(r - 2, c);
                    }
                    break;
                case 2: // Right
                    if (c + 2 >= width - 1)
                        continue;
                    if (Maze[r, c + 2])
                    {
                        Maze[r, c + 2] = false;
                        Maze[r, c + 1] = false;
                        recursion(r, c + 2);
                    }
                    break;
                case 3: // Down
                    if (r + 2 >= height - 1)
                        continue;
                    if (Maze[r + 2, c])
                    {
                        Maze[r + 2, c] = false;
                        Maze[r + 1, c] = false;
                        recursion(r + 2, c);
                    }
                    break;
                case 4: // Left
                    if (c - 2 <= 0)
                        continue;
                    if (Maze[r, c - 2])
                    {
                        Maze[r, c - 2] = false;
                        Maze[r, c - 1] = false;
                        recursion(r, c - 2);
                    }
                    break;
            }
        }
    }

    // Fisher Yates Shuffle
    public void Shuffle<T>(T[] array)
    {
        for (int i = array.Length; i > 1; i--)
        {
            int j = _Random.Next(i); 
            T tmp = array[j];
            array[j] = array[i - 1];
            array[i - 1] = tmp;
        }
    }
}
