using Microsoft.Extensions.Configuration;
using TaxFiler.Service.LlamaClient;
using Refit;

namespace TaxFiler.Service.LlamaIndex;

public class LlamaIndexService : ILlamaIndexService
{
    private readonly ILlamaApiClient _llamaApiClient;
    private readonly string _agentId;

    public LlamaIndexService(ILlamaApiClient llamaApiClient, IConfiguration config)
    {
        _llamaApiClient = llamaApiClient;
        var llamaConfig = config.GetSection("LlamaParse");
        _agentId = llamaConfig["AgentId"] ?? throw new Exception("LlamaParse:AgentId not configured");
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
        using var fileStream = new MemoryStream(bytes);
        var filePart = new StreamPart(fileStream, fileName, "application/pdf");
        
        var uploadResponse = await _llamaApiClient.UploadFileAsync(filePart);
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
        
        var jobResponse = await _llamaApiClient.CreateExtractionJobAsync(payload);
        if (jobResponse == null || string.IsNullOrEmpty(jobResponse.id))
            throw new Exception("Job ID not returned from LlamaIndex");
        return jobResponse;
    }

    public async Task<LlamaIndexExtractionJobStatusResponse> GetExtractionJobAsync(string jobId)
    {
        var jobResponse = await _llamaApiClient.GetExtractionJobAsync(jobId);
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
        var result = await _llamaApiClient.GetExtractionJobResultAsync(jobId);
        return result;
    }
}