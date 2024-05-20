using AmiyaBotPlayerRatingServer.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace AmiyaBotPlayerRatingServer.Hangfire
{
    public class MAAGenerateImageSnapshotService
    {
        private readonly PlayerRatingDatabaseContext _dbContext;

        public MAAGenerateImageSnapshotService(IConfiguration configuration, PlayerRatingDatabaseContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
        }

        public async Task GenerateThumbnail(string responseId)
        {
            var response = _dbContext.MAAResponses.FirstOrDefault(r => r.Id == Guid.Parse(responseId));

            if (response == null)
            {
                return;
            }

            if (String.IsNullOrWhiteSpace(response.Payload) || response.ImagePayload != null || response.ImagePayloadThumbnail != null)
            {
                return;
            }
            
            var imageBytes = Convert.FromBase64String(response.Payload);
            
            using (var image = Image.Load(imageBytes))
            {
                //保存原图
                using (var originalStream = new MemoryStream())
                {
                    await image.SaveAsync(originalStream, SixLabors.ImageSharp.Formats.Jpeg.JpegFormat.Instance); 
                    response.ImagePayload = originalStream.ToArray(); 
                }

                var thumbnailWidth = 192;
                var thumbnailHeight = 108;

                // 按照指定比例缩放
                var resizeOptions = new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(thumbnailWidth, thumbnailHeight),
                    Mode = ResizeMode.Min
                };
                image.Mutate(x => x.Resize(resizeOptions));
                
                // 保存缩略图
                using (var thumbnailStream = new MemoryStream())
                {
                    await image.SaveAsync(thumbnailStream, SixLabors.ImageSharp.Formats.Jpeg.JpegFormat.Instance);
                    response.ImagePayloadThumbnail = thumbnailStream.ToArray();
                }
            }
            
            _dbContext.MAAResponses.Update(response);
            await _dbContext.SaveChangesAsync();
        }


    }
}
