using Dashboards_reports.CollectionTracker.Domain;
using Dashboards_reports.CollectionTracker.Dtos;
using Dashboards_reports.CollectionTracker.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Dashboards_reports.CollectionTracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StageBucketsController(IClientRepository repository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StageBucketDto>>> GetStageBuckets(CancellationToken cancellationToken)
    {
        var buckets = await repository.GetStageBucketsAsync(cancellationToken);
        var allStages = await repository.GetAllBucketStagesAsync(cancellationToken);
        var stagesByBucket = allStages.GroupBy(s => s.BucketId)
            .ToDictionary(g => g.Key, g => g.OrderBy(s => s.SortOrder).Select(s => s.StageName).ToList());

        var result = buckets.Select(b => ToDto(b, stagesByBucket.GetValueOrDefault(b.Id, []))).ToList();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StageBucketDto>> GetStageBucketById(int id, CancellationToken cancellationToken)
    {
        var bucket = await repository.GetStageBucketByIdAsync(id, cancellationToken);
        if (bucket is null || !bucket.IsActive)
            return NotFound(new { message = $"Stage bucket {id} was not found." });

        var stages = await repository.GetBucketStagesAsync(id, cancellationToken);
        return Ok(ToDto(bucket, stages.Select(s => s.StageName).ToList()));
    }

    [HttpPost]
    public async Task<ActionResult<StageBucketDto>> CreateStageBucket(
        [FromBody] CreateStageBucketRequest request,
        CancellationToken cancellationToken)
    {
        var key = request.Key?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(key))
            return BadRequest(new { message = "Bucket key is required." });

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Bucket name is required." });

        var appliesToJson = request.AppliesTo is { Count: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(request.AppliesTo)
            : null;

        var id = await repository.CreateStageBucketAsync(key, name, appliesToJson, cancellationToken);
        if (id == 0)
            return Conflict(new { message = "Bucket key already exists." });

        var created = await repository.GetStageBucketByIdAsync(id, cancellationToken);
        if (created is null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Bucket created but reload failed." });

        return CreatedAtAction(nameof(GetStageBucketById), new { id }, ToDto(created, []));
    }

    [HttpPost("{id:int}/update")]
    public async Task<ActionResult<StageBucketDto>> UpdateStageBucket(
        int id,
        [FromBody] UpdateStageBucketRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetStageBucketByIdAsync(id, cancellationToken);
        if (existing is null || !existing.IsActive)
            return NotFound(new { message = $"Stage bucket {id} was not found." });

        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Bucket name is required." });

        var appliesToJson = request.AppliesTo is { Count: > 0 }
            ? System.Text.Json.JsonSerializer.Serialize(request.AppliesTo)
            : null;

        var updated = await repository.UpdateStageBucketAsync(id, name, appliesToJson, cancellationToken);
        if (!updated)
            return NotFound(new { message = $"Stage bucket {id} was not found." });

        var refreshed = await repository.GetStageBucketByIdAsync(id, cancellationToken);
        var stages = await repository.GetBucketStagesAsync(id, cancellationToken);
        return Ok(ToDto(refreshed!, stages.Select(s => s.StageName).ToList()));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteStageBucket(int id, CancellationToken cancellationToken)
    {
        var existing = await repository.GetStageBucketByIdAsync(id, cancellationToken);
        if (existing is null || !existing.IsActive)
            return NotFound(new { message = $"Stage bucket {id} was not found." });

        var deleted = await repository.DeleteStageBucketAsync(id, cancellationToken);
        if (!deleted)
            return NotFound(new { message = $"Stage bucket {id} was not found." });

        return NoContent();
    }

    [HttpPost("{id:int}/stages")]
    public async Task<ActionResult<StageBucketDto>> SetBucketStages(
        int id,
        [FromBody] SetBucketStagesRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await repository.GetStageBucketByIdAsync(id, cancellationToken);
        if (existing is null || !existing.IsActive)
            return NotFound(new { message = $"Stage bucket {id} was not found." });

        await repository.SetBucketStagesAsync(id, request.Stages, cancellationToken);

        var refreshed = await repository.GetStageBucketByIdAsync(id, cancellationToken);
        var stages = await repository.GetBucketStagesAsync(id, cancellationToken);
        return Ok(ToDto(refreshed!, stages.Select(s => s.StageName).ToList()));
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderBuckets(
        [FromBody] ReorderBucketsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.BucketIds.Count == 0)
            return BadRequest(new { message = "BucketIds list is required." });

        await repository.ReorderBucketsAsync(request.BucketIds, cancellationToken);
        return NoContent();
    }

    private static StageBucketDto ToDto(StageBucketDefinition bucket, List<string> stages)
    {
        List<string> appliesTo = [];
        if (!string.IsNullOrWhiteSpace(bucket.AppliesTo))
        {
            try { appliesTo = System.Text.Json.JsonSerializer.Deserialize<List<string>>(bucket.AppliesTo) ?? []; }
            catch { /* ignore malformed JSON */ }
        }

        return new StageBucketDto
        {
            Id = bucket.Id,
            Key = bucket.Key,
            Name = bucket.Name,
            SortOrder = bucket.SortOrder,
            IsActive = bucket.IsActive,
            AppliesTo = appliesTo,
            Stages = stages,
        };
    }
}
