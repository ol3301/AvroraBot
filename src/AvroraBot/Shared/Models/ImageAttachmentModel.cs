namespace AvroraBot.Shared.Models;

public record ImageAttachmentModel(FileInfoResponseModel FileInfo, ReadOnlyMemory<byte> Data);