using System.Drawing;
using System.Drawing.Drawing2D;
using AmiyaBotPlayerRatingServer.Data;
using AmiyaBotPlayerRatingServer.Model;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
                var thumbnailWidth = 192;
                var thumbnailHeight = 108;

                // Resize the image preserving the aspect ratio
                var resizeOptions = new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(thumbnailWidth, thumbnailHeight),
                    Mode = ResizeMode.Min
                };
                image.Mutate(x => x.Resize(resizeOptions));

                // Convert the image to a byte array
                using (var originalStream = new MemoryStream())
                {
                    await image.SaveAsync(originalStream, SixLabors.ImageSharp.Formats.Png.PngFormat.Instance); // Save as PNG
                    response.ImagePayload = originalStream.ToArray(); // Original image bytes
                }

                // Create the thumbnail image
                using (var thumbnailStream = new MemoryStream())
                {
                    await image.SaveAsPngAsync(thumbnailStream); // Save the thumbnail as PNG
                    response.ImagePayloadThumbnail = thumbnailStream.ToArray(); // Thumbnail image bytes
                }
            }

            // Update the database with the changes
            _dbContext.MAAResponses.Update(response);
            await _dbContext.SaveChangesAsync();
        }


    }
}
