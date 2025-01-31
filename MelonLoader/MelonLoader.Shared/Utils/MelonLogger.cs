using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using MelonLoader.Pastel;

namespace MelonLoader.Utils
{
    public class MelonLogger
    {
        public static readonly Color DefaultMelonColor = Color.Cyan;

        //Identical to Msg(string) except it skips walking the stack to find a melon
        internal static void MsgDirect(string txt) =>
            NativeMsg(DefaultMelonColor, MelonUtils.DefaultTextColor, null, txt, true);

        public static void Msg(object obj) =>
            NativeMsg(DefaultMelonColor, MelonUtils.DefaultTextColor, null, obj.ToString());

        public static void Msg(string txt) => NativeMsg(DefaultMelonColor, MelonUtils.DefaultTextColor, null, txt);

        public static void Msg(string txt, params object[] args) => NativeMsg(DefaultMelonColor,
            MelonUtils.DefaultTextColor, null, string.Format(txt, args));

        public static void Msg(ConsoleColor txt_color, object obj) =>
            NativeMsg(DefaultMelonColor, txt_color.ToDrawingColor(), null, obj.ToString());

        public static void Msg(ConsoleColor txt_color, string txt) =>
            NativeMsg(DefaultMelonColor, txt_color.ToDrawingColor(), null, txt);

        public static void Msg(ConsoleColor txt_color, string txt, params object[] args) => NativeMsg(DefaultMelonColor,
            txt_color.ToDrawingColor(), null, string.Format(txt, args));

        //Identical to Msg(Color, string) except it skips walking the stack to find a melon
        public static void MsgDirect(Color txt_color, string txt) =>
            NativeMsg(DefaultMelonColor, txt_color, null, txt, true);

        public static void Msg(Color txt_color, object obj) =>
            NativeMsg(DefaultMelonColor, txt_color, null, obj.ToString());

        public static void Msg(Color txt_color, string txt) => NativeMsg(DefaultMelonColor, txt_color, null, txt);

        public static void Msg(Color txt_color, string txt, params object[] args) =>
            NativeMsg(DefaultMelonColor, txt_color, null, string.Format(txt, args));


        public static void Warning(object obj) => NativeWarning(null, obj.ToString());
        public static void Warning(string txt) => NativeWarning(null, txt);
        public static void Warning(string txt, params object[] args) => NativeWarning(null, string.Format(txt, args));


        public static void Error(object obj) => NativeError(null, obj.ToString());
        public static void Error(string txt) => NativeError(null, txt);
        public static void Error(string txt, params object[] args) => NativeError(null, string.Format(txt, args));
        public static void Error(string txt, Exception ex) => NativeError(null, $"{txt}\n{ex}");

        public static void WriteLine(int length = 30) => MsgDirect(new string('-', length));
        public static void WriteLine(Color color, int length = 30) => MsgDirect(color, new string('-', length));

        private static void NativeMsg(Color namesection_color, Color txt_color, string namesection, string txt,
            bool skipStackWalk = false)
        {
            Internal_Msg(namesection_color, txt_color, namesection, txt ?? "null");
            RunMsgCallbacks(namesection_color, txt_color, namesection, txt ?? "null");
        }

        private static void NativeWarning(string namesection, string txt)
        {
            Internal_Warning(namesection, txt ?? "null");
            RunWarningCallbacks(namesection, txt ?? "null");
        }

        private static void NativeError(string namesection, string txt)
        {
            Internal_Error(namesection, txt ?? "null");
            RunErrorCallbacks(namesection, txt ?? "null");
        }

        public static void BigError(string namesection, string txt)
        {
            RunErrorCallbacks(namesection, txt ?? "null");

            Internal_Error(namesection, new string('=', 50));
            foreach (var line in txt.Split('\n'))
                Internal_Error(namesection, line);
            Internal_Error(namesection, new string('=', 50));
        }

