using Core.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Core.Queries;

public static class UserImageEditQueries
{
    public static async Task<int> TodayImageEditCountForUser(this IMongoCollection<UserImageEditEntity> userImageEditCollection, long userId)
    {
        var today = DateTime.UtcNow.Date;

        return await userImageEditCollection
            .AsQueryable()
            .Where(x => x.UserId == userId &&
                        x.CreatedAt >= today &&
                        x.CreatedAt < today.AddDays(1))
            .CountAsync();
    }
}