using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic.CypherChallenge;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic.SkillGuess;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;
using AmiyaBotPlayerRatingServer.Model;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RedLockNet.SERedis;
using StackExchange.Redis;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public class GameManager(
        IServiceProvider serviceProvider,
        IConnectionMultiplexer redisService,
        RedLockFactory redLockFactory,
        PlayerRatingDatabaseContext dbContext)
    {
        private readonly IDatabase _redisService = redisService.GetDatabase();

        public IGameManager CreateGameManager(string gameType)
        {
            return gameType switch
            {
                "SchulteGrid" => serviceProvider.GetService<SchulteGridGameManager>()!,
                "SkinGuess" => serviceProvider.GetService<SkinGuessManager>()!,
                "SkillGuess" => serviceProvider.GetService<SkillGuessManager>()!,
                "CypherChallenge" => serviceProvider.GetService<CypherChallengeManager>()!,
                _ => throw new ArgumentException("Invalid game type: "+gameType),
            };
        }

        private Game DeserializeGame(string gameJson)
        {
            var peek = JsonConvert.DeserializeObject<Game>(gameJson);
            var gameType = peek?.GameType;
            return gameType switch
            {
                "SchulteGrid" => JsonConvert.DeserializeObject<SchulteGridGame>(gameJson)!,
                "SkinGuess" => JsonConvert.DeserializeObject<SkinGuessGame>(gameJson)!,
                "SkillGuess" => JsonConvert.DeserializeObject<SkillGuessGame>(gameJson)!,
                "CypherChallenge" => JsonConvert.DeserializeObject<CypherChallengeGame>(gameJson)!,
                _ => throw new ArgumentException("Invalid game type" + gameType),
            };
        }

        public async Task<String> RequestJoinCode()
        {
            int maxTry = 100;
            string joinCode;
            int count;
            do
            {
                joinCode = new Random().Next(100000, 999999).ToString();
                maxTry--;
                if (maxTry == 0)
                {
                    return "";
                }
                //检查是否存在相同的joinCode
                count  = await dbContext.GameInfos.CountAsync(x => x.JoinCode == joinCode&&x.IsClosed==false);
            } while (count>0);

            return joinCode;
        }
        
        private async Task<Game?> GetGameFromRedis(string gameId)
        {
            var gameJson = await _redisService.StringGetAsync("AmiyaBot-Minigame-Game-" + gameId);
            if (gameJson.IsNullOrEmpty)
            {
                return null;
            }

            // 将JSON字符串转换为Game对象列表
            var game = DeserializeGame(gameJson!);
            return game;
        }

        private async Task SaveGameToRedis(Game game)
        {
            var gameJson = SerializeGame(game);
            await _redisService.StringSetAsync("AmiyaBot-Minigame-Game-" + game.Id, gameJson);
        }
        
        private string SerializeGame(Game game)
        {
            return JsonConvert.SerializeObject(game);
        }

        private string GenerateLockName(string gameId)
        {
            return "AmiyaBot-Minigame-Game-Lock-" + gameId;
        }
        
        public async Task<Game?> GetGameAsync(string gameId, bool readOnly = true)
        {
            //从数据库中获取查id

            var gameInfo = await dbContext.GameInfos.Include(x=>x.PlayerList).FirstOrDefaultAsync(a=>a.Id==gameId);

            if (gameInfo == null)
            {
                return null;
            }

            if (readOnly)
            {
                // 不需要获取锁，直接获取数据
                var game = await GetGameFromRedis(gameInfo.Id);
                if (game == null)
                {
                    return null;
                }

                return game;
            }
            else
            {
                // 获取锁
                var redisLock = await redLockFactory.CreateLockAsync(GenerateLockName(gameInfo.Id), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

                if (redisLock.IsAcquired)
                {
                    // 获取锁成功，继续操作
                    var game = await GetGameFromRedis(gameInfo.Id);
                    if (game == null)
                    {
                        return null;
                    }
                    game.RedLock= redisLock;
                    game.IsLocked = true;
                    return game;
                }
                else
                {
                    // 获取锁失败
                    return null;
                }

            }
        }

        public async Task<List<Game>> GetGameByCreatorIdAsync(string creatorId)
        {
            //从数据库中获取查id

            var gameInfo = await dbContext.GameInfos.Where(x => x.CreatorId == creatorId).ToListAsync();

            var ret = new List<Game>();

            foreach (var info in gameInfo)
            {
                // 不需要获取锁，直接获取数据
                var game = await GetGameFromRedis(info.Id);
                if (game == null)
                {
                    continue;
                }
                ret.Add(game);
            }

            return ret;
            
        }
        
        public async Task<Game?> GetGameByJoinCodeAsync(string joinCode, bool readOnly = true)
        {
            //从数据库中获取查id

            var info = await dbContext.GameInfos.Where(x => x.JoinCode == joinCode&&x.IsClosed==false).FirstOrDefaultAsync();

            if (info == null)
            {
                return null;
            }

            if (readOnly)
            {
                // 不需要获取锁，直接获取数据
                var game = await GetGameFromRedis(info.Id);
                if (game == null)
                {
                    return null;
                }
            }
            else
            {
                // 获取锁
                var redisLock = await redLockFactory.CreateLockAsync(GenerateLockName(info.Id),
                    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

                if (redisLock.IsAcquired)
                {
                    // 获取锁成功，继续操作
                    var game = await GetGameFromRedis(info.Id);
                    if (game == null)
                    {
                        return null;
                    }
                    game.RedLock = redisLock;
                    game.IsLocked = true;

                    return game;
                }
                else
                {
                    return null;
                }

            }

            return null;
        }

        [UsedImplicitly]
        public async Task<bool> SaveGameAsync(Game game)
        {
            GameInfo? gameInfo;
            if (game.Id == null)
            {
                gameInfo = new GameInfo();
            }
            else
            {
                if (!game.IsLocked || game.RedLock == null)
                {
                    //非锁定状态不可保存
                    return false;
                }

                gameInfo = await dbContext.GameInfos.Include(x => x.PlayerList).FirstOrDefaultAsync(g=>g.Id==game.Id);
                if (gameInfo == null)
                {
                    return false;
                }

                // 获取锁成功，检查是否存在数据冲突
                var existingGame = await GetGameFromRedis(game.Id);
                if (existingGame != null)
                {
                    // 如果存在冲突，则返回 false
                    if (existingGame.Version != game.Version)
                    {
                        // 数据冲突，不保存
                        return false;
                    }
                }
            }


            // 保存游戏信息到数据库
            foreach (var pPair in game.PlayerList)
            {
                var player = await dbContext.Users.FindAsync(pPair.Key);
                if (player != null)
                {
                    if (gameInfo.PlayerList.All(x => x.Id != player.Id))
                    {
                        gameInfo.PlayerList.Add(player);
                    }
                }

            }
            gameInfo.JoinCode = game.JoinCode;
            gameInfo.CreatorId = game.CreatorId;
            gameInfo.IsClosed = game.IsClosed;
            gameInfo.GameType = game.GameType;
            gameInfo.JoinCode = game.JoinCode;

            if (game.Id == null)
            {
                gameInfo.Id = Guid.NewGuid().ToString();
                await dbContext.GameInfos.AddAsync(gameInfo);
                game.Id = gameInfo.Id;
            }
            await dbContext.SaveChangesAsync();

            // 保存游戏数据到Redis
            game.Version++;
            await SaveGameToRedis(game);

            if (game.RedLock != null)
            {
                game.IsLocked = false;
                game.RedLock.Dispose();
            }

            return true;
        }

        public void CreateStatistics(IScorable game)
        {
            //只有至少有两人有分数的房间才会统计
            if (game.PlayerScore.Count(s => s.Value>0) < 2)
            {
                return;
            }

            //统计第一名第二名和第三名
            var playerScoreList = game.PlayerScore.ToList();
            playerScoreList.Sort((a, b) => b.Value.CompareTo(a.Value));

            var firstPlace = playerScoreList.Count > 0 ? playerScoreList[0] : default;
            var secondPlace = playerScoreList.Count > 1 ? playerScoreList[1] : default;
            var thirdPlace = playerScoreList.Count > 2 ? playerScoreList[2] : default;

            //统计每个玩家的正确和错误次数
            var playerAnswerList = game.PlayerMoveList.Where(x => x.IsValid).GroupBy(p => p.PlayerId)
                .Select(p => new
                {
                    PlayerId = p.Key,
                    CorrectCount = p.Count(c => c.IsCorrect),
                    WrongCount = p.Count(c => !c.IsCorrect)
                }).ToList();

            foreach (var pl in game.PlayerList)
            {
                var playerId = pl.Key;
                var playerSt =
                    dbContext.ApplicationUserMinigameStatistics.FirstOrDefault(x => x.UserId == playerId);
                if (playerSt == null)
                {
                    playerSt = new ApplicationUserMinigameStatistics()
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = playerId,
                        TotalGamesPlayed = 0,
                        TotalGamesFirstPlace = 0,
                        TotalGamesSecondPlace = 0,
                        TotalGamesThirdPlace = 0,
                        TotalAnswersCorrect = 0,
                        TotalAnswersWrong = 0
                    };
                    dbContext.ApplicationUserMinigameStatistics.Add(playerSt);
                }

                playerSt.TotalGamesPlayed++;
                playerSt.TotalAnswersCorrect +=
                    playerAnswerList.FirstOrDefault(x => x.PlayerId == playerId)?.CorrectCount ?? 0;
                playerSt.TotalAnswersWrong +=
                    playerAnswerList.FirstOrDefault(x => x.PlayerId == playerId)?.WrongCount ?? 0;

                if (firstPlace.Key == playerId)
                {
                    playerSt.TotalGamesFirstPlace++;
                }
                else if (secondPlace.Key == playerId)
                {
                    playerSt.TotalGamesSecondPlace++;
                }
                else if (thirdPlace.Key == playerId)
                {
                    playerSt.TotalGamesThirdPlace++;
                }

                dbContext.SaveChanges();
            }
        }
    }

}