        internal static void RunMsgCallbacks(Color namesection_color, Color txt_color, string namesection, string txt)
        {
            MsgCallbackHandler?.Invoke(namesection_color, txt_color, namesection, txt);
        }

        public static event Action<Color, Color, string, string> MsgCallbackHandler;

        internal static void RunWarningCallbacks(string namesection, string txt) =>
            WarningCallbackHandler?.Invoke(namesection, txt);

        public static event Action<string, string> WarningCallbackHandler;

        internal static void RunErrorCallbacks(string namesection, string txt) =>
            ErrorCallbackHandler?.Invoke(namesection, txt);

        public static event Action<string, string> ErrorCallbackHandler;

        public class Instance
        {
            private string Name = null;
            private Color DrawingColor = DefaultMelonColor;

            public Instance(string name) => Name = name?.Replace(" ", "_");
            public Instance(string name, Color color) : this(name) => DrawingColor = color;

            public void Msg(object obj) => NativeMsg(DrawingColor, MelonUtils.DefaultTextColor, Name, obj.ToString());
            public void Msg(string txt) => NativeMsg(DrawingColor, MelonUtils.DefaultTextColor, Name, txt);

            public void Msg(string txt, params object[] args) => NativeMsg(DrawingColor, MelonUtils.DefaultTextColor,
                Name, string.Format(txt, args));


            public void Msg(ConsoleColor txt_color, object obj) =>
                NativeMsg(DrawingColor, txt_color.ToDrawingColor(), Name, obj.ToString());

            public void Msg(ConsoleColor txt_color, string txt) =>
                NativeMsg(DrawingColor, txt_color.ToDrawingColor(), Name, txt);

            public void Msg(ConsoleColor txt_color, string txt, params object[] args) => NativeMsg(DrawingColor,
                txt_color.ToDrawingColor(), Name, string.Format(txt, args));

            public void Msg(Color txt_color, object obj) => NativeMsg(DrawingColor, txt_color, Name, obj.ToString());
            public void Msg(Color txt_color, string txt) => NativeMsg(DrawingColor, txt_color, Name, txt);

            public void Msg(Color txt_color, string txt, params object[] args) =>
                NativeMsg(DrawingColor, txt_color, Name, string.Format(txt, args));

            public void Warning(object obj) => NativeWarning(Name, obj.ToString());
            public void Warning(string txt) => NativeWarning(Name, txt);
            public void Warning(string txt, params object[] args) => NativeWarning(Name, string.Format(txt, args));

            public void Error(object obj) => NativeError(Name, obj.ToString());
            public void Error(string txt) => NativeError(Name, txt);
            public void Error(string txt, params object[] args) => NativeError(Name, string.Format(txt, args));
            public void Error(string txt, Exception ex) => NativeError(Name, $"{txt}\n{ex}");

            public void WriteSpacer() => MelonLogger.WriteSpacer();
            public void WriteLine(int length = 30) => MelonLogger.WriteLine(length);
            public void WriteLine(Color color, int length = 30) => MelonLogger.WriteLine(color, length);

            public void BigError(string txt) => MelonLogger.BigError(Name, txt);
        }

