
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ClaimsRules.Api.Models;
using ClaimsRules.Api.Mapping;
using ClaimsRules.Api.Services;

namespace ClaimsRules.Api.Functions
{
    public class CsvIngestFn
    {
        private readonly FhirClient _fhir;
        private readonly CosmosAudit _audit;
        private readonly EventPublisher _events;
        public CsvIngestFn(FhirClient fhir, CosmosAudit audit, EventPublisher events)
        {
            _fhir = fhir; _audit = audit; _events = events;
        }

        [Function("csv-ingest")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, FunctionContext ctx)
        {
            var log = ctx.GetLogger<CsvIngestFn>();
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var lines = body.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            int success = 0, failure = 0;
            foreach (var line in lines.Skip(1)) // skip header
            {
                var cols = line.Trim().Split(',');
                if (cols.Length < 9) { failure++; continue; }
                var csv = new ClaimCsv(cols[0], cols[1], cols[2], cols[3], cols[4], cols[5], cols[6], cols[7], cols[8]);
                var claim = CsvToFhirMapper.ToClaim(csv);
                var resp = await _fhir.PostResourceAsync(claim);
                var auditDoc = new { id = csv.ClaimId, type = "csv", statusCode = (int)resp.StatusCode, member = csv.MemberId, provider = csv.ProviderId };
                await _audit.WriteAsync(auditDoc);
                if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 300) success++; else failure++;
            }
            await _events.SendAsync("claims.csv.processed", new { success, failure });
            var res = req.CreateResponse(HttpStatusCode.OK);
            await res.WriteStringAsync(JsonConvert.SerializeObject(new { success, failure }));
            return res;
        }
    }
}
