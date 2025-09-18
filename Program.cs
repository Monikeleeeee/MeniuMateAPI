using FluentValidation;
using MeniuMate_API.Data;
using MeniuMate_API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using O9d.AspNet.FluentValidation;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ForumDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

var app = builder.Build();

#region Menius

var meniuGroup = app.MapGroup("/api").WithValidationFilter();

//Get all menius
meniuGroup.MapGet("menius", async(ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    return (await dbContext.Menius.ToListAsync(cancellationToken)).Select(meniu =>
        new MeniuDto(meniu.Id, meniu.Name, meniu.Description));
});

//Get one meniu
meniuGroup.MapGet("menius/{meniuId}", async (int meniuId, ForumDbContext dbContext) =>
{
    var meniu = await dbContext.Menius.FirstOrDefaultAsync(m => m.Id == meniuId);
    if (meniu == null)
        return Results.NotFound();

    return Results.Ok(new MeniuDto(meniu.Id, meniu.Name, meniu.Description));
});

//Create meniu
meniuGroup.MapPost("menius", async ([Validate] CreateMeniuDto createMeniuDto, ForumDbContext dbContext) =>
{
    var meniu = new Meniu()
    {
        Name = createMeniuDto.Name,
        Description = createMeniuDto.Description,
        CreationDate = DateTime.UtcNow
    };

    dbContext.Menius.Add(meniu);

    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/menius/{meniu.Id}", 
        new MeniuDto(meniu.Id, meniu.Name, meniu.Description));
});

//Update meniu
meniuGroup.MapPut("menius/{meniuId}", async (int meniuId, [Validate] UpdateMeniuDto updateMeniuDto, ForumDbContext dbContext) =>
{
    var meniu = await dbContext.Menius.FirstOrDefaultAsync(m => m.Id == meniuId);
    if (meniu == null)
        return Results.NotFound();

    meniu.Description = updateMeniuDto.Description;

    dbContext.Update(meniu);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new MeniuDto(meniu.Id, meniu.Name, meniu.Description));
});

//Delete meniu
meniuGroup.MapDelete("menius/{meniuId}", async (int meniuId, ForumDbContext dbContext) =>
{
    var meniu = await dbContext.Menius.FirstOrDefaultAsync(m => m.Id == meniuId);
    if (meniu == null)
        return Results.NotFound();

    dbContext.Remove(meniu);
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});

#endregion Menius

#region Dishes

var dishGroup = app.MapGroup("/api/menius/{meniuId}").WithValidationFilter();

// Get all dishes
dishGroup.MapGet("dishes", async (int meniuId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var meniuExists = await dbContext.Menius.AnyAsync(m => m.Id == meniuId, cancellationToken);
    if (!meniuExists)
        return Results.NotFound();

    var dishes = await dbContext.Dishes
        .Include(d => d.Meniu)
        .Where(d => d.Meniu.Id == meniuId)
        .ToListAsync(cancellationToken);

    return Results.Ok(dishes.Select(d => new DishDto(
        d.Id, d.Name, d.Description, d.Price, d.Ingredients, d.IsAvailable
    )));
});

// Get one dish
dishGroup.MapGet("dishes/{dishId}", async (int meniuId, int dishId, ForumDbContext dbContext) =>
{
    var dish = await dbContext.Dishes
        .Include(d => d.Meniu)
        .FirstOrDefaultAsync(d => d.Id == dishId && d.Meniu.Id == meniuId);

    if (dish == null)
        return Results.NotFound("Dish not found in this meniu");

    return Results.Ok(new DishDto(dish.Id, dish.Name, dish.Description, dish.Price, dish.Ingredients, dish.IsAvailable));
});

// Create dish
dishGroup.MapPost("dishes", async (int meniuId, [Validate] CreateDishDto createDishDto, ForumDbContext dbContext) =>
{
    var meniu = await dbContext.Menius.FindAsync(meniuId);
    if (meniu == null) return Results.NotFound("Meniu not found");

    var dish = new Dish
    {
        Name = createDishDto.Name,
        Description = createDishDto.Description,
        Price = createDishDto.Price,
        Ingredients = createDishDto.Ingredients,
        IsAvailable = createDishDto.IsAvailable,
        CreationDate = DateTime.UtcNow,
        Meniu = meniu
    };

    dbContext.Dishes.Add(dish);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/menius/{meniuId}/dishes/{dish.Id}",
        new DishDto(dish.Id, dish.Name, dish.Description, dish.Price, dish.Ingredients, dish.IsAvailable));
});

// Update dish
dishGroup.MapPut("dishes/{dishId}", async (int meniuId, int dishId, [Validate] UpdateDishDto updateDishDto, ForumDbContext dbContext) =>
{
    var dish = await dbContext.Dishes
        .Include(d => d.Meniu)
        .FirstOrDefaultAsync(d => d.Id == dishId && d.Meniu.Id == meniuId);

    if (dish == null) return Results.NotFound();

    dish.Description = updateDishDto.Description;
    dish.Price = updateDishDto.Price;
    dish.Ingredients = updateDishDto.Ingredients;
    dish.IsAvailable = updateDishDto.IsAvailable;

    dbContext.Update(dish);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new DishDto(dish.Id, dish.Name, dish.Description, dish.Price, dish.Ingredients, dish.IsAvailable));
});

// Delete dish
dishGroup.MapDelete("dishes/{dishId}", async (int meniuId, int dishId, ForumDbContext dbContext) =>
{
    var dish = await dbContext.Dishes
        .Include(d => d.Meniu)
        .FirstOrDefaultAsync(d => d.Id == dishId && d.Meniu.Id == meniuId);

    if (dish == null) return Results.NotFound();

    dbContext.Remove(dish);
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});

#endregion Dishes

#region Comments

