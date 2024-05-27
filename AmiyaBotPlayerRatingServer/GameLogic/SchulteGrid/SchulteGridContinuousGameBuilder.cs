namespace AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid
{
    public static class SchulteGridContinuousGameBuilder
    {
        public static async Task<Tuple<char[,]?, Dictionary<string, List<(int, int)>>?>> BuildPuzzleContinuousMode(int sizeX, int sizeY, List<string> words, List<string> blackList, int timeout)
        {
            var combinedWords = words.Concat(blackList).ToList();
            var validWords = combinedWords.Where(item => !combinedWords.Any(name => name != item && name.Contains(item))).ToList();
            validWords = validWords.Where(item => !blackList.Any(item.Contains)).ToList();

            int totalWordLength = validWords.Sum(w => w.Length);
            int totalCells = sizeX * sizeY;

            if (totalWordLength<totalCells)
                return Tuple.Create<char[,]?, Dictionary<string, List<(int, int)>>?>(null, null);

            var corners = new List<(int, int)> { (0, 0), (0, sizeY - 1), (sizeX - 1, 0), (sizeX - 1, sizeY - 1) };
        
            for (int i = 0; i<timeout; i++)
            {
                var(x, y) = corners[new Random().Next(corners.Count)];
                var(success, filledPuzzles, answers) = await FillPuzzle(new char[sizeY, sizeX], validWords, x, y);
            
                if (success && !IsUnwantedWordPresent(filledPuzzles, combinedWords, answers.Keys.ToList()))
                    return Tuple.Create<char[,]?, Dictionary<string, List<(int, int)>>?>(filledPuzzles, answers);
            }

            return Tuple.Create<char[,]?, Dictionary<string, List<(int, int)>>?>(null, null);
        }

        class TrieNode
        {
            public readonly Dictionary<char, TrieNode> Children = new();
            public bool IsEndOfWord;
        }

        class Trie
        {
            private readonly TrieNode _root = new();

            public void Insert(string word)
            {
                TrieNode node = _root;
                foreach (var ch in word)
                {
                    if (!node.Children.ContainsKey(ch))
                    {
                        node.Children[ch] = new TrieNode();
                    }
                    node = node.Children[ch];
                }
                node.IsEndOfWord = true;
            }

            public bool SearchFrom(char[,] puzzle, int x, int y, bool[,] visited)
            {
                return SearchFrom(puzzle, x, y, _root, visited);
            }

            private bool SearchFrom(char[,] puzzle, int x, int y, TrieNode node, bool[,] visited)
            {
                if (node.IsEndOfWord)
                    return true;

                int rows = puzzle.GetLength(0), cols = puzzle.GetLength(1);
                if (x < 0 || x >= cols || y < 0 || y >= rows || visited[y, x])
                    return false;

                char ch = puzzle[y, x];
                if (!node.Children.ContainsKey(ch))
                    return false;

                visited[y, x] = true;
                var directions = new List<(int, int)> { (0, 1), (0, -1), (1, 0), (-1, 0) };
                foreach (var (dx, dy) in directions)
                {
                    int newX = x + dx, newY = y + dy;
                    if (SearchFrom(puzzle, newX, newY, node.Children[ch], visited))
                    {
                        visited[y, x] = false;
                        return true;
                    }
                }

                visited[y, x] = false;
                return false;
            }
        }

        private static bool IsUnwantedWordPresent(char[,] puzzle, List<string> combinedWords, List<string> answers)
        {
            Trie trie = new Trie();
            foreach (var word in combinedWords.Except(answers))
            {
                trie.Insert(word);
            }

            int rows = puzzle.GetLength(0);
            int cols = puzzle.GetLength(1);
            bool[,] visited = new bool[rows, cols];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (trie.SearchFrom(puzzle, x, y, visited))
                        return true;
                }
            }
            return false;
        }

        private static async Task<Tuple<bool, char[,], Dictionary<string, List<(int, int)>>>> FillPuzzle(char[,] puzzle, List<string> words, int startX, int startY)
        {
            if (puzzle[startY, startX] != '\0' || !words.Any())
                return Tuple.Create(false, new char[0, 0], new Dictionary<string, List<(int, int)>>());

            char[,] tempPuzzle = (char[,])puzzle.Clone();
            int emptyCellsCount = CountEmptyCells(tempPuzzle);
            List<string> possibleWords = words.Where(w => w.Length <= emptyCellsCount).ToList();

            if (emptyCellsCount <= 6)
                possibleWords = possibleWords.Where(w => w.Length == emptyCellsCount).ToList();

            // random.shuffle(possible_words)
            Shuffle(possibleWords);

            Dictionary<int, string> lengthToWord = new Dictionary<int, string>();
            foreach (var word in possibleWords)
            {
                lengthToWord.TryAdd(word.Length, word);
            }

            possibleWords = lengthToWord.Values.ToList();
            TestOutputPuzzle(puzzle);

            while (possibleWords.Any())
            {
                string word = possibleWords.First();
                possibleWords.RemoveAt(0);
                var (success, resultPuzzles, paths, maxFillLength) = await FillChar(tempPuzzle, startX, startY, word);

                if (!success)
                {
                    possibleWords = possibleWords.Where(w => w.Length < maxFillLength).ToList();
                    continue;
                }

                foreach (var (resultPuzzle, path) in resultPuzzles.Zip(paths, Tuple.Create))
                {
                    if (!IsPuzzleEmpty(resultPuzzle))
                        return Tuple.Create(true, resultPuzzle, new Dictionary<string, List<(int, int)>> { { word, path } });

                    var maxSurroundingCells = FindMaxSurrounded(resultPuzzle);
                    foreach (var (newX, newY) in maxSurroundingCells)
                    {
                        var remainingWords = words.Where(w => w != word).ToList();
                        var (deepSuccess, deeperPuzzles, deeperAnswers) = await FillPuzzle(resultPuzzle, remainingWords, newX, newY);

                        if (deepSuccess)
                        {
                            deeperAnswers.Add(word, path);
                            return Tuple.Create(true, deeperPuzzles, deeperAnswers);
                        }
                    }
                }
            }

            return Tuple.Create(false, new char[0, 0], new Dictionary<string, List<(int, int)>>());
        }

        private static void Shuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        private static bool IsPuzzleEmpty(char[,] puzzle)
        {
            int rows = puzzle.GetLength(0);
            int cols = puzzle.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (puzzle[i, j] == '\0')
                        return true;
                }
            }
            return false;
        }

        private static int CountEmptyCells(char[,] puzzle)
        {
            int count = 0;
            int rows = puzzle.GetLength(0);
            int cols = puzzle.GetLength(1);
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (puzzle[y, x] == '\0')
                        count++;
                }
            }
            return count;
        }

        private static async Task<Tuple<bool, List<char[,]>, List<List<(int, int)>>, int>> FillChar(char[,] puzzle, int startX, int startY, string wordLeft)
        {
            if (puzzle[startY, startX] != '\0')
                return Tuple.Create(false, new List<char[,]>(), new List<List<(int, int)>>(), 0);

            char[,] tempPuzzle = (char[,])puzzle.Clone();
            tempPuzzle[startY, startX] = wordLeft[0];
            PrintLog($"Trying to fill char at ({startX}, {startY}), word_left:{wordLeft}");

            if (wordLeft.Length == 1)
            {
                if (!IsSingleConnected(tempPuzzle) || !ValidateConnectedGraph(tempPuzzle))
                    return Tuple.Create(false, new List<char[,]>(), new List<List<(int, int)>>(), 0);

                return Tuple.Create(true, new List<char[,]> { tempPuzzle }, new List<List<(int, int)>> { new() { (startX, startY) } }, 1);
            }

            var directions = new List<(int, int)> { (0, 1), (0, -1), (1, 0), (-1, 0) };
            var surroundingCells = new List<(int, int, int)>();

            foreach (var (dx, dy) in directions)
            {
                int newX = startX + dx, newY = startY + dy;
                if (newX >= 0 && newX < puzzle.GetLength(1) && newY >= 0 && newY < puzzle.GetLength(0) && tempPuzzle[newY, newX] == '\0')
                    surroundingCells.Add((newX, newY, CountSurrounded(tempPuzzle, newX, newY)));
            }

            surroundingCells.Sort((a, b) => b.Item3.CompareTo(a.Item3));
            var random = new Random();
            surroundingCells = surroundingCells.OrderBy(_ => random.Next()).ToList();

            List<char[,]> validPuzzles = new List<char[,]>();
            List<List<(int, int)>> validPaths = new List<List<(int, int)>>();
            int maxLength = 0;

            foreach (var (newX, newY, _) in surroundingCells)
            {
                var (success, resultPuzzles, paths, length) = await FillChar(tempPuzzle, newX, newY, wordLeft.Substring(1));
                maxLength = Math.Max(maxLength, length);
                if (success)
                {
                    foreach (var (p, path) in resultPuzzles.Zip(paths, Tuple.Create))
                    {
                        validPuzzles.Add(p);
                        validPaths.Add(new List<(int, int)> { (startX, startY) }.Concat(path).ToList());
                    }
                }
            }

            if (validPuzzles.Any())
                return Tuple.Create(true, validPuzzles, validPaths, wordLeft.Length);
            else
            {
                PrintLog("valid_puzzles empty");
                return Tuple.Create(false, new List<char[,]>(), new List<List<(int, int)>>(), maxLength + 1);
            }
        }

        private static List<(int, int)> FindMaxSurrounded(char[,] puzzle)
        {
            int maxSurrounded = 0;
            List<(int, int)> maxCoords = new List<(int, int)>();
            for (int y = 0; y < puzzle.GetLength(0); y++)
            {
                for (int x = 0; x < puzzle.GetLength(1); x++)
                {
                    if (puzzle[y, x] == '\0')
                    {
                        int surrounded = CountSurrounded(puzzle, x, y);
                        if (surrounded > maxSurrounded)
                        {
                            maxSurrounded = surrounded;
                            maxCoords = [(x, y)];
                        }
                        else if (surrounded == maxSurrounded)
                        {
                            maxCoords.Add((x, y));
                        }
                    }
                }
            }
            return maxCoords.OrderBy(coordinate => CountSurrounded(puzzle, coordinate.Item1, coordinate.Item2)).Reverse().ToList();
        }

        private static int CountSurrounded(char[,] puzzle, int x, int y)
        {
            int surrounded = 0;
            if (puzzle[y, x] == '\0')
            {
                if (x == 0 || x == puzzle.GetLength(1) - 1 || y == 0 || y == puzzle.GetLength(0) - 1)
                    surrounded++;
                if (x > 0 && puzzle[y, x - 1] == '1')
                    surrounded++;
                if (x < puzzle.GetLength(1) - 1 && puzzle[y, x + 1] == '1')
                    surrounded++;
                if (y > 0 && puzzle[y - 1, x] == '1')
                    surrounded++;
                if (y < puzzle.GetLength(0) - 1 && puzzle[y + 1, x] == '1')
                    surrounded++;
            }
            return surrounded;
        }

        private static bool IsSingleConnected(char[,] array)
        {
            bool[,] visited = new bool[array.GetLength(0), array.GetLength(1)];

            void Dfs(int i, int j)
            {
                if (i < 0 || i >= array.GetLength(0) || j < 0 || j >= array.GetLength(1))
                    return;
                if (visited[i, j] || array[i, j] != '\0')
                    return;
                visited[i, j] = true;
                Dfs(i - 1, j);
                Dfs(i + 1, j);
                Dfs(i, j - 1);
                Dfs(i, j + 1);
            }

            bool foundFirstZero = false;
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    if (array[i, j] == '\0' && !visited[i, j])
                    {
                        if (foundFirstZero)
                            return false;
                        Dfs(i, j);
                        foundFirstZero = true;
                    }
                }
            }

            return true;
        }

        private static bool ValidateConnectedGraph(char[,] matrix, int deadEndTolerance = 0, int nonAdjacentWallsTolerance = 0)
        {
            bool HasNonAdjacentWalls(int i, int j, char[,] mat)
            {
                int rows = mat.GetLength(0), cols = mat.GetLength(1);

                var directions = new List<(int, int)> { (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1) };
                List<int> walls = Enumerable.Repeat(0, 8).ToList();

                int idx;
                for (idx = 0; idx < directions.Count; idx++)
                {
                    var (di, dj) = directions[idx];
                    int ni = i + di, nj = j + dj;
                    if (ni >= 0 && ni < rows && nj >= 0 && nj < cols)
                    {
                        if (mat[ni, nj] != '\0')
                            walls[idx] = 1;
                    }
                }

                int startIdx = walls.IndexOf(1);
                if (startIdx == -1)
                    return false;

                idx = (startIdx - 1 + 8) % 8;
                while (walls[idx] == 1)
                {
                    walls[idx] = 2;
                    idx = (idx - 1 + 8) % 8;
                }

                idx = (startIdx + 1) % 8;
                while (walls[idx] == 1)
                {
                    walls[idx] = 2;
                    idx = (idx + 1) % 8;
                }

                walls[startIdx] = 2;

                return walls.Contains(1);
            }

            bool IsDeadEnd(int i, int j, char[,] mat)
            {
                int rows = mat.GetLength(0), cols = mat.GetLength(1);

                var directions = new List<(int, int)> { (0, 1), (1, 0), (0, -1), (-1, 0) };
                int countZeros = 0;

                foreach (var (di, dj) in directions)
                {
                    int ni = i + di, nj = j + dj;
                    if (ni >= 0 && ni < rows && nj >= 0 && nj < cols && mat[ni, nj] == '\0')
                        countZeros++;
                }

                return countZeros == 1;
            }

            bool IsCorridor(int i, int j, char[,] mat)
            {
                int rows = mat.GetLength(0), cols = mat.GetLength(1);

                if ((i - 1 < 0 || mat[i - 1, j] == '\0') && (i + 1 >= rows || mat[i + 1, j] == '\0'))
                {
                    if ((j - 1 < 0 || mat[i, j - 1] != '\0') && (j + 1 >= cols || mat[i, j + 1] != '\0'))
                        return true;
                }

                if ((j - 1 < 0 || mat[i, j - 1] == '\0') && (j + 1 >= cols || mat[i, j + 1] == '\0'))
                {
                    if ((i - 1 < 0 || mat[i - 1, j] != '\0') && (i + 1 >= rows || mat[i + 1, j] != '\0'))
                        return true;
                }

                return false;
            }

            int deadEndCount = 0, nonAdjacentWallsCount = 0;
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    if (matrix[i, j] == '\0')
                    {
                        if (IsDeadEnd(i, j, matrix))
                        {
                            deadEndCount++;
                            if (deadEndCount > deadEndTolerance)
                            {
                                TestOutputPuzzle(matrix);
                                PrintLog($"{i}, {j} is_dead_end");
                                return false;
                            }
                            else
                            {
                                PrintLog($"{i}, {j} is_dead_end");
                            }
                        }
                        else
                        {
                            if (IsCorridor(i, j, matrix))
                            {
                                TestOutputPuzzle(matrix);
                                PrintLog($"{i}, {j} is_corridor");
                                return false;
                            }

                            if (HasNonAdjacentWalls(i, j, matrix))
                            {
                                nonAdjacentWallsCount++;
                                if (nonAdjacentWallsCount > nonAdjacentWallsTolerance)
                                {
                                    TestOutputPuzzle(matrix);
                                    PrintLog($"{i}, {j} has_non_adjacent_walls");
                                    return false;
                                }
                                else
                                {
                                    PrintLog($"{i}, {j} has_non_adjacent_walls");
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        // ReSharper disable once UnusedParameter.Local
        private static void PrintLog(string message)
        {
            // Console.WriteLine(message);
        }

        private static void TestOutputPuzzle(char[,] puzzle)
        {
            int rows = puzzle.GetLength(0);
            int cols = puzzle.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                string row = "[";
                for (int j = 0; j < cols; j++)
                {
                    row += puzzle[i, j] == '\0' ? " 0 , " : $" '{puzzle[i, j]}', ";
                }
                row = row.TrimEnd(',', ' ') + "],";
                PrintLog(row);
            }
        }
    }
}
