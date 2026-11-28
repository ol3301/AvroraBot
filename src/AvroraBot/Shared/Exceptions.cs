namespace AvroraBot.Shared;

public class BotValidationException(string message) : Exception(message);
public class BotSystemException(string message) : Exception(message);