
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace ClaimsRules.Api.Functions
{
    public class EdiIngestFn
    {
        [Function("edi-ingest")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext ctx)
        {
            var log = ctx.GetLogger<EdiIngestFn>();
            // TODO: parse X12 837 payload and map to FHIR Claim.
            var res = req.CreateResponse(HttpStatusCode.NotImplemented);
            await res.WriteStringAsync("EDI 837 parsing stub — replace with mapper.");
            return res;
        }
    }
}
