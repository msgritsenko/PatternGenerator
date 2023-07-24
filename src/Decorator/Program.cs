using PatternsGenerator.Decorator;
using System.Text.RegularExpressions;

namespace Decorator
{
    public static class Program
    {
        public static void Main()
        {
            ILogger logger = new MaskLogger(new Logger());

            logger.Information("Driver ID: 125 126 226");
            logger.Error("Driver ID: 125 126 226");
        }
    }

    public interface ILogger
    {
        void Information(string message);

        void Error(string message);
    }

    public class Logger : ILogger
    {
        public void Error(string message)
        {
            Console.Write("Error: ");
            Console.WriteLine(message);
        }

        public void Information(string message)
        {
            Console.Write("Information: ");
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Class for hiding the numbers in messages;
    /// </summary>
    [Decorator]
    public partial class MaskLogger : ILogger
    {
        private readonly ILogger _original;
        private static readonly Regex regex = new Regex(@"\d");
        public MaskLogger(ILogger original)
        {
            _original = original;
        }

        public void Error(string message)
        {
            _original.Error(regex.Replace(message, "*"));
        }
    }
}