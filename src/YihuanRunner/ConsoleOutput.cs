using System.Text;

namespace YihuanRunner;

internal static class ConsoleOutput
{
    public static void TryConfigureUtf8()
    {
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
