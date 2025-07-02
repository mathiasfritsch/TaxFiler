using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;


namespace TaxFiler.Service.LlamaIndex;

public class LlamaIndexService : ILlamaIndexService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _agentId;

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
    };

    public LlamaIndexService(IConfiguration config)
    {
        _httpClient = new HttpClient();
        var llamaConfig = config.GetSection("LlamaParse");
        _apiKey = llamaConfig["ApiKey"] ?? throw new Exception("LlamaParse:ApiKey not configured");
        _agentId = llamaConfig["AgentId"] ?? throw new Exception("LlamaParse:AgentId not configured");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<LlamaIndexJobResultResponse> UploadFileAndCreateJobAsync(byte[] bytes, string fileName)
    {
        // 1. Upload file
        var uploadResponse = await UploadFileAsync(bytes,fileName);
        // 2. Create job
        var job = await CreateJobAsync(uploadResponse.id);
        // 3. Poll job until completion or failure, and get result if successful
        LlamaIndexJobResultResponse? result = await PollExtractionJobAsync(job.id);
        return result;
    }

    private async Task<LlamaIndexUploadFileResponse> UploadFileAsync(byte[] bytes, string fileName)
    {
        using var form = new MultipartFormDataContent();
        using var fileStream = new MemoryStream(bytes); 
        var fileContent = new StreamContent(fileStream);
        
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "upload_file", fileName);

        var response = await _httpClient.PostAsync("https://api.cloud.llamaindex.ai/api/v1/files", form);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var uploadResponse = JsonSerializer.Deserialize<LlamaIndexUploadFileResponse>(json, _jsonOptions);
        if (uploadResponse == null || string.IsNullOrEmpty(uploadResponse.id))
            throw new Exception("File ID not returned from LlamaIndex");
        return uploadResponse;
    }

    private async Task<LlamaIndexExtractionJobCreationResponse> CreateJobAsync(string fileId)
    {
        var payload = new
        {
            file_id = fileId,
            extraction_agent_id = _agentId
        };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("https://api.cloud.llamaindex.ai/api/v1/extraction/jobs", content);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var jobResponse = JsonSerializer.Deserialize<LlamaIndexExtractionJobCreationResponse>(json, _jsonOptions);
        if (jobResponse == null || string.IsNullOrEmpty(jobResponse.id))
            throw new Exception("Job ID not returned from LlamaIndex");
        return jobResponse;
    }

    public async Task<LlamaIndexExtractionJobStatusResponse> GetExtractionJobAsync(string jobId)
    {
        var response = await _httpClient.GetAsync($"https://api.cloud.llamaindex.ai/api/v1/extraction/jobs/{jobId}");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var jobResponse = JsonSerializer.Deserialize<LlamaIndexExtractionJobStatusResponse>(json, _jsonOptions);
        if (jobResponse == null || string.IsNullOrEmpty(jobResponse.id))
            throw new Exception("Job not found or invalid response from LlamaIndex");
        return jobResponse;
    }

    public async Task<LlamaIndexJobResultResponse?> PollExtractionJobAsync(string jobId, int intervalSeconds = 5,
        int timeoutSeconds = 300)
    {
        var start = DateTime.UtcNow;
        while (true)
        {
            var job = await GetExtractionJobAsync(jobId);
            if (job.status == nameof(JobStatus.SUCCESS))
            {
                var result = await GetExtractionJobResultAsync(jobId);
                return result;
            }

            if (job.status == nameof(JobStatus.ERROR) || job.status == nameof(JobStatus.CANCELLED))
            {
                throw new Exception($"Job failed with status: {job.status}");
            }

            if ((DateTime.UtcNow - start).TotalSeconds > timeoutSeconds)
                throw new TimeoutException($"Polling timed out after {timeoutSeconds} seconds.");
            await Task.Delay(intervalSeconds * 1000);
        }
    }

    public async Task<LlamaIndexJobResultResponse?> GetExtractionJobResultAsync(string jobId)
    {
        var response =
            await _httpClient.GetAsync($"https://api.cloud.llamaindex.ai/api/v1/extraction/jobs/{jobId}/result");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<LlamaIndexJobResultResponse>(json, _jsonOptions);
        return result;
    }
}