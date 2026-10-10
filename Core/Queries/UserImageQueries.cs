using Core.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Core.Queries;

public static class UserImageQueries
{
    public static async Task<int> TodayImageCountForUser(this IMongoCollection<UserImageEntity> userImageCollection, long userId)
    {
        var today = DateTime.UtcNow.Date;

        return await userImageCollection
            .AsQueryable()
            .Where(x => x.UserId == userId &&
                        x.CreatedAt >= today &&
                        x.CreatedAt < today.AddDays(1))
            .CountAsync();
    }
}