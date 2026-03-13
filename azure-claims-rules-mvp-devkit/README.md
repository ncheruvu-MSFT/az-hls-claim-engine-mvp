
# DevKit — Synthetic-data (no Private Endpoints, no Defender) 

This kit lets you run the **Claims Rules MVP** locally with **synthetic data only**. It removes production controls (Private Endpoints, Defender) for faster iteration. **Do not use PHI.**

## Components
- mock-fhir/ — .NET 8 minimal API that accepts `POST /Claim` and returns a stub response.
- data/synthetic/ — CSV + tiny EDI sample + Python generator.
- patches/ — `local.settings.json` pointing to the mock FHIR.

## Quick start
1. Install .NET 8 SDK and Azure Functions Core Tools.
2. In `mock-fhir/`: `dotnet run` (starts on `http://localhost:7072`).
3. In your API project: apply `patches/local.settings.json` to use the mock endpoint.
4. Generate synthetic CSV: `python data/synthetic/generate_synthetic_claims.py`.
5. Post CSV to the API: `curl -X POST --data-binary @data/synthetic/claims_50.csv http://localhost:7071/api/csv-ingest`.

Security note: This dev kit is **non-compliant** by design. For HIPAA/Zero Trust guidance, see internal policies (linked in the annotations of this message).
