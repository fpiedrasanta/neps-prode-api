using Microsoft.EntityFrameworkCore;
using Prode.Domain.Entities;

namespace Prode.Infrastructure.Data.Seed
{
    public static class ResultTypeSeed
    {
        public static async Task SeedResultTypesAsync(ApplicationDbContext context)
        {
            // Verificar si ya existen los ResultType
            if (await context.ResultTypes.AnyAsync())
            {
                return; // Ya están cargados
            }

            var resultTypes = new List<ResultType>
            {
                new ResultType
                {
                    Id = Guid.Parse("a1b2c3d4-e5f6-7890-a1b2-c3d4e5f67890"),
                    Name = "Exacto",
                    Description = "Acertó resultado y goles exactos",
                    Points = 4,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ResultType
                {
                    Id = Guid.Parse("b2c3d4e5-f6a7-8901-b2c3-d4e5f6a78901"),
                    Name = "Parcial Fuerte",
                    Description = "Acertó resultado y diferencia de goles",
                    Points = 3,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ResultType
                {
                    Id = Guid.Parse("c3d4e5f6-a7b8-9012-c3d4-e5f6a7b89012"),
                    Name = "Parcial Débil",
                    Description = "Solo acertó resultado",
                    Points = 2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new ResultType
                {
                    Id = Guid.Parse("d4e5f6a7-b8c9-0123-d4e5-f6a7b8c90123"),
                    Name = "Error",
                    Description = "No acertó resultado",
                    Points = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.ResultTypes.AddRangeAsync(resultTypes);
            await context.SaveChangesAsync();
        }
    }
}