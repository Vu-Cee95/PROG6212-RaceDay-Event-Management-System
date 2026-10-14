using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Services;

public interface ICategoryService
{
    Task<ServiceResult<IReadOnlyList<CategoryDto>>> GetForEventAsync(int eventId);
    Task<ServiceResult<CategoryDto>> CreateAsync(int eventId, CategoryRequest request);
    Task<ServiceResult<CategoryDto>> UpdateAsync(int categoryId, CategoryRequest request);
    Task<ServiceResult<object>> DeleteAsync(int categoryId);
}

// Age/distance categories belong to an Event. Anyone may view them; only the Organiser who owns the Event may create, update or delete them.
public class CategoryService : ICategoryService
{
    private readonly RaceDayDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CategoryService(RaceDayDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<IReadOnlyList<CategoryDto>>> GetForEventAsync(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return ServiceResult<IReadOnlyList<CategoryDto>>.NotFound($"Event {eventId} was not found.");
        }

        var list = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryId)
            .Select(c => ToDto(c))
            .ToListAsync();

        return ServiceResult<IReadOnlyList<CategoryDto>>.Ok(list);
    }

    public async Task<ServiceResult<CategoryDto>> CreateAsync(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null)
        {
            return ServiceResult<CategoryDto>.NotFound($"Event {eventId} was not found.");
        }

        if (ev.OrganiserId != _currentUser.UserId)
        {
            return ServiceResult<CategoryDto>.Forbidden("You can only add categories to events that you created.");
        }

        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.CategoryName == name))
        {
            return ServiceResult<CategoryDto>.BadRequest($"A category named '{name}' already exists for this event.");
        }

        var category = new Category
        {
            EventId = eventId,
            CategoryName = name,
            Description = request.Description?.Trim()
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return ServiceResult<CategoryDto>.Created(ToDto(category));
    }

    public async Task<ServiceResult<CategoryDto>> UpdateAsync(int categoryId, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null)
        {
            return ServiceResult<CategoryDto>.NotFound($"Category {categoryId} was not found.");
        }

        if (category.Event.OrganiserId != _currentUser.UserId)
        {
            return ServiceResult<CategoryDto>.Forbidden("You can only update categories of events that you created.");
        }

        category.CategoryName = request.CategoryName.Trim();
        category.Description = request.Description?.Trim();
        await _db.SaveChangesAsync();

        return ServiceResult<CategoryDto>.Ok(ToDto(category));
    }

    public async Task<ServiceResult<object>> DeleteAsync(int categoryId)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null)
        {
            return ServiceResult<object>.NotFound($"Category {categoryId} was not found.");
        }

        if (category.Event.OrganiserId != _currentUser.UserId)
        {
            return ServiceResult<object>.Forbidden("You can only delete categories of events that you created.");
        }

        if (await _db.Enrolments.AnyAsync(en => en.CategoryId == categoryId))
        {
            return ServiceResult<object>.Conflict("Participants have enrolled in this category; it cannot be deleted.");
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return ServiceResult<object>.NoContent();
    }

    private static CategoryDto ToDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        Description = c.Description
    };
}
