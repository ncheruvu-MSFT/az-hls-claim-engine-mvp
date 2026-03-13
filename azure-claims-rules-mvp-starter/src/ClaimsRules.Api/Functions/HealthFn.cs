
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ClaimsRules.Api.Functions
{
    public class HealthFn
    {
        [Function("health")] 
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteStringAsync("ok");
            return res;
        }
    }
}
