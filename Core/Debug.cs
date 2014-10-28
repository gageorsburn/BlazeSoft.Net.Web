using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;

namespace BlazeSoft.Net.Web
{
    internal class DebugSession
    {
        internal Dictionary<string, Stopwatch> timers = new Dictionary<string, Stopwatch>();

        internal string Output { get; set; }
    }

    /// <summary>
    /// Represents the debug utility.
    /// </summary>
    public class Debug
    {
        private static Dictionary<HttpContext, DebugSession> debugSessions = new Dictionary<HttpContext, DebugSession>();

        internal Debug() { }

        private static DebugSession GetSession(HttpContext httpContext)
        {
            if (!debugSessions.ContainsKey(httpContext))
                debugSessions.Add(httpContext, new DebugSession());

            return debugSessions[httpContext];
        }

        private static void DestroySession(HttpContext httpContext)
        {
            if (debugSessions.ContainsKey(httpContext))
                debugSessions.Remove(httpContext);
        }

        /// <summary>
        /// Writes a line of text to the debug console.
        /// </summary>
        /// <param name="textObject">Line of text to write to debug console.</param>
        /// <param name="parameters">Parameters to be replaced in text.</param>
        public static void WriteLine(object textObject, params object[] parameters)
        {
            Write(textObject, parameters);
            Write(Environment.NewLine);
        }

        /// <summary>
        /// Writes text to the debug console.
        /// </summary>
        /// <param name="textObject">Text to write to the debug console.</param>
        /// <param name="parameters">Parameters to be replaced in text.</param>
        public static void Write(object textObject, params object[] parameters)
        {
            DebugSession debugSession = GetSession(HttpContext.Current);

            string text = textObject != null ? textObject.ToString() : "";

            for (int parameter = 0; parameter < parameters.Length; parameter++)
                text = text.Replace("{" + parameter + "}", parameters[parameter] != null ? parameters[parameter].ToString() : "{NULL}");

            debugSession.Output += text;
        }

        /// <summary>
        /// Starts the specified timer.
        /// </summary>
        /// <param name="name">Name of the timer.</param>
        public static void StartTimer(string name)
        {
            DebugSession debugSession = GetSession(HttpContext.Current);

            if (!debugSession.timers.ContainsKey(name))
                debugSession.timers.Add(name, new Stopwatch());

            debugSession.timers[name].Start();
        }

        /// <summary>
        /// Stops the specified timer.
        /// </summary>
        /// <param name="name">Name of the timer.</param>
        public static void StopTimer(string name)
        {
            DebugSession debugSession = GetSession(HttpContext.Current);

            if (debugSession.timers.ContainsKey(name))
                debugSession.timers[name].Stop();
        }

        internal static string Output
        {
            get
            {
                DebugSession debugSession = GetSession(HttpContext.Current);
                string Output = string.Empty;

                Output += "<!--\r\n";
                Output += "-------------------------------------------------------------------------------\r\n";
                Output += "- BlazeSoft Debug Output \r\n";
                Output += "-------------------------------------------------------------------------------\r\n";

                foreach (string timerKey in debugSession.timers.Keys)
                    Output += "- " + timerKey + ": " + debugSession.timers[timerKey].ElapsedMilliseconds + "ms" + "\r\n";

                Output += "-------------------------------------------------------------------------------\r\n";

                foreach (string OutputLine in debugSession.Output.SplitByNewLine())
                    if (OutputLine.Trim() != "")
                        Output += "- " + OutputLine + "\r\n";

                Output += "-------------------------------------------------------------------------------\r\n";
                Output += "-->";
                
                DestroySession(HttpContext.Current);
                return Output;
            }
        }
    }
}