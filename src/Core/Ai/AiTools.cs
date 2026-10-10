using System.ComponentModel;

namespace Core.Ai;

public static class AiToolList
{
    [Description("""
                 Generate an image based on the user's request.
                 Use this tool whenever the user asks to create, generate, draw, design,
                 visualize, illustrate, or render an image.

                 Construct a complete image-generation prompt from the user's request and
                 relevant conversation context. Preserve all explicitly requested details,
                 including subject, appearance, setting, composition, perspective, style,
                 mood, lighting, colors, text, and other important visual constraints.
                 If the user leaves creative details unspecified, choose appropriate
                 defaults rather than asking unnecessary questions.

                 Do not describe the tool call or explain how the image will be generated;
                 call the tool directly when an image is requested.
                 """)]
    public static void CreateImage(
        [Description("""
                     A self-contained, detailed prompt describing exactly what the generated
                     image should contain and how it should look. Include the user's
                     requested subject, composition, visual style, environment, lighting,
                     colors, mood, camera/viewpoint, and any required text or other details.
                     Resolve relevant context from the conversation into this prompt.
                     """)]
        string imagePrompt) { }
    
    [Description("""
                 Edit an existing image based on the user's request.
                 Use this tool whenever the user asks to edit, modify, alter, retouch,
                 enhance, restore, remove, replace, add to, or otherwise change an image.

                 Construct a complete image-editing prompt from the user's request and
                 relevant conversation context. Preserve all explicitly requested details,
                 including the target image, subjects, appearance, setting, composition,
                 perspective, style, lighting, colors, text, and other visual constraints.

                 Preserve the original image's composition, identity, style, and details
                 that the user did not ask to change. Make only the requested modifications
                 unless additional changes are necessary to produce a coherent result.

                 Do not describe the tool call or explain how the image will be edited;
                 call the tool directly when an image-editing request is made.
                 """)]
    public static void EditImage(
        [Description("""
                     A self-contained, detailed prompt describing exactly how the existing
                     image should be edited. Include the requested modifications, what must
                     remain unchanged, visual style, composition, lighting, colors, and any
                     other relevant constraints. Resolve relevant context from the
                     conversation into this prompt.
                     """)]
        string imagePrompt) { }
}