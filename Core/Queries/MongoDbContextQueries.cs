using Core.Abstractions;

namespace Core.Queries;

public static class MongoDbContextQueries
{
    public static async Task<int> TodayImageOperationCountForUser(this IMongoDbContext context, long userId)
    {
        var imageGenerations = await context.UserImages.TodayImageCountForUser(userId);
        var imageEdits = await context.UserImageEdits.TodayImageEditCountForUser(userId);

        return imageGenerations + imageEdits;
    }
}