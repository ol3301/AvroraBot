using Core.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Core.Queries;

public static class MessageQueries
{
    public static async Task<IEnumerable<MessageEntity>> MessageHistoryForUser(this IMongoCollection<MessageEntity> messagesTable, long userId, DateTime dateTime)
    {
        var messages = await messagesTable.AsQueryable()
            .Where(x => x.UserId == userId && x.CreatedAt > dateTime)
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync();

        return messages
            .OrderBy(x => x.CreatedAt)
            .ToList();
    }
    
    public static async Task<int> TodayMessageCountForUser(this IMongoCollection<MessageEntity> messages, long userId)
    {
        var today = DateTime.UtcNow.Date;

        return await messages
            .AsQueryable()
            .Where(x => x.UserId == userId &&
                        x.CreatedAt >= today &&
                        x.CreatedAt < today.AddDays(1))
            .CountAsync();
    }
    
    public static async Task<int> MessageCountForUser(this IMongoCollection<MessageEntity> messages, long userId, DateTime dateTime)
    {
        return await messages
            .AsQueryable()
            .Where(x => x.UserId == userId && x.CreatedAt  >= dateTime)
            .CountAsync();
    }
}