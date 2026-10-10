using Core.Abstractions;
using Core.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Infrastructure;

public class MongoDbContext : IMongoDbContext
{
    public IMongoClient Client { get; }
    public IMongoCollection<UserEntity> Users { get; }
    public IMongoCollection<MessageEntity> Messages { get; }
    public IMongoCollection<UserImageEntity> UserImages { get; }
    public IMongoCollection<UserImageEditEntity> UserImageEdits { get; }
    public IMongoCollection<InvoiceEntity> Invoices { get; }

    public MongoDbContext(MongoClient client)
    {
        Client = client;
        Users = client.GetDatabase("TelegramBot").GetCollection<UserEntity>("Users");
        Messages = client.GetDatabase("TelegramBot").GetCollection<MessageEntity>("Messages");
        UserImages = client.GetDatabase("TelegramBot").GetCollection<UserImageEntity>("UserImages");
        UserImageEdits = client.GetDatabase("TelegramBot").GetCollection<UserImageEditEntity>("UserImageEdits");
        Invoices = client.GetDatabase("TelegramBot").GetCollection<InvoiceEntity>("Invoices");
        Messages.Indexes.CreateOne(
            new CreateIndexModel<MessageEntity>(Builders<MessageEntity>.IndexKeys
                .Ascending(x => x.UserId) 
                .Ascending(x => x.CreatedAt)));
    }

    static MongoDbContext()
    {
        MapEntities();
    }

    private static void MapEntities()
    {
        BsonClassMap.RegisterClassMap<UserEntity>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(x => x.UserId);
        });
        
        BsonClassMap.RegisterClassMap<MessageEntity>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(x => x.MessageId);
        });
        
        BsonClassMap.RegisterClassMap<UserImageEntity>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(x => x.MessageId);
        });
        
        BsonClassMap.RegisterClassMap<UserImageEditEntity>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(x => x.MessageId);
        });
        
        BsonClassMap.RegisterClassMap<InvoiceEntity>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(x => x.InvoiceId)
                .SetSerializer(new GuidSerializer(GuidRepresentation.Standard));
        });
    }
}
