using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClaimsRules.Api.Models;

namespace ClaimsRules.Api.Services;

/// <summary>
/// AI-powered Master Data Management matching service using Azure OpenAI
/// Uses Entra ID (Azure AD) authentication - no API key required
/// Cost: ~$0.0004 per match evaluation (gpt-35-turbo)
/// </summary>
public class AIMatchingService
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAIConfig _config;
    private readonly TokenCredential _credential;
    private readonly CosmosAudit _cosmosAudit;
    private readonly ILogger<AIMatchingService> _logger;

    // Cost per 1K tokens (gpt-35-turbo pricing as of Jan 2026)
    private const decimal INPUT_COST_PER_1K = 0.0005m;
    private const decimal OUTPUT_COST_PER_1K = 0.0015m;

    public AIMatchingService(
        HttpClient httpClient,
        IConfiguration configuration,
        CosmosAudit cosmosAudit,
        ILogger<AIMatchingService> logger)
    {
        _httpClient = httpClient;
        _cosmosAudit = cosmosAudit;
        _logger = logger;
        _credential = new DefaultAzureCredential();

        // Load Azure OpenAI configuration
        _config = new AzureOpenAIConfig
        {
            Endpoint = configuration["AzureOpenAI:Endpoint"] 
                ?? throw new InvalidOperationException("AzureOpenAI:Endpoint not configured"),
            DeploymentName = configuration["AzureOpenAI:DeploymentName"] ?? "gpt-35-turbo",
            MaxTokens = int.Parse(configuration["AzureOpenAI:MaxTokens"] ?? "500"),
            Temperature = double.Parse(configuration["AzureOpenAI:Temperature"] ?? "0.3"),
            ApiVersion = configuration["AzureOpenAI:ApiVersion"] ?? "2024-02-15-preview"
        };
    }

    /// <summary>
    /// Evaluate patient match confidence using OpenAI GPT-3.5-turbo
    /// Returns confidence score (0-100) and recommended decision
    /// </summary>
    public async Task<AIMatchResponse> EvaluateMatchAsync(AIMatchRequest request)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            // Build prompt for OpenAI
            var prompt = BuildMatchEvaluationPrompt(request.Record1, request.Record2);

            // Call OpenAI API
            var openAIResponse = await CallOpenAIAsync(prompt);

            // Parse JSON response
            var evaluation = ParseOpenAIResponse(openAIResponse);

            // Determine recommended decision based on confidence
            var decision = DetermineDecision(evaluation.confidence);

            // Calculate cost
            var cost = CalculateCost(openAIResponse.usage.prompt_tokens, openAIResponse.usage.completion_tokens);

            var response = new AIMatchResponse
            {
                MatchId = request.MatchId ?? Guid.NewGuid().ToString(),
                Confidence = evaluation.confidence,
                IsMatch = evaluation.isMatch,
                Reasoning = evaluation.reasoning,
                MatchingFactors = evaluation.matchingFactors,
                ConcerningFactors = evaluation.concerningFactors,
                RecommendedDecision = decision,
                EstimatedCost = cost,
                TokensUsed = openAIResponse.usage.total_tokens
            };

            // Audit to Cosmos DB
            await _cosmosAudit.WriteAuditAsync("ai-match-evaluation", new
            {
                response.MatchId,
                Record1Id = request.Record1.PatientId,
                Record2Id = request.Record2.PatientId,
                response.Confidence,
                response.IsMatch,
                response.RecommendedDecision,
                response.EstimatedCost,
                response.TokensUsed,
                DurationMs = (DateTime.UtcNow - startTime).TotalMilliseconds
            });

            _logger.LogInformation(
                "AI Match Evaluation: {MatchId} | Confidence: {Confidence}% | Decision: {Decision} | Cost: ${Cost:F4}",
                response.MatchId, response.Confidence, response.RecommendedDecision, response.EstimatedCost);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating match with AI for {MatchId}", request.MatchId);
            throw;
        }
    }

    /// <summary>
    /// Learn from human reviewer decision to improve model accuracy
    /// Stores feedback in Cosmos DB for future model retraining
    /// </summary>
    public async Task<AILearningResponse> LearnFromReviewerAsync(AILearningRequest request)
    {
        try
        {
            // Determine if AI was correct
            var correct = request.AIRecommendation == request.HumanDecision;

            // Store feedback in Cosmos DB
            await _cosmosAudit.WriteAuditAsync("ai-learning-feedback", new
            {
                request.MatchId,
                request.AIConfidence,
                request.AIRecommendation,
                request.HumanDecision,
                request.HumanReviewer,
                request.ReviewedAt,
                request.ReviewTimeSeconds,
                request.ReviewerNotes,
                Correct = correct,
                CreatedAt = DateTime.UtcNow
            });

            // Calculate updated metrics
            var metrics = await CalculateModelMetricsAsync();

            var response = new AILearningResponse
            {
                MatchId = request.MatchId,
                Correct = correct,
                ImprovementSuggestion = GenerateImprovementSuggestion(request, correct),
                UpdatedMetrics = metrics
            };

            _logger.LogInformation(
                "AI Learning Feedback: {MatchId} | Correct: {Correct} | Accuracy: {Accuracy:F2}%",
                request.MatchId, correct, metrics.Accuracy);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing learning feedback for {MatchId}", request.MatchId);
            throw;
        }
    }

    /// <summary>
    /// Get current AI model performance metrics
    /// </summary>
    public async Task<AIModelMetrics> GetModelMetricsAsync()
    {
        return await CalculateModelMetricsAsync();
    }

    /// <summary>
    /// Build OpenAI prompt for patient match evaluation
    /// </summary>
    private string BuildMatchEvaluationPrompt(PatientMatchCandidate record1, PatientMatchCandidate record2)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert healthcare Master Data Management (MDM) system.");
        sb.AppendLine("Evaluate if these two patient records refer to the same person.");
        sb.AppendLine();
        sb.AppendLine("Record 1:");
        sb.AppendLine($"- Name: {record1.Name}");
        sb.AppendLine($"- DOB: {record1.DateOfBirth}");
        if (!string.IsNullOrEmpty(record1.SSN))
            sb.AppendLine($"- SSN (last 4): {record1.SSN}");
        if (!string.IsNullOrEmpty(record1.Address))
            sb.AppendLine($"- Address: {record1.Address}");
        if (!string.IsNullOrEmpty(record1.Phone))
            sb.AppendLine($"- Phone: {record1.Phone}");
        if (!string.IsNullOrEmpty(record1.Gender))
            sb.AppendLine($"- Gender: {record1.Gender}");
        sb.AppendLine();
        sb.AppendLine("Record 2:");
        sb.AppendLine($"- Name: {record2.Name}");
        sb.AppendLine($"- DOB: {record2.DateOfBirth}");
        if (!string.IsNullOrEmpty(record2.SSN))
            sb.AppendLine($"- SSN (last 4): {record2.SSN}");
        if (!string.IsNullOrEmpty(record2.Address))
            sb.AppendLine($"- Address: {record2.Address}");
        if (!string.IsNullOrEmpty(record2.Phone))
            sb.AppendLine($"- Phone: {record2.Phone}");
        if (!string.IsNullOrEmpty(record2.Gender))
            sb.AppendLine($"- Gender: {record2.Gender}");
        sb.AppendLine();
        sb.AppendLine("Consider:");
        sb.AppendLine("- Name variations (nicknames, typos, maiden names, middle names)");
        sb.AppendLine("- Date typos vs. actual different people (transposed digits common)");
        sb.AppendLine("- Address moves (same person, new location is normal)");
        sb.AppendLine("- Phone number changes (common, not decisive)");
        sb.AppendLine("- SSN is most reliable identifier when available");
        sb.AppendLine();
        sb.AppendLine("Respond ONLY with valid JSON in this exact format:");
        sb.AppendLine("{");
        sb.AppendLine("  \"confidence\": 0-100,");
        sb.AppendLine("  \"isMatch\": true or false,");
        sb.AppendLine("  \"reasoning\": \"brief explanation\",");
        sb.AppendLine("  \"matchingFactors\": [\"factor1\", \"factor2\"],");
        sb.AppendLine("  \"concerningFactors\": [\"concern1\"]");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Call Azure OpenAI Chat Completion API with Entra ID authentication
    /// </summary>
    private async Task<OpenAIChatResponse> CallOpenAIAsync(string userPrompt)
    {
        var request = new OpenAIChatRequest
        {
            model = _config.DeploymentName,
            messages = new List<OpenAIMessage>
            {
                new OpenAIMessage
                {
                    role = "system",
                    content = "You are a healthcare data quality expert specializing in Master Data Management. " +
                              "You provide accurate, confident assessments of whether patient records match. " +
                              "Always respond with valid JSON only, no additional text."
                },
                new OpenAIMessage
                {
                    role = "user",
                    content = userPrompt
                }
            },
            max_tokens = _config.MaxTokens,
            temperature = _config.Temperature
        };

        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        // Get Entra ID token for Azure OpenAI
        var tokenRequestContext = new TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" });
        var token = await _credential.GetTokenAsync(tokenRequestContext, default);

        // Build Azure OpenAI endpoint URL
        var url = $"{_config.Endpoint}/openai/deployments/{_config.DeploymentName}/chat/completions?api-version={_config.ApiVersion}";

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        httpRequest.Headers.Add("Authorization", $"Bearer {token.Token}");

        var httpResponse = await _httpClient.SendAsync(httpRequest);
        httpResponse.EnsureSuccessStatusCode();

        var responseJson = await httpResponse.Content.ReadAsStringAsync();
        var openAIResponse = JsonSerializer.Deserialize<OpenAIChatResponse>(responseJson)
            ?? throw new InvalidOperationException("Failed to deserialize Azure OpenAI response");

        return openAIResponse;
    }

    /// <summary>
    /// Parse OpenAI JSON response into structured model
    /// </summary>
    private OpenAIMatchEvaluation ParseOpenAIResponse(OpenAIChatResponse response)
    {
        var content = response.choices[0].message.content;

        // Remove markdown code blocks if present
        content = content.Trim();
        if (content.StartsWith("```json"))
            content = content.Substring(7);
        if (content.StartsWith("```"))
            content = content.Substring(3);
        if (content.EndsWith("```"))
            content = content.Substring(0, content.Length - 3);
        content = content.Trim();

        var evaluation = JsonSerializer.Deserialize<OpenAIMatchEvaluation>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Failed to parse OpenAI match evaluation");

        // Clamp confidence to 0-100
        evaluation.confidence = Math.Clamp(evaluation.confidence, 0, 100);

        return evaluation;
    }

    /// <summary>
    /// Determine recommended decision based on confidence threshold
    /// </summary>
    private MatchDecision DetermineDecision(int confidence)
    {
        return confidence switch
        {
            >= 95 => MatchDecision.AutoApprove,
            >= 80 => MatchDecision.PriorityReview,
            >= 50 => MatchDecision.StandardReview,
            _ => MatchDecision.AutoReject
        };
    }

    /// <summary>
    /// Calculate OpenAI API cost based on token usage
    /// </summary>
    private decimal CalculateCost(int inputTokens, int outputTokens)
    {
        var inputCost = (inputTokens / 1000m) * INPUT_COST_PER_1K;
        var outputCost = (outputTokens / 1000m) * OUTPUT_COST_PER_1K;
        return inputCost + outputCost;
    }

    /// <summary>
    /// Calculate AI model performance metrics from Cosmos DB feedback
    /// </summary>
    private async Task<AIModelMetrics> CalculateModelMetricsAsync()
    {
        // Query all learning feedback from Cosmos DB
        var feedbackItems = await _cosmosAudit.QueryAuditAsync(
            "SELECT * FROM c WHERE c.type = 'ai-learning-feedback' ORDER BY c.createdAt DESC",
            maxItems: 1000);

        if (!feedbackItems.Any())
        {
            return new AIModelMetrics
            {
                LastUpdated = DateTime.UtcNow
            };
        }

        var total = feedbackItems.Count;
        var correct = feedbackItems.Count(f => (bool)f.GetProperty("correct"));
        
        // Calculate confusion matrix
        var truePositives = feedbackItems.Count(f => 
            (bool)f.GetProperty("aiRecommendation") && (bool)f.GetProperty("humanDecision"));
        var trueNegatives = feedbackItems.Count(f => 
            !(bool)f.GetProperty("aiRecommendation") && !(bool)f.GetProperty("humanDecision"));
        var falsePositives = feedbackItems.Count(f => 
            (bool)f.GetProperty("aiRecommendation") && !(bool)f.GetProperty("humanDecision"));
        var falseNegatives = feedbackItems.Count(f => 
            !(bool)f.GetProperty("aiRecommendation") && (bool)f.GetProperty("humanDecision"));

        var accuracy = (double)correct / total * 100;
        var precision = truePositives + falsePositives > 0 
            ? (double)truePositives / (truePositives + falsePositives) * 100 
            : 0;
        var recall = truePositives + falseNegatives > 0 
            ? (double)truePositives / (truePositives + falseNegatives) * 100 
            : 0;

        // Calculate auto-approve rate (95%+ confidence)
        var evaluations = await _cosmosAudit.QueryAuditAsync(
            "SELECT * FROM c WHERE c.type = 'ai-match-evaluation' ORDER BY c.createdAt DESC",
            maxItems: 1000);
        
        var autoApproveCount = evaluations.Count(e => 
            e.GetProperty("confidence").GetInt32() >= 95);
        var autoApproveRate = evaluations.Any() 
            ? (double)autoApproveCount / evaluations.Count * 100 
            : 0;

        // Calculate total cost
        var totalCost = evaluations.Sum(e => 
            e.GetProperty("estimatedCost").GetDecimal());

        return new AIModelMetrics
        {
            TotalEvaluations = evaluations.Count,
            CorrectPredictions = correct,
            Accuracy = accuracy,
            Precision = precision,
            Recall = recall,
            AutoApproveRate = autoApproveRate,
            TruePositives = truePositives,
            TrueNegatives = trueNegatives,
            FalsePositives = falsePositives,
            FalseNegatives = falseNegatives,
            TotalCost = totalCost,
            LastUpdated = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Generate improvement suggestion based on feedback
    /// </summary>
    private string? GenerateImprovementSuggestion(AILearningRequest request, bool correct)
    {
        if (correct)
            return null;

        // AI was wrong - provide suggestion
        if (request.AIRecommendation && !request.HumanDecision)
        {
            return $"False positive at {request.AIConfidence}% confidence. " +
                   "Consider increasing threshold or weighting factors differently.";
        }
        else if (!request.AIRecommendation && request.HumanDecision)
        {
            return $"False negative at {request.AIConfidence}% confidence. " +
                   "Consider lowering threshold or recognizing more name variations.";
        }

        return null;
    }
}
