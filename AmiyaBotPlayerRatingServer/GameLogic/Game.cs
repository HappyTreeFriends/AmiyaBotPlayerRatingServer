using System.Collections.Concurrent;
using AmiyaBotPlayerRatingServer.Model;
using Newtonsoft.Json;
using RedLockNet;
using RedLockNet.SERedis;

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public class Game:IDisposable,IAsyncDisposable
    {
        public String Id { get; set; }
        public String JoinCode { get; set; }
        
        public String GameType { get; set; }

        public String CreatorId { get; set; }
        public String CreatorConnectionId { get; set; }
        public DateTime CreateTime { get; set; }

        public bool IsPrivate { get; set; }
        public String JoinPassword { get; set; }

        public bool IsStarted { get; set; }
        public DateTime? StartTime { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompleteTime { get; set; }
        public bool IsClosed { get; set; }
        public DateTime? CloseTime { get; set; }

        public ConcurrentDictionary<String, String> PlayerList { get; set; } = new ConcurrentDictionary<String,String>();

        public class RallyNode
        {
            public string Name { get; }
            public HashSet<int> PlayerIds { get; } = new HashSet<int>();
            public bool IsCompleted { get; set; }

            public RallyNode(string name)
            {
                Name = name;
            }

            public void AddPlayer(int playerId)
            {
                PlayerIds.Add(playerId);
            }
        }

        public ConcurrentDictionary<string, RallyNode> RallyNodes { get; } = new ConcurrentDictionary<string, RallyNode>();

        public int Version { get; set; }

        [JsonIgnore]
        public bool IsLocked { get; set; }
        [JsonIgnore]
        public IRedLock RedLock { get; set; }

        public void Dispose()
        {
            if (RedLock != null)
            {
                RedLock.Dispose();
            }
        }

        public ValueTask DisposeAsync()
        {
            if (RedLock != null)
            {
                RedLock.Dispose();
            }
            return ValueTask.CompletedTask;
        }
    }
}
