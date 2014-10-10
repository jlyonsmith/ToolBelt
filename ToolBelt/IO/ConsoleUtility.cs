using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace ToolBelt
{
    /// <summary>
    /// Message types
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Informational message
        /// </summary>
        Info,
        /// <summary>
        /// Normal message
        /// </summary>
        Normal,
        /// <summary>
        /// Warning message
        /// </summary>
        Warning,
        /// <summary>
        /// Error message
        /// </summary>
        Error
    };

    /// <summary>
    /// 
    /// </summary>
    public static class ConsoleUtility
    {
        /// <summary>
        /// 
        /// </summary>
        public static int ConsoleWidth
        {
            get
            {
                int lineLength = 80;

                // Getting the console width can fail if there is not console
                try
                {
                    lineLength = Console.BufferWidth < 2 ? Console.WindowWidth - 1 : Console.BufferWidth - 1;
                }
                catch (IOException)
                {
                }

                return lineLength;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        /// <param name="lineLength"></param>
        public static void WriteLineWithWordbreaks(string text, int lineLength)
        {
            string[] lines = StringUtility.WordWrap(text, lineLength);

            WriteLines(lines);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lines"></param>
        public static void WriteLines(string[] lines)
        {
            foreach (string line in lines)
                Console.WriteLine(line);
        }

        /// <summary>
        /// Write a console message. Goes to the standard error stream, <see cref="Console.Error"/> 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="message"></param>
        public static void WriteMessage(MessageType s, string message)
        {
            // NOTE: This override is needed for messages that have { } which is not actually formatting

            switch (s)
            {
            case MessageType.Info:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Error.Write(ConsoleUtilityResources.Debug);
                Console.Error.WriteLine(message);
                Console.ResetColor();
                break;
            default:
            case MessageType.Normal:
                Console.Error.WriteLine(message);
                return;
            case MessageType.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.Write(ConsoleUtilityResources.Warning);
                Console.Error.WriteLine(message);
                Console.ResetColor();
                break;
            case MessageType.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write(ConsoleUtilityResources.Error);
                Console.Error.WriteLine(message);
                Console.ResetColor();
                break;
            }
        }

        /// <summary>
        /// Write a console message. Goes to the standard error stream, <see cref="Console.Error"/> 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void WriteMessage(MessageType s, string format, params object[] args)
        {
            switch (s)
            {
            case MessageType.Info:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Error.Write(ConsoleUtilityResources.Debug);
                Console.Error.WriteLine(format, args);
                Console.ResetColor();
                break;
            default:
            case MessageType.Normal:
                Console.Error.WriteLine(format, args);
                return;
            case MessageType.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Error.Write(ConsoleUtilityResources.Warning);
                Console.Error.WriteLine(format, args);
                Console.ResetColor();
                break;
            case MessageType.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.Write(ConsoleUtilityResources.Error);
                Console.Error.WriteLine(format, args);
                Console.ResetColor();
                break;
            }
        }

    }
}
