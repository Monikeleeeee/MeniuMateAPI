using FluentValidation;
using MeniuMate_API.Auth.Model;
using MeniuMate_API.Data;
using MeniuMate_API.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using O9d.AspNet.FluentValidation;
using System.Net.Http;
using System.Security.Claims;

namespace MeniuMate_API
{
    public static class CommentEndPoints
    {
        public static void AddCommentApi(this WebApplication app)
        {
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

                return Results.Ok(comments.Select(c => new CommentDto(c.Id, c.Content, c.Rating, c.Dish.Id, c.UserId)));
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

                return Results.Ok(new CommentDto(comment.Id, comment.Content, comment.Rating, comment.Dish.Id, comment.UserId));
            });

            // Create comment
            commentGroup.MapPost("comments", [Authorize(Roles = ForumRoles.ForumUser)] async (int meniuId, int dishId, [Validate] CreateCommentDto createCommentDto, HttpContext httpContext, ForumDbContext dbContext) =>
            {
                var dish = await dbContext.Dishes.Include(d => d.Meniu)
                    .FirstOrDefaultAsync(d => d.Id == dishId && d.Meniu.Id == meniuId);

                if (dish == null) return Results.NotFound("Dish not found");

                var userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                var comment = new Comment
                {
                    Content = createCommentDto.Content,
                    Rating = createCommentDto.Rating,
                    CreationDate = DateTime.UtcNow,
                    Dish = dish,
                    UserId = userId
                };

                dbContext.Comments.Add(comment);
                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/menius/{meniuId}/dishes/{dishId}/comments/{comment.Id}",
                    new CommentDto(comment.Id, comment.Content, comment.Rating, dish.Id, dish.UserId));
            });

            // Update comment
            commentGroup.MapPut("comments/{commentId}", [Authorize(Roles = ForumRoles.ForumUser)] async (int meniuId, int dishId, int commentId, [Validate] UpdateCommentDto updateCommentDto, HttpContext httpContext, ForumDbContext dbContext) =>
            {
                var comment = await dbContext.Comments
                    .Include(c => c.Dish)
                    .ThenInclude(d => d.Meniu)
                    .FirstOrDefaultAsync(c => c.Id == commentId
                                           && c.Dish.Id == dishId
                                           && c.Dish.Meniu.Id == meniuId);

                if (comment == null)
                    return Results.NotFound("Comment not found");

                var userIdFromToken = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                    ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Normalize both to lowercase and trim
                if (!httpContext.User.IsInRole(ForumRoles.Admin) &&
                    userIdFromToken?.Trim().ToLower() != comment.UserId?.Trim().ToLower())
                {
                    return Results.Forbid();
                }

                comment.Content = updateCommentDto.Content;
                comment.Rating = updateCommentDto.Rating;

                await dbContext.SaveChangesAsync();

                return Results.Ok(new CommentDto(comment.Id, comment.Content, comment.Rating, comment.Dish.Id, comment.UserId));
            });


            // Delete comment
            commentGroup.MapDelete("comments/{commentId}", [Authorize(Roles = ForumRoles.ForumUser)]
            async (int meniuId, int dishId, int commentId, HttpContext httpContext, ForumDbContext dbContext) =>
            {
                var comment = await dbContext.Comments
                    .Include(c => c.Dish)
                    .ThenInclude(d => d.Meniu)
                    .FirstOrDefaultAsync(c => c.Id == commentId && c.Dish.Id == dishId && c.Dish.Meniu.Id == meniuId);

                if (comment == null)
                    return Results.NotFound();

                var userIdFromToken = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                                      ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

                var normalizedTokenUserId = userIdFromToken?.Trim().ToLower();
                var normalizedCommentUserId = comment.UserId?.Trim().ToLower();

                if (!httpContext.User.IsInRole(ForumRoles.Admin) &&
                    normalizedTokenUserId != normalizedCommentUserId)
                {
                    return Results.Forbid();
                }

                dbContext.Comments.Remove(comment);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });

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

}
