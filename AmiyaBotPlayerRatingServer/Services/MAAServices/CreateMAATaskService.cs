using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;

namespace AmiyaBotPlayerRatingServer.Services.MAAServices
{
    public class CreateMAATaskService
    {
        private readonly PlayerRatingDatabaseContext _context;

        public CreateMAATaskService(
            PlayerRatingDatabaseContext context)
        {
            _context = context;
        }

        public async Task<MAATask> CreateMAATask(Guid connectionId,String type,String parameters,Guid? parentRepetitiveTaskId=null)
        {
            var utcNow = DateTime.UtcNow;

            var userTask = new MAATask
            {
                ConnectionId = connectionId,
                Type = type,
                Parameters = parameters,
                CreatedAt = utcNow,
                IsCompleted = false,
                IsSystemGenerated = false,
                ParentRepetitiveTaskId = parentRepetitiveTaskId
            };

            var generatedTasks = new List<MAATask>();

            //if (userTask.Type == "LinkStart")
            //{
            //    // 生成一个LinkStart-StartUp任务
            //    var startUpTask = new MAATask
            //    {
            //        ConnectionId = connectionId,
            //        Type = "StartUp",
            //        Parameters = null,
            //        CreatedAt = utcNow.AddMilliseconds(100),
            //        IsCompleted = false,
            //        IsSystemGenerated = true,
            //        ParentTask = userTask
            //    };
            //    generatedTasks.Add(startUpTask);
            //}

            if (userTask.Type != "CaptureImage" && userTask.Type != "CaptureImageNow")
            {
                var captureTask = new MAATask
                {
                    ConnectionId = connectionId,
                    Type = "CaptureImage",
                    Parameters = null,
                    CreatedAt = utcNow.AddMilliseconds(200),
                    IsCompleted = false,
                    IsSystemGenerated = true,
                    ParentTask = userTask
                };
                generatedTasks.Add(captureTask);
            }

            await _context.MAATasks.AddAsync(userTask);

            foreach (var captureTask in generatedTasks)
            {
                await _context.MAATasks.AddAsync(captureTask);
            }

            await _context.SaveChangesAsync();

            return userTask;
        }
    }
}
