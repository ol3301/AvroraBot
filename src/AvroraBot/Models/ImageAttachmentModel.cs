namespace AvroraBot.Models;

public record ImageAttachmentModel(FileInfoResponseModel FileInfo, ReadOnlyMemory<byte> Data);