
namespace AvroraBot.Data;

public static class MongoDbContextQueries
{
    public static async Task<int> TodayImageOperationCountForUser(this MongoDbContext context, long userId)
    {
        var imageGenerations = await context.UserImages.TodayImageCountForUser(userId);
        var imageEdits = await context.UserImageEdits.TodayImageEditCountForUser(userId);

        return imageGenerations + imageEdits;
    }
}