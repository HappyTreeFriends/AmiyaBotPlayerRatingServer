using AmiyaBotPlayerRatingServer.Data;
using DevLab.JmesPath;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace AmiyaBotPlayerRatingServer.Controllers
{
    [ApiController]
    [Route("api/arknights")]
    [Produces("application/json")]
    public class ArknightsGameDataController : ControllerBase
    {
        private readonly ArknightsMemoryCache _memeCache;

        public ArknightsGameDataController(ArknightsMemoryCache memeCache)
        {
            _memeCache = memeCache;
        }

        public class JsonPathQueryModel
        {
            public string File { get; set; }
            public string Query { get; set; }
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPost("json-path")]
        public IActionResult Query([FromBody] JsonPathQueryModel model)
        {
            try
            {
                var jsonData = _memeCache.GetJson(model.File);
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
                var jsonData = _memeCache.GetText(model.File);
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


        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "普通账户")]
        [HttpPost("operator-avatar")]
        public IActionResult GetArknightsAvatar()
        {
            //首先获取所有干员

            var operators = _memeCache.GetJson("character_table.json") as JObject;
            if (operators == null)
            {
                return NotFound("character_table.json not found");
            }

            var skins = _memeCache.GetJson("skin_table.json") as JObject;

            foreach(var opProp in operators.Properties())
            {
                var opId = opProp.Name;
                var opName = opProp.Value["name"]?.Value<string>();
                var opAvatar = opProp.Value["avatar"]?.Value<string>();
            }

            return Ok();
        }
    }
}