        internal static void Internal_Msg(Color namesection_color, Color txt_color, string namesection, string txt,
            bool error = false)
        {
            BootstrapInterop.NativeWriteLogToFile(
                $"[{MelonUtils.TimeStamp}] {(error ? "[ERROR] " : "")}{(namesection is null ? "" : $"[{namesection}] ")}{txt}");

            if (MelonUtils.IsUnderWineOrSteamProton())
            {
                WriteTimestampWine(error);
                if (error)
                    WriteConsoleColor(ConsoleColor.Red, "[ERROR] ");
                
                if (namesection is not null)
                {
                    WriteConsoleColor(error ? ConsoleColor.Red : ConsoleColor.Gray, "[");
                    WriteConsoleColor(namesection_color.ToConsoleColor(), namesection);
                    WriteConsoleColor(error ? ConsoleColor.Red : ConsoleColor.Gray, "] ");
                }
                
                WriteConsoleColor(txt_color.ToConsoleColor(), txt, "\n");
                
                return;
            }


            StringBuilder builder = new StringBuilder();
            
            if (error)
                builder.Append("[ERROR]".Pastel(Color.IndianRed));
            
            builder.Append(GetTimestamp(error));

            if (namesection is not null)
            {
                builder.Append("[".Pastel(error ? Color.IndianRed : Color.LightGray));
                builder.Append(namesection.Pastel(namesection_color));
                builder.Append("] ".Pastel(error ? Color.IndianRed : Color.LightGray));
            }

            builder.Append(txt.Pastel(txt_color));
            Console.WriteLine(builder.ToString());
        }
        [DllImport("Kernel32")]
        internal static extern int SetConsoleTextAttribute(IntPtr hConsoleOutput, int wAttributes);
        
        [DllImport("Kernel32")]
        internal static extern IntPtr GetStdHandle(uint handleType);
        internal static void WriteConsoleColor(params object[] oo)
        {
            foreach (var o in oo)
                if (o == null)
                    Console.ResetColor();
                else if (o is ConsoleColor color)
                    SetConsoleTextAttribute(GetStdHandle(4294967285), (int)color);
                else
                    Console.Write(o.ToString());
        }

        internal static void WriteTimestampWine(bool error)
        {
            if (error)
            {
                WriteConsoleColor(ConsoleColor.Red, $"[{MelonUtils.TimeStamp}] ");
                return;
            }
            
            WriteConsoleColor(ConsoleColor.Gray, $"[");
            WriteConsoleColor(ConsoleColor.Green, MelonUtils.TimeStamp);
            WriteConsoleColor(ConsoleColor.Gray, $"] ");
        }

        internal static string GetTimestamp(bool error)
        {
            StringBuilder builder = new StringBuilder();
            if (error)
            {
                builder.Append("[".Pastel(Color.IndianRed));
                builder.Append(MelonUtils.TimeStamp.Pastel(Color.IndianRed));
                builder.Append("] ".Pastel(Color.IndianRed));
                return builder.ToString();
            }

            builder.Append("[".Pastel(Color.LightGray));
            builder.Append(MelonUtils.TimeStamp.Pastel(Color.LimeGreen));
            builder.Append("] ".Pastel(Color.LightGray));

            return builder.ToString();
        }

        internal static void Internal_Warning(string namesection, string txt)
        {
            Internal_Msg(Color.Yellow, Color.Yellow, namesection, txt);
        }


        internal static void Internal_Error(string namesection, string txt) =>
            Internal_Msg(Color.IndianRed, Color.IndianRed, namesection, txt, true);

        public static void WriteSpacer()
        {
            BootstrapInterop.NativeWriteLogToFile("");
            Console.WriteLine();
        }

        internal static void Internal_PrintModName(Color meloncolor, Color authorcolor, string name, string author,
            string additionalCredits, string version, string id)
        {
            BootstrapInterop.NativeWriteLogToFile(
                $"[{MelonUtils.TimeStamp}] {name} v{version}{(id == null ? "" : $" ({id})")}");
            BootstrapInterop.NativeWriteLogToFile($"[{MelonUtils.TimeStamp}] by {author}");

            StringBuilder builder = new StringBuilder();
            builder.Append(GetTimestamp(false));
            builder.Append(name.Pastel(meloncolor));
            builder.AppendLine($" v{version} {(id == null ? "" : $"({id})")}");

            builder.Append(GetTimestamp(false));
            builder.Append($"by {author}".Pastel(authorcolor));

            if (additionalCredits is not null)
            {
                builder.AppendLine();
                builder.Append(GetTimestamp(false));
                builder.Append($"Additional credits: {additionalCredits}");
            }

            Console.WriteLine(builder.ToString());
        }
    }
}