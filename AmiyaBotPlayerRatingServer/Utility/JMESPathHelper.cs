using DevLab.JmesPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AmiyaBotPlayerRatingServer.Utility
{
    public static class JMESPathHelper
    {
        public static JToken? JMESPathQuery(this JToken jToken, string jmesPath)
        {
            try
            {
                var jsonData = JsonConvert.SerializeObject(jToken);
                JmesPath jmes = new JmesPath();
                var result = jmes.Transform(jsonData, jmesPath);
                return JToken.Parse(result);
            }
            catch(Exception exp)
            {
                return null;
            }
        }
    }
}
