using FluentValidation;
using MeniuMate_API.Auth.Model;
using MeniuMate_API.Data;
using MeniuMate_API.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using O9d.AspNet.FluentValidation;
using System.Security.Claims;

namespace MeniuMate_API
{
    public static class DishEndPoints
    {
        public static void AddDishApi(this WebApplication app)
        {
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
            dishGroup.MapPost("dishes", [Authorize(Roles = ForumRoles.Admin)] async (int meniuId, [Validate] CreateDishDto createDishDto, HttpContext httpContext, ForumDbContext dbContext) =>
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
                    Meniu = meniu,
                    UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                };

                dbContext.Dishes.Add(dish);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/menius/{meniuId}/dishes/{dish.Id}",
                    new DishDto(dish.Id, dish.Name, dish.Description, dish.Price, dish.Ingredients, dish.IsAvailable));
            });

            // Update dish
            dishGroup.MapPut("dishes/{dishId}", [Authorize(Roles = ForumRoles.Admin)] async (int meniuId, int dishId, [Validate] UpdateDishDto updateDishDto, ForumDbContext dbContext) =>
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
            dishGroup.MapDelete("dishes/{dishId}", [Authorize(Roles = ForumRoles.Admin)] async (int meniuId, int dishId, ForumDbContext dbContext) =>
            {
                var dish = await dbContext.Dishes
                    .Include(d => d.Meniu)
                    .FirstOrDefaultAsync(d => d.Id == dishId && d.Meniu.Id == meniuId);

                if (dish == null) return Results.NotFound();

                dbContext.Remove(dish);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
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
}
