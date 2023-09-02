using Microsoft.AspNetCore.Mvc;
using Aliyun.OSS;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class TestController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public TestController(IWebHostEnvironment env, IConfiguration configuration)
        {
            _env = env;
            _configuration = configuration;
        }

        [HttpGet]
        public object Index()
        {
            // 从appsettings.json获取OSS相关设置
            string endPoint = _configuration["Aliyun:Oss:EndPoint"];
            string bucket = _configuration["Aliyun:Oss:Bucket"];
            string key = _configuration["Aliyun:Oss:Key"];
            string secret = _configuration["Aliyun:Oss:Secret"];

            // 初始化OSS客户端
            var client = new OssClient(endPoint, key, secret);

            // 计算文件数量（简单示例，适用于文件数量不多的情况）
            ObjectListing result = null;
            string nextMarker = string.Empty;
            int fileCount = 0;

            do
            {
                var listObjectsRequest = new ListObjectsRequest(bucket)
                {
                    Marker = nextMarker,
                    MaxKeys = 100
                };

                // 列出对象
                result = client.ListObjects(listObjectsRequest);

                // 计数
                fileCount += result.ObjectSummaries.Count();

                nextMarker = result.NextMarker;

            } while (result.IsTruncated);

            return new { _env.EnvironmentName, FileCount = fileCount };
        }
    }
}