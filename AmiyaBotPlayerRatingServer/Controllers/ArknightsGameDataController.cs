using AmiyaBotPlayerRatingServer.Data;
using DevLab.JmesPath;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("api/arknights")]
    [Produces("application/json")]
    public class ArknightsGameDataController(ArknightsMemoryCache memeCache) : ControllerBase
    {
#pragma warning disable CS8618
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public class JsonPathQueryModel
        {
            public string File { get; set; }
            public string Query { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Global
#pragma warning restore CS8618

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPost("json-path")]
        public IActionResult Query([FromBody] JsonPathQueryModel model)
        {
            try
            {
                var jsonData = memeCache.GetJson(model.File);
                if (jsonData != null)
                {
                    var result = jsonData.SelectToken(model.Query);
                    return Ok(result);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPost("jmes-path")]
        public IActionResult QueryJmes([FromBody] JsonPathQueryModel model)
        {
            try
            {
                var jsonData = memeCache.GetText(model.File);
                if (jsonData != null)
                {
                    JmesPath jmes = new JmesPath();
                    var result = jmes.Transform(jsonData, model.Query);
                    return Content(result, "application/json");
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}