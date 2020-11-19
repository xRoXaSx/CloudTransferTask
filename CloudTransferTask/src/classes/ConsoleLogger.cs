using System;

namespace CloudTransferTask.src.classes {
    class ConsoleLogger {

        /// <summary>
        /// Log information
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void Debug(string text) {
            WriteHighlightedConsole(ConsoleColor.Gray, "", "DEBUG:\t", text);
        }

        /// <summary>
        /// Log info
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void Info(string text) {
            WriteHighlightedConsole(ConsoleColor.Green, "", "INFO:\t", text);
        }


        /// <summary>
        /// Log warnings
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void Warning(string text) {
            WriteHighlightedConsole(ConsoleColor.Gray, "", "WARNING:\t", text);
        }


        /// <summary>
        /// Log notice
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void Notice(string text) {
            WriteHighlightedConsole(ConsoleColor.Yellow, "", "NOTICE:\t", text);
        }


        /// <summary>
        /// Log errors
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void Error(string text) {
            WriteHighlightedConsole(ConsoleColor.Red, "", "ERROR:\t", text);
        }


        /// <summary>
        /// Validation info
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void ValidationOK(string text) {
            WriteHighlightedConsole(ConsoleColor.Green, "", "VALIDATION OK:\t", text);
        }


        /// <summary>
        /// Validation info
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void ValidationInfo(string text) {
            WriteHighlightedConsole(ConsoleColor.Yellow, "", "VALIDATION INF:\t", text);
        }


        /// <summary>
        /// Validation error info
        /// </summary>
        /// <param name="text">The text that should be logged</param>
        public static void ValidationError(string text) {
            WriteHighlightedConsole(ConsoleColor.Red, "", "VALIDATION ERR:\t", text);
        }


        /// <summary>
        /// Write highlighted to console
        /// </summary>
        /// <param name="highlightedColor">The ConsoleColor to write the highlighted text in</param>
        /// <param name="text1">Non-highlighted text</param>
        /// <param name="text2">Highlighted text</param>
        /// <param name="text3">Non-highlighted text</param>
        public static void WriteHighlightedConsole(ConsoleColor highlightedColor, string text1, string text2, string text3 = "") {
            var initialColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + text1);

            Console.ForegroundColor = highlightedColor;
            Console.Write(text2);

            Console.ForegroundColor = initialColor;
            Console.WriteLine(text3);
        }
    }
}
