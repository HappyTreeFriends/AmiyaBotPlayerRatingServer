using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using AmiyaBotPlayerRatingServer.GameLogic.SkillGuess;
using AmiyaBotPlayerRatingServer.GameLogic.SkinGuess;
using AmiyaBotPlayerRatingServer.Model;
using AmiyaBotPlayerRatingServer.RealtimeHubs;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using RedLockNet.SERedis;
using StackExchange.Redis;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public class GameManager
    {
        //public readonly List<Game> GameList = new List<Game>();
        public readonly List<SystemNotification> Notifications = new List<SystemNotification>();

        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<GameHub> _gameHub;
        private readonly IDatabase _redisService;
        private readonly RedLockFactory _redLockFactory;
        private readonly PlayerRatingDatabaseContext _dbContext;

        public GameManager(IServiceProvider serviceProvider, 
            IHubContext<GameHub> gameHub, IConnectionMultiplexer redisService,
            RedLockFactory redLockFactory,
            PlayerRatingDatabaseContext dbContext)
        {
            _serviceProvider = serviceProvider;
            _gameHub = gameHub;
            _redisService = redisService.GetDatabase();
            _redLockFactory = redLockFactory;
            _dbContext = dbContext;
        }

        public IGameManager CreateGameManager(string gameType)
        {
            return gameType switch
            {
                "SchulteGrid" => _serviceProvider!.GetService<SchulteGridGameManager>()!,
                "SkinGuess" => _serviceProvider!.GetService<SkinGuessManager>()!,
                "SkillGuess" => _serviceProvider!.GetService<SkillGuessManager>()!,
                _ => throw new ArgumentException("Invalid game type"),
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
                count  = await _dbContext.GameInfos.CountAsync(x => x.JoinCode == joinCode&&x.IsClosed==false);
            } while (count>0);

            return joinCode;
        }
        
        [ItemCanBeNull]
        private async Task<Game?> GetGameFromRedis(string gameid)
        {
            var gameJson = await _redisService.StringGetAsync("AmiyaBot-Minigame-Game-" + gameid);
            if (gameJson.IsNullOrEmpty)
            {
                return null;
            }

            // 将JSON字符串转换为Game对象列表
            var game = DeserializeGame(gameJson);
            return game;
        }

        private async Task SaveGameToRedis(Game game)
        {
            var gameJson = SerializeGame(game);
            await _redisService.StringSetAsync("AmiyaBot-Minigame-Game-" + game.Id, gameJson);
        }

        private Game? DeserializeGame(string gameJson)
        {
            var peek = JsonConvert.DeserializeObject<Game>(gameJson);
            var gameType = peek.GameType;
            return gameType switch
            {
                "SchulteGrid" => JsonConvert.DeserializeObject<SchulteGridGame>(gameJson)!,
                "SkinGuess" => JsonConvert.DeserializeObject<SkinGuessGame>(gameJson)!,
                "SkillGuess" => JsonConvert.DeserializeObject<SkillGuessGame>(gameJson)!,
                _ => throw new ArgumentException("Invalid game type"),
            };
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

            var gameInfo = await _dbContext.GameInfos.Include(x=>x.PlayerList).FirstOrDefaultAsync(a=>a.Id==gameId);

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
                var redisLock = await _redLockFactory.CreateLockAsync(GenerateLockName(gameInfo.Id), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

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

            var gameInfo = await _dbContext.GameInfos.Where(x => x.CreatorId == creatorId.ToString()).ToListAsync();

            var ret = new List<Game>();

            foreach (var info in gameInfo)
            {
                    // 不需要获取锁，直接获取数据
                    var game = await GetGameFromRedis(info.Id);
                    if (game == null)
                    {
                        continue;
                    }

            }

            return ret;
            
        }

        public async Task<List<Game>> GetGameByPlayerIdAsync(string playerId, bool readOnly = true)
        {
            //从数据库中获取查id

            var gameInfo = await _dbContext.GameInfos.Include(x => x.PlayerList)
                .Where(x => x.PlayerList.Any(p => p.Id == playerId.ToString()))
                .ToListAsync();

            var ret = new List<Game>();

            foreach (var info in gameInfo)
            {
                if (readOnly)
                {
                    // 不需要获取锁，直接获取数据
                    var game = await GetGameFromRedis(info.Id);
                    if (game == null)
                    {
                        continue;
                    }
                }
                else
                {
                    // 获取锁
                    var redisLock = await _redLockFactory.CreateLockAsync(GenerateLockName(info.Id), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

                    if (redisLock.IsAcquired)
                    {
                        // 获取锁成功，继续操作
                        var game = await GetGameFromRedis(info.Id);
                        game.RedLock= redisLock;
                        game.IsLocked = true;
                        
                        ret.Add(game);
                    }
                    else
                    {
                        // 获取锁失败
                        continue;
                    }

                }

            }

            return ret;

        }

        public async Task<Game?> GetGameByJoinCodeAsync(string joinCode, bool readOnly = true)
        {
            //从数据库中获取查id

            var info = await _dbContext.GameInfos.Where(x => x.JoinCode == joinCode.ToString()).FirstOrDefaultAsync();

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
                var redisLock = await _redLockFactory.CreateLockAsync(GenerateLockName(info.Id),
                    TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

                if (redisLock.IsAcquired)
                {
                    // 获取锁成功，继续操作
                    var game = await GetGameFromRedis(info.Id);
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

        public async Task<bool> SaveGameAsync(Game game)
        {
            if (game == null)
            {
                return false;
            }

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

                gameInfo = await _dbContext.GameInfos.Include(x => x.PlayerList).FirstOrDefaultAsync(g=>g.Id==game.Id);
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
                var player = await _dbContext.Users.FindAsync(pPair.Key);
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
                await _dbContext.GameInfos.AddAsync(gameInfo);
                game.Id = gameInfo.Id;
            }
            await _dbContext.SaveChangesAsync();

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

    }

}
