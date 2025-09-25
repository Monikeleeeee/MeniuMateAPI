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
    public static class MeniuEndPoints
    {
        public static void AddMeniuApi(this WebApplication app)
        {
            var meniuGroup = app.MapGroup("/api").WithValidationFilter();
            //Get all menius
            meniuGroup.MapGet("menius", async (ForumDbContext dbContext, CancellationToken cancellationToken) =>
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
            meniuGroup.MapPost("menius", [Authorize(Roles = ForumRoles.Admin)] async ([Validate] CreateMeniuDto createMeniuDto, HttpContext httpContext, ForumDbContext dbContext) =>
            {
                var meniu = new Meniu()
                {
                    Name = createMeniuDto.Name,
                    Description = createMeniuDto.Description,
                    CreationDate = DateTime.UtcNow,
                    UserId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                };

                dbContext.Menius.Add(meniu);

                await dbContext.SaveChangesAsync();

                return Results.Created($"/api/menius/{meniu.Id}",
                    new MeniuDto(meniu.Id, meniu.Name, meniu.Description));
            });

            //Update meniu
            meniuGroup.MapPut("menius/{meniuId}", [Authorize(Roles = ForumRoles.Admin)] async (int meniuId, [Validate] UpdateMeniuDto updateMeniuDto, ForumDbContext dbContext) =>
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
            meniuGroup.MapDelete("menius/{meniuId}", [Authorize(Roles = ForumRoles.Admin)] async (int meniuId, ForumDbContext dbContext) =>
            {
                var meniu = await dbContext.Menius.FirstOrDefaultAsync(m => m.Id == meniuId);
                if (meniu == null)
                    return Results.NotFound();

                dbContext.Remove(meniu);
                await dbContext.SaveChangesAsync();

                return Results.NoContent();
            });
        }
    }
    public class CreateMeniuDtoValidator : AbstractValidator<CreateMeniuDto>
    {
        public CreateMeniuDtoValidator()
        {
            RuleFor(dto => dto.Name).NotEmpty().NotNull().Length(min: 2, max: 100);
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

}
