using Core.Entities;
using MongoDB.Driver;

namespace Core.Abstractions;

public interface IMongoDbContext
{
    public IMongoClient  Client { get; }
    public IMongoCollection<UserEntity> Users { get; }
    public IMongoCollection<MessageEntity> Messages { get; }
    public IMongoCollection<UserImageEntity> UserImages { get; }
    public IMongoCollection<UserImageEditEntity> UserImageEdits { get; }
    public IMongoCollection<InvoiceEntity> Invoices { get; }
}