var commentGroup = app.MapGroup("/api/menius/{meniuId}/dishes/{dishId}").WithValidationFilter();

// Get all comments
commentGroup.MapGet("comments", async (int meniuId, int dishId, ForumDbContext dbContext, CancellationToken cancellationToken) =>
{
    var meniuExists = await dbContext.Menius.AnyAsync(m => m.Id == meniuId, cancellationToken);
    if (!meniuExists)
        return Results.NotFound("Meniu not found");

    var dishExists = await dbContext.Dishes.AnyAsync(d => d.Id == dishId && d.Meniu.Id == meniuId, cancellationToken);
    if (!dishExists)
        return Results.NotFound("Dish not found in this meniu");

    var comments = await dbContext.Comments
        .Include(c => c.Dish)
        .ThenInclude(d => d.Meniu)
        .Where(c => c.Dish.Id == dishId && c.Dish.Meniu.Id == meniuId)
        .ToListAsync(cancellationToken);

    return Results.Ok(comments.Select(c => new CommentDto(c.Id, c.Content, c.Rating, c.Dish.Id)));
});

// Get one comment
commentGroup.MapGet("comments/{commentId}", async (int meniuId, int dishId, int commentId, ForumDbContext dbContext) =>
{
    var comment = await dbContext.Comments
        .Include(c => c.Dish)
        .ThenInclude(d => d.Meniu)
        .FirstOrDefaultAsync(c => c.Id == commentId
                               && c.Dish.Id == dishId
                               && c.Dish.Meniu.Id == meniuId);

    if (comment == null)
        return Results.NotFound("Comment not found");

    return Results.Ok(new CommentDto(comment.Id, comment.Content, comment.Rating, comment.Dish.Id));
});

// Create comment
commentGroup.MapPost("comments", async (int meniuId, int dishId, [Validate] CreateCommentDto createCommentDto, ForumDbContext dbContext) =>
{
    var dish = await dbContext.Dishes.Include(d => d.Meniu)
        .FirstOrDefaultAsync(d => d.Id == dishId && d.Meniu.Id == meniuId);

    if (dish == null) return Results.NotFound("Dish not found");

    var comment = new Comment
    {
        Content = createCommentDto.Content,
        Rating = createCommentDto.Rating,
        CreationDate = DateTime.UtcNow,
        Dish = dish
    };

    dbContext.Comments.Add(comment);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/api/menius/{meniuId}/dishes/{dishId}/comments/{comment.Id}",
        new CommentDto(comment.Id, comment.Content, comment.Rating, dish.Id));
});

// Update comment
commentGroup.MapPut("comments/{commentId}", async (int meniuId, int dishId, int commentId, [Validate] UpdateCommentDto updateCommentDto, ForumDbContext dbContext) =>
{
    var comment = await dbContext.Comments
        .Include(c => c.Dish)
        .ThenInclude(d => d.Meniu)
        .FirstOrDefaultAsync(c => c.Id == commentId
                               && c.Dish.Id == dishId
                               && c.Dish.Meniu.Id == meniuId);

    if (comment == null)
        return Results.NotFound("Comment not found");

    comment.Content = updateCommentDto.Content;
    comment.Rating = updateCommentDto.Rating;

    await dbContext.SaveChangesAsync();

    return Results.Ok(new CommentDto(comment.Id, comment.Content, comment.Rating, comment.Dish.Id));
});


// Delete comment
commentGroup.MapDelete("comments/{commentId}", async (int meniuId, int dishId, int commentId, ForumDbContext dbContext) =>
{
    var comment = await dbContext.Comments
        .FirstOrDefaultAsync(c => c.Id == commentId && c.Dish.Id == dishId && c.Dish.Meniu.Id == meniuId);

    if (comment == null) return Results.NotFound();

    dbContext.Remove(comment);
    await dbContext.SaveChangesAsync();

    return Results.NoContent();
});

#endregion Comments

app.Run();

public class CreateMeniuDtoValidator : AbstractValidator<CreateMeniuDto>
{
    public CreateMeniuDtoValidator()
    {
        RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min:2, max:100);
        RuleFor(dto => dto.Description).NotEmpty().NotNull().Length(min: 10, max: 300);
    }
}

public class UpdateMeniuDtoValidator : AbstractValidator<UpdateMeniuDto>
{
    public UpdateMeniuDtoValidator()
    {
        RuleFor(dto => dto.Description).NotEmpty().NotNull().Length(min: 10, max: 300);
    }
}

public class CreateDishDtoValidator : AbstractValidator<CreateDishDto>
{
    public CreateDishDtoValidator()
    {
        RuleFor(d => d.Name).NotEmpty().Length(2, 100);
        RuleFor(d => d.Description).NotEmpty().Length(5, 300);
        RuleFor(d => d.Price).GreaterThan(0);
        RuleFor(d => d.Ingredients).NotEmpty().Length(5, 300);
    }
}

public class UpdateDishDtoValidator : AbstractValidator<UpdateDishDto>
{
    public UpdateDishDtoValidator()
    {
        RuleFor(d => d.Description).NotEmpty().Length(10, 300);
        RuleFor(d => d.Price).GreaterThan(0);
        RuleFor(d => d.Ingredients).NotEmpty().Length(5, 300);
    }
}

public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentDtoValidator()
    {
        RuleFor(c => c.Content).NotEmpty().Length(5, 500);
        RuleFor(c => c.Rating).InclusiveBetween(1, 5);
    }
}

public class UpdateCommentDtoValidator : AbstractValidator<UpdateCommentDto>
{
    public UpdateCommentDtoValidator()
    {
        RuleFor(c => c.Content).NotEmpty().Length(5, 500);
        RuleFor(c => c.Rating).InclusiveBetween(1, 5);
    }
}
