namespace Core.CoreServices;

public static class StringsService
{
    public static string BadImageExtensionMessage(string extension, IEnumerable<string> allowedExtensions)
    {
        return $"Тип изображения {extension} не поддерживается. " + 
               $"Допустимые типы: {string.Join(", ", allowedExtensions)}";
    }
    
    public static string BadImageSizeMessage(long size, long maxSize)
    {
        return $"Размер изображения превышает максимально допустимый размер в {maxSize / (1024 * 1024)} МБ. Текущий размер {size / (1024 * 1024)} МБ";
    }
}