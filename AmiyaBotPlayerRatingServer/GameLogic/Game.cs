﻿using System.Collections.Concurrent;
using Newtonsoft.Json;
using RedLockNet;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace AmiyaBotPlayerRatingServer.GameLogic
{
    public class Game:IDisposable,IAsyncDisposable
    {
        public String? Id { get; set; }
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

        public ConcurrentDictionary<String, String> PlayerList { get; set; } = new();

        public class RallyNode(string name)
        {
            public string Name { get; set; } = name;
            public HashSet<string> PlayerIds { get; set; } = new();
        }

        public ConcurrentDictionary<string, RallyNode> RallyNodes { get; } = new();

        public int Version { get; set; }

        [JsonIgnore]
        public bool IsLocked { get; set; }
        [JsonIgnore]
        public IRedLock? RedLock { get; set; }

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
