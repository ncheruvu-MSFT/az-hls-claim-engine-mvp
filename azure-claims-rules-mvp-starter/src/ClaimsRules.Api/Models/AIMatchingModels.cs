namespace ClaimsRules.Api.Models;

/// <summary>
/// Request model for AI-powered match confidence scoring
/// </summary>
public class AIMatchRequest
{
    public required PatientMatchCandidate Record1 { get; set; }
    public required PatientMatchCandidate Record2 { get; set; }
    public string? MatchId { get; set; }
}

/// <summary>
/// Patient match candidate for AI evaluation
/// </summary>
public class PatientMatchCandidate
{
    public required string PatientId { get; set; }
    public required string Name { get; set; }
    public required string DateOfBirth { get; set; }
    public string? SSN { get; set; } // Last 4 digits only
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
}

/// <summary>
/// AI confidence score response from OpenAI
/// </summary>
public class AIMatchResponse
{
    public required string MatchId { get; set; }
    public int Confidence { get; set; } // 0-100
    public bool IsMatch { get; set; }
    public required string Reasoning { get; set; }
    public List<string> MatchingFactors { get; set; } = new();
    public List<string> ConcerningFactors { get; set; } = new();
    public MatchDecision RecommendedDecision { get; set; }
    public decimal EstimatedCost { get; set; } // OpenAI API cost
    public int TokensUsed { get; set; }
}

/// <summary>
/// AI recommended decision based on confidence threshold
/// </summary>
public enum MatchDecision
{
    AutoApprove,      // 95-100% confidence
    PriorityReview,   // 80-94% confidence
    StandardReview,   // 50-79% confidence
    AutoReject        // 0-49% confidence
}

/// <summary>
/// Request to update AI model with human reviewer feedback
/// </summary>
public class AILearningRequest
{
    public required string MatchId { get; set; }
    public int AIConfidence { get; set; }
    public bool AIRecommendation { get; set; }
    public bool HumanDecision { get; set; } // true = approved, false = rejected
    public required string HumanReviewer { get; set; }
    public DateTime ReviewedAt { get; set; }
    public int ReviewTimeSeconds { get; set; }
    public string? ReviewerNotes { get; set; }
}

/// <summary>
/// AI learning feedback response
/// </summary>
public class AILearningResponse
{
    public required string MatchId { get; set; }
    public bool Correct { get; set; } // Did AI match human decision?
    public string? ImprovementSuggestion { get; set; }
    public AIModelMetrics UpdatedMetrics { get; set; } = new();
}

/// <summary>
/// AI model performance metrics
/// </summary>
public class AIModelMetrics
{
    public int TotalEvaluations { get; set; }
    public int CorrectPredictions { get; set; }
    public double Accuracy { get; set; } // % of AI decisions matching human
    public double Precision { get; set; } // % of AI "match" that were correct
    public double Recall { get; set; } // % of actual matches AI identified
    public double AutoApproveRate { get; set; } // % with 95%+ confidence
    public int TruePositives { get; set; }
    public int TrueNegatives { get; set; }
    public int FalsePositives { get; set; }
    public int FalseNegatives { get; set; }
    public decimal TotalCost { get; set; } // Total OpenAI API cost
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Azure OpenAI API configuration (uses Entra ID auth)
/// </summary>
public class AzureOpenAIConfig
{
    public required string Endpoint { get; set; } // e.g., https://openaiclaimstest001.openai.azure.com
    public required string DeploymentName { get; set; } // e.g., gpt-35-turbo
    public int MaxTokens { get; set; } = 500;
    public double Temperature { get; set; } = 0.3; // Lower = more deterministic
    public string ApiVersion { get; set; } = "2024-02-15-preview";
}

/// <summary>
/// OpenAI API configuration (legacy, for comparison)
/// </summary>
public class OpenAIConfig
{
    public required string ApiKey { get; set; }
    public string Model { get; set; } = "gpt-3.5-turbo-0125"; // Cheapest model
    public int MaxTokens { get; set; } = 500;
    public double Temperature { get; set; } = 0.3; // Lower = more deterministic
    public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
}

/// <summary>
/// OpenAI chat completion request (simplified)
/// </summary>
public class OpenAIChatRequest
{
    public required string model { get; set; }
    public required List<OpenAIMessage> messages { get; set; }
    public int max_tokens { get; set; }
    public double temperature { get; set; }
    public string? response_format { get; set; } // "json_object" for structured output
}

/// <summary>
/// OpenAI message structure
/// </summary>
public class OpenAIMessage
{
    public required string role { get; set; } // "system", "user", "assistant"
    public required string content { get; set; }
}

/// <summary>
/// OpenAI chat completion response (simplified)
/// </summary>
public class OpenAIChatResponse
{
    public required string id { get; set; }
    public required List<OpenAIChoice> choices { get; set; }
    public required OpenAIUsage usage { get; set; }
}

public class OpenAIChoice
{
    public int index { get; set; }
    public required OpenAIMessage message { get; set; }
    public string? finish_reason { get; set; }
}

public class OpenAIUsage
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
}

/// <summary>
/// Parsed JSON response from OpenAI
/// </summary>
public class OpenAIMatchEvaluation
{
    public int confidence { get; set; }
    public bool isMatch { get; set; }
    public required string reasoning { get; set; }
    public List<string> matchingFactors { get; set; } = new();
    public List<string> concerningFactors { get; set; } = new();
}
