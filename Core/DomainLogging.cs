using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Core;

public static class DomainLogging
{
    public static void LogTextResponse(ILogger logger, ChatResponse response)
    {
        if (response.Messages.Count > 1)
        {
            logger.LogCritical("Unexpected number of messages in response: {Count}", response.Messages.Count);
            response.Messages.ToList().ForEach(x => logger.LogCritical("Message: {Role}, {Content}", x.Role, x.ToString()));
        }

        var toolCalls = response.Messages.First().Contents.OfType<FunctionCallContent>().ToList();
        if (toolCalls.Count > 0)
        {
            if (toolCalls.Count > 1)
            {
                logger.LogCritical("Unexpected number of tool calls in response: {Count}", toolCalls.Count());
                toolCalls.ForEach(x => logger.LogCritical("FunctionCallContent: {Name}, {Arguments}", x.Name, x.Arguments));
            }
            
            logger.LogInformation("FunctionCallContent: {Name}, {Arguments}", toolCalls.First().Name, toolCalls.First().Arguments);
        }

        if (response.Usage?.InputTokenCount == null)
        {
            logger.LogCritical("InputTokenCount is null");
        }
        
        if (response.Usage?.OutputTokenCount == null)
        {
            logger.LogCritical("OutputTokenCount is null");
        }
    }

    public static void LogImageResponse(ILogger logger, ImageGenerationResponse response)
    {
        var dataContent = response.Contents.OfType<DataContent>().FirstOrDefault();
        if (dataContent == null)
        {
            logger.LogCritical("Missing DataContent in image generation response");
        }
        
        if (response.Usage?.InputTokenCount == null)
        {
            logger.LogCritical("InputTokenCount is null");
        }
        
        if (response.Usage?.OutputTokenCount == null)
        {
            logger.LogCritical("OutputTokenCount is null");
        }
    }
}