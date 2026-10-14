using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Filters;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers;

// Age or distance categories for an event (e.g. Under 20, Senior, 10km, 21km).
[ApiController]
[Tags("Categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories)
    {
        _categories = categories;
    }

    // List the categories of an event.
    [HttpGet("api/events/{eventId:int}/categories")]
    [SwaggerOperation(Summary = "Retrieve all categories for an event",
        Description = "Public. Both roles can view the categories available for an event. 404 if the event does not exist.")]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var result = await _categories.GetForEventAsync(eventId);
        return result.ToActionResult(this);
    }

    // Add a category to an event.
    [HttpPost("api/events/{eventId:int}/categories")]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Add a new category to an event",
        Description = "Organiser only; the caller must own the event. Body: { categoryName, description }.")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CategoryRequest request)
    {
        var result = await _categories.CreateAsync(eventId, request);
        return result.ToActionResult(this);
    }

    // Update a category.
    [HttpPut("api/categories/{categoryId:int}")]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Update a category",
        Description = "Organiser only; the caller must own the event the category belongs to. Body: { categoryName, description }.")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int categoryId, [FromBody] CategoryRequest request)
    {
        var result = await _categories.UpdateAsync(categoryId, request);
        return result.ToActionResult(this);
    }

    // Delete a category.
    [HttpDelete("api/categories/{categoryId:int}")]
    [RequireRole(UserRole.Organiser)]
    [SwaggerOperation(Summary = "Delete a category",
        Description = "Organiser only; the caller must own the event. Returns 409 if participants have enrolled in the category.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int categoryId)
    {
        var result = await _categories.DeleteAsync(categoryId);
        return result.ToActionResult(this);
    }
}
