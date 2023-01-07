global using global::System;
global using global::System.Collections.Generic;
global using global::System.IO;
global using global::System.Linq;
global using global::System.Threading;
global using global::System.Threading.Tasks;

using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ChartZ.Compiler
{

    public class Segment
    {

        public string type;
        public string command;
        public string header;
        public List<string> payload;
        public List<string> branches;
        public int branchesLN;
        public List<Condition> conditions;
        public string jump;
        public int jumpLN;

    }

    public class Condition
    {
        public Condition parent;
        public ModeType mode = ModeType.AND;

        public string memory;
        public ConditionType type;
        public int value;

        public List<Condition> checks = new List<Condition>();
    }

    public enum ModeType
    {
        AND,
        OR
    }

    public enum ConditionType
    {

        EQUAL,
        NOT_EQUAL,

        GREATER_THAN,
        LESS_THAN,

        GREATER_OR_EQUAL,
        LESS_OR_EQUAL
    }

    public class Program
    {
        private static Dictionary<string, Segment> Segments = new Dictionary<string, Segment>();
        private static List<string> GetArgumentListFromString(string args)
        {
            List<string> args3 = new List<string>();

            bool ignorespaces = false;
            string last = "";
            char sep = '\0';
            int i = 0;
            foreach (char c in args)
            {
                if (!ignorespaces && (c == '"' || c == '\''))
                    sep = c;
                if (c == sep && (i == 0 || args[i - 1] != '\\'))
                {
                    if (ignorespaces) ignorespaces = false;
                    else ignorespaces = true;
                }
                else if (c == ' ' && !ignorespaces && (i == 0 || args[i - 1] != '\\'))
                {
                    if (ignorespaces || last != "")
                        args3.Add(last);
                    last = "";
                }
                else if (c != '\\' || (i + 1 < args.Length && args[i + 1] != sep && (args[i + 1] != ' ' || ignorespaces)))
                {
                    last += c;
                }

                i++;
            }

            if (last == "" == false) args3.Add(last);

            return args3;
        }

        /// <summary>
        /// Main method
        /// </summary>
        public static void Main(string[] args)
        {
            // Check files
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Error: no input files specified.");
                Environment.Exit(1);
            }

            // Compile files
            foreach (string file in args)
            {
                if (!File.Exists(file))
                    Console.Error.WriteLine("Not found: " + file);
                else
                    if (!Compile(File.ReadAllText(file).Replace("\r", "").Replace("\t", "    ").Split('\n'), file))
                    Environment.Exit(1);
            }
        }

        private class InjectedSegment
        {
            public string segID;

            public Segment oSeg;
            public string oSegName;

            public string cmd;
            public List<string> cmdArgs;
        }

        /// <summary>
        /// Compiles a given file
        /// </summary>
        /// <param name="commands">Commands to run</param>
        public static bool Compile(string[] commands, string file = null, Stream? outputStream = null)
        {
            bool built = false;

            List<InjectedSegment> injection = new List<InjectedSegment>();

            string currentSegmentName = null;
            Segment currentSegment = null;

            // Handle each command
            bool Handle(string command, List<string> args, string fullCommand)
            {
                // Handle injecting segments
                bool injected = false;
                for (int i = 0; i < args.Count; i++)
                {
                    string arg = args[i];
                    if (arg == "!>>")
                    {
                        if (args[i + 1] == "segment")
                        {
                            args.RemoveAt(i + 1);
                            args.RemoveAt(i);

                            InjectedSegment inj = new InjectedSegment();
                            string segUUID = Guid.NewGuid().ToString();
                            args.Add(segUUID);

                            // Inject segment
                            inj.oSeg = currentSegment;
                            inj.oSegName = currentSegmentName;
                            inj.cmd = command;
                            inj.cmdArgs = args;
                            currentSegment = null;
                            currentSegmentName = null;
                            HandleCommand("segment", new List<string>(new string[] { segUUID }), "segment " + segUUID);
                            injection.Add(inj);
                            injected = true;
                            break;
                        }
                    }
                }

                if (injected)
                    return true;
                if (!HandleCommand(command, args, fullCommand))
                    return false;
                return true;
            }
            int ln = 0;
            foreach (string cmd in commands)
            {
                ln++;
                string command = cmd;
                while (command.StartsWith(" "))
                    command = command.Substring(1);
                if (command == "" || command.StartsWith("#") || command.StartsWith("//"))
                    continue;

                // Parse
                List<string> args = GetArgumentListFromString(command);
                if (args.Count == 0)
                    continue;
                string commandFull = command;
                command = args[0];
                args.RemoveAt(0);

                if (!Handle(command, args, commandFull))
                    return false;
            }

            bool HandleCommand(string command, List<string> args, string commandFull)
            {
                switch (command)
                {
                    case "build":
                        {
                            if (currentSegment != null)
                            {
                                Console.Error.WriteLine("Unclosed segment: " + currentSegmentName);
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            if (outputStream == null)
                            {
                                if (args.Count < 1)
                                {
                                    Console.Error.WriteLine("Missing arument for command 'build', expected binary or json.");
                                    if (file != null)
                                        Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                    return false;
                                }
                                else if (args[0] != "json" && args[0] != "binary")
                                {
                                    Console.Error.WriteLine("Invalid arument for command 'build', expected binary or json.");
                                    if (file != null)
                                        Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                    return false;
                                }
                                if (args.Count < 2)
                                {
                                    Console.Error.WriteLine("Missing arument for command 'build', missing output file.");
                                    if (file != null)
                                        Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                    return false;
                                }
                                if (args.Count < 3)
                                {
                                    Console.Error.WriteLine("Missing arument for command 'build', missing segment variable name.");
                                    if (file != null)
                                        Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                    return false;
                                }
                                else if (!Segments.ContainsKey(args[2]))
                                {
                                    Console.Error.WriteLine("Invalid arument for command 'build', invalid segment variable name.");
                                    if (file != null)
                                        Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                    return false;
                                }
                            }
                            else
                            {
                                if (args.Count < 1)
                                {
                                    Console.Error.WriteLine("Missing arument for command 'build', missing segment variable name.");
                                    if (file != null)
                                        Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                    return false;
                                }
                                else if (!Segments.ContainsKey(args[0]))
                                {
                                    Console.Error.WriteLine("Invalid arument for command 'build', invalid segment variable name.");
                                    if (file != null)
                                        Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                    return false;
                                }
                            }

                            // Tag list
                            List<string> tags = new List<string>();
                            Dictionary<int, string> tagsV = new Dictionary<int, string>();
                            int GetTag(string property)
                            {
                                if (!tags.Contains(property))
                                    tags.Add(property);
                                return tags.IndexOf(property);
                            }

                            // Verify segment references
                            foreach (Segment seg in Segments.Values)
                            {
                                if (seg.jump != null && !Segments.ContainsKey(seg.jump))
                                {
                                    Console.Error.WriteLine("Jump node not recognized: " + seg.jump);
                                    if (file != null)
                                        Console.Error.WriteLine("    At [" + file + "]:" + seg.jumpLN);
                                    return false;
                                }
                                if (seg.branches != null)
                                {
                                    // Verify branches
                                    foreach (string branch in seg.branches)
                                    {
                                        if (!Segments.ContainsKey(branch))
                                        {
                                            Console.Error.WriteLine("Branch node not recognized: " + branch);
                                            if (file != null)
                                                Console.Error.WriteLine("    At [" + file + "]:" + seg.branchesLN);
                                            return false;
                                        }
                                    }
                                }
                            }

                            // Build
                            Stream output;
                            if (outputStream == null)
                            {
                                if (File.Exists(args[1]))
                                    File.Delete(args[1]);
                                output = File.OpenWrite(args[1]);
                            }
                            else
                            {
                                output = outputStream;
                                string node = args[0];
                                args.Clear();
                                args.Add("binary");
                                args.Add("");
                                args.Add(node);
                            }
                            switch (args[0])
                            {
                                case "binary":
                                    {
                                        // Build segment data
                                        MemoryStream segments = new MemoryStream();
                                        List<string> added = new List<string>();
                                        Segment entry = Segments[args[2]];
                                        void AddSegment(Segment seg, string segI)
                                        {
                                            if (added.Contains(segI))
                                                return;

                                            // References first
                                            if (seg.jump != null)
                                                AddSegment(Segments[seg.jump], seg.jump);
                                            if (seg.branches != null)
                                            {
                                                foreach (string i in seg.branches)
                                                    AddSegment(Segments[i], i);
                                            }

                                            // Add
                                            added.Add(segI);

                                            // Segment command
                                            if (seg.command != null)
                                                WriteInt(GetTag(seg.command), segments);
                                            else
                                                WriteInt(-1, segments);

                                            // Segment type
                                            if (seg.type != null)
                                                WriteInt(GetTag(seg.type), segments);
                                            else
                                                WriteInt(-1, segments);

                                            // Segment jump
                                            if (seg.jump != null)
                                                WriteInt(added.IndexOf(seg.jump), segments);
                                            else
                                                WriteInt(-1, segments);

                                            // Segment header
                                            if (seg.header != null)
                                                WriteInt(GetTag(seg.header), segments);
                                            else
                                                WriteInt(-1, segments);

                                            // Segment payload
                                            if (seg.payload != null)
                                            {
                                                WriteInt(seg.payload.Count, segments);
                                                foreach (string payloadI in seg.payload)
                                                    WriteInt(GetTag(payloadI), segments);
                                            }
                                            else
                                                WriteInt(0, segments);

                                            // Segment branches
                                            if (seg.branches != null)
                                            {
                                                WriteInt(seg.branches.Count, segments);
                                                foreach (string branch in seg.branches)
                                                    WriteInt(added.IndexOf(branch), segments);
                                            }
                                            else
                                                WriteInt(0, segments);

                                            // Segment conditions
                                            if (seg.conditions != null)
                                            {
                                                void WriteConditions(List<Condition> conds)
                                                {
                                                    foreach (Condition segCond in conds)
                                                    {
                                                        segments.WriteByte((byte)(segCond.mode == ModeType.AND ? 1 : 0));
                                                        if (segCond.checks.Count == 0)
                                                        {
                                                            WriteInt(0, segments);
                                                            switch (segCond.type)
                                                            {
                                                                case ConditionType.EQUAL:
                                                                    segments.WriteByte(0);
                                                                    break;
                                                                case ConditionType.NOT_EQUAL:
                                                                    segments.WriteByte(1);
                                                                    break;
                                                                case ConditionType.GREATER_THAN:
                                                                    segments.WriteByte(2);
                                                                    break;
                                                                case ConditionType.GREATER_OR_EQUAL:
                                                                    segments.WriteByte(3);
                                                                    break;
                                                                case ConditionType.LESS_THAN:
                                                                    segments.WriteByte(4);
                                                                    break;
                                                                case ConditionType.LESS_OR_EQUAL:
                                                                    segments.WriteByte(5);
                                                                    break;
                                                            }
                                                            WriteInt(GetTag(segCond.memory), segments);
                                                            WriteInt(segCond.value, segments);
                                                        }
                                                        else
                                                        {
                                                            WriteInt(segCond.checks.Count, segments);
                                                            WriteConditions(segCond.checks);
                                                        }
                                                    }
                                                }
                                                WriteInt(seg.conditions.Count, segments);
                                                WriteConditions(seg.conditions);
                                            }
                                            else
                                                WriteInt(0, segments);
                                        }
                                        AddSegment(entry, args[2]);

                                        // Write entry
                                        WriteInt(added.IndexOf(args[2]), output);

                                        // Write tags
                                        WriteInt(tags.Count, output);
                                        foreach (string tag in tags)
                                            WriteString(tag, output);

                                        // Write segmetns
                                        WriteInt(added.Count, output);
                                        output.Write(segments.ToArray());

                                        break;
                                    }
                                case "json":
                                    {
                                        // Build segment jsons
                                        List<Dictionary<string, object>> segments = new List<Dictionary<string, object>>();
                                        List<string> added = new List<string>();
                                        Segment entry = Segments[args[2]];
                                        void AddSegment(Segment seg, string segI)
                                        {
                                            if (added.Contains(segI))
                                                return;

                                            // References first
                                            if (seg.jump != null)
                                                AddSegment(Segments[seg.jump], seg.jump);
                                            if (seg.branches != null)
                                            {
                                                foreach (string i in seg.branches)
                                                    AddSegment(Segments[i], i);
                                            }

                                            Dictionary<string, object> segment = new Dictionary<string, object>();

                                            // Add
                                            added.Add(segI);

                                            // Segment type
                                            if (seg.type != null)
                                                segment[GetTag("type").ToString()] = GetTag(seg.type);
                                            // Segment command
                                            if (seg.command != null)
                                                segment[GetTag("command").ToString()] = GetTag(seg.command);
                                            // Segment header
                                            if (seg.header != null)
                                                segment[GetTag("header").ToString()] = GetTag(seg.header);
                                            // Segment conditions
                                            if (seg.conditions != null)
                                            {
                                                List<int> WriteConditions(List<Condition> conds)
                                                {
                                                    List<int> conditions = new List<int>();
                                                    foreach (Condition segCond in conds)
                                                    {
                                                        conditions.Add(GetTag(segCond.mode.ToString().ToLower()));
                                                        if (segCond.checks.Count == 0)
                                                        {
                                                            conditions.Add(0);
                                                            switch (segCond.type)
                                                            {
                                                                case ConditionType.EQUAL:
                                                                    conditions.Add(0);
                                                                    break;
                                                                case ConditionType.NOT_EQUAL:
                                                                    conditions.Add(1);
                                                                    break;
                                                                case ConditionType.GREATER_THAN:
                                                                    conditions.Add(2);
                                                                    break;
                                                                case ConditionType.GREATER_OR_EQUAL:
                                                                    conditions.Add(3);
                                                                    break;
                                                                case ConditionType.LESS_THAN:
                                                                    conditions.Add(4);
                                                                    break;
                                                                case ConditionType.LESS_OR_EQUAL:
                                                                    conditions.Add(5);
                                                                    break;
                                                            }
                                                            conditions.Add(GetTag(segCond.memory));
                                                            conditions.Add(segCond.value);
                                                        }
                                                        else
                                                        {
                                                            conditions.Add(segCond.checks.Count);
                                                            conditions.AddRange(WriteConditions(segCond.checks));
                                                        }
                                                    }
                                                    return conditions;
                                                }
                                                segment[GetTag("conditions").ToString()] = WriteConditions(seg.conditions);
                                            }
                                            // Segment payload
                                            if (seg.payload != null)
                                                segment[GetTag("payload").ToString()] = new List<int>(seg.payload.Select(t => GetTag(t)));
                                            // Segment branches
                                            if (seg.branches != null)
                                                segment[GetTag("branches").ToString()] = new List<int>(seg.branches.Select(t => added.IndexOf(t)));
                                            // Segment jump
                                            if (seg.jump != null)
                                                segment[GetTag("jump").ToString()] = added.IndexOf(seg.jump);

                                            // Add segment
                                            segments.Add(segment);
                                        }
                                        AddSegment(entry, args[2]);

                                        // Build output
                                        Dictionary<string, object> outp = new Dictionary<string, object>();
                                        outp["tags"] = tags;
                                        outp["entry"] = added.IndexOf(args[2]);
                                        outp["chart"] = segments;
                                        output.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(outp)));

                                        break;
                                    }
                            }

                            // Write
                            output.Close();
                            built = true;

                            break;
                        }
                    case "segment":
                        {
                            if (args.Count == 0)
                            {
                                Console.Error.WriteLine("Missing arument for command 'segment', expected a variable name.");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            currentSegmentName = args[0];
                            currentSegment = new Segment();
                            if (Segments.ContainsKey(currentSegmentName))
                            {
                                Console.Error.WriteLine("Invalid arument for command 'segment', variable name was already used in this compiler session, check other files.");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            break;
                        }
                    case "endsegment":
                        {
                            if (currentSegmentName == null)
                            {
                                Console.Error.WriteLine("Cannot use endsegment without a opening segment command");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            Segments[currentSegmentName] = currentSegment;
                            currentSegment = null;
                            currentSegmentName = null;
                            if (injection.Count != 0)
                            {
                                var inj = injection[injection.Count - 1];
                                injection.RemoveAt(injection.Count - 1);
                                currentSegment = inj.oSeg;
                                currentSegmentName = inj.oSegName;
                                inj.cmdArgs.AddRange(args);
                                Handle(inj.cmd, inj.cmdArgs, "<INJECTED>");
                            }
                            break;
                        }
                    case "command":
                        {
                            if (currentSegmentName == null)
                            {
                                Console.Error.WriteLine("Cannot use '" + command + "' without a opening segment command");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            if (args.Count == 0)
                            {
                                Console.Error.WriteLine("Missing arument for command 'command', expected a command name.");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            currentSegment.command = args[0];
                            break;
                        }
                    case "type":
                        {
                            if (currentSegmentName == null)
                            {
                                Console.Error.WriteLine("Cannot use '" + command + "' without a opening segment command");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            if (args.Count == 0)
                            {
                                Console.Error.WriteLine("Missing arument for command 'type', expected a type name.");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            currentSegment.type = args[0];
                            break;
                        }
                    case "header":
                        {
                            if (currentSegmentName == null)
                            {
                                Console.Error.WriteLine("Cannot use '" + command + "' without a opening segment command");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            if (args.Count == 0)
                            {
                                Console.Error.WriteLine("Missing arument for command 'header', expected a header message.");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            currentSegment.header = args[0];
                            break;
                        }
                    case "payload":
                        {
                            if (currentSegmentName == null)
                            {
                                Console.Error.WriteLine("Cannot use '" + command + "' without a opening segment command");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            currentSegment.payload = new List<string>(args);
                            break;
                        }
                    case "branches":
                        {
                            if (currentSegmentName == null)
                            {
                                Console.Error.WriteLine("Cannot use '" + command + "' without a opening segment command");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            currentSegment.branches = new List<string>(args);
                            currentSegment.branchesLN = ln;
                            break;
                        }
                    case "jump":
                        {
                            if (currentSegmentName == null)
                            {
                                Console.Error.WriteLine("Cannot use '" + command + "' without a opening segment command");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            if (args.Count == 0)
                            {
                                Console.Error.WriteLine("Missing arument for command 'jump', expected a target segment.");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            currentSegment.jump = args[0];
                            currentSegment.jumpLN = ln;
                            break;
                        }
                    case "conditions":
                        {
                            if (currentSegmentName == null)
                            {
                                Console.Error.WriteLine("Cannot use '" + command + "' without a opening segment command");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }

                            // Parse conditions
                            currentSegment.conditions = new List<Condition>();
                            string conditionChain = "";
                            foreach (string con in args)
                            {
                                conditionChain += con.Replace(" ", "");
                            }
                            if (conditionChain == "")
                            {
                                Console.Error.WriteLine("Missing conditions for command 'conditions'");
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }

                            Condition current = new Condition();
                            currentSegment.conditions.Add(current);
                            bool isName = true;
                            bool isValue = false;
                            string currentVal = "";
                            current.memory = "";
                            for (int i = 0; i < conditionChain.Length; i++)
                            {
                                // Get character
                                char ch = conditionChain[i];

                                // Handle character
                                switch (ch)
                                {
                                    case '(':
                                        {
                                            if (!isName)
                                            {
                                                Console.Error.WriteLine("Unexpected character: '('");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }
                                            Condition c = new Condition();
                                            c.parent = current;
                                            current.checks.Add(c);
                                            current = c;
                                            current.memory = "";
                                            break;
                                        }
                                    case ')':
                                        {
                                            if (isValue)
                                            {
                                                try
                                                {
                                                    if (current.memory != null)
                                                        current.value = int.Parse(currentVal);
                                                }
                                                catch
                                                {
                                                    Console.Error.WriteLine("Invalid value: " + currentVal);
                                                    if (file != null)
                                                        Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                    return false;
                                                }
                                                currentVal = "";
                                            }
                                            if (current.parent == null)
                                            {
                                                Console.Error.WriteLine("Unexpected character: ')'");
                                                if (file != null) 
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }
                                            current = current.parent;
                                            current.memory = null;
                                            break;
                                        }
                                    case '|':
                                        {
                                            if (isName || conditionChain.Length <= i + 1 || conditionChain[i + 1] != '|')
                                            {
                                                Console.Error.WriteLine("Unexpected character: '|'");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }

                                            // Finish first
                                            if (isValue)
                                            {
                                                isName = true;
                                                isValue = false;
                                                if (current.checks.Count == 0)
                                                {
                                                    try
                                                    {
                                                        if (current.memory != null)
                                                            current.value = int.Parse(currentVal);
                                                    }
                                                    catch
                                                    {
                                                        Console.Error.WriteLine("Invalid value: " + currentVal);
                                                        if (file != null)
                                                            Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                        return false;
                                                    }
                                                }
                                                currentVal = "";
                                            }

                                            // 'Or' condition
                                            var lst = currentSegment.conditions;
                                            if (current.parent != null)
                                                lst = current.parent.checks;

                                            var p = current.parent;
                                            current = new Condition();
                                            current.parent = p;
                                            current.mode = ModeType.OR;
                                            current.memory = "";
                                            lst.Add(current);
                                            i += 1;

                                            break;
                                        }
                                    case '&':
                                        {
                                            if (isName || conditionChain.Length <= i + 1 || conditionChain[i + 1] != '&')
                                            {
                                                Console.Error.WriteLine("Unexpected character: '&'");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }

                                            // Finish first
                                            if (isValue)
                                            {
                                                isName = true;
                                                isValue = false;
                                                if (current.checks.Count == 0)
                                                {
                                                    try
                                                    {
                                                        if (current.memory != null)
                                                            current.value = int.Parse(currentVal);
                                                    }
                                                    catch
                                                    {
                                                        Console.Error.WriteLine("Invalid value: " + currentVal);
                                                        if (file != null)
                                                            Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                        return false;
                                                    }
                                                }
                                                currentVal = "";
                                            }

                                            // 'And' condition
                                            var lst = currentSegment.conditions;
                                            if (current.parent != null)
                                                lst = current.parent.checks;

                                            var p = current.parent;
                                            current = new Condition();
                                            current.parent = p;
                                            current.mode = ModeType.AND;
                                            current.memory = "";
                                            lst.Add(current);
                                            i += 1;

                                            break;
                                        }
                                    case '=':
                                        {
                                            if (!isName || conditionChain.Length <= i + 1 || conditionChain[i + 1] != '=')
                                            {
                                                Console.Error.WriteLine("Unexpected character: '='");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }

                                            if (current.memory == "")
                                            {
                                                Console.Error.WriteLine("Unexpected character: '='");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }

                                            current.type = ConditionType.EQUAL;
                                            isName = false;
                                            isValue = true;
                                            i += 1;
                                            break;
                                        }
                                    case '!':
                                        {
                                            if (!isName || conditionChain.Length <= i + 1 || conditionChain[i + 1] != '=')
                                            {
                                                Console.Error.WriteLine("Unexpected character: '!'");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }

                                            if (current.memory == "")
                                            {
                                                Console.Error.WriteLine("Unexpected character: '!'");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }

                                            current.type = ConditionType.NOT_EQUAL;
                                            isName = false;
                                            isValue = true;
                                            i += 1;
                                            break;
                                        }
                                    case '<':
                                        {
                                            if (!isName)
                                            {
                                                Console.Error.WriteLine("Unexpected character: '<'");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }

                                            if (current.memory == "")
                                            {
                                                Console.Error.WriteLine("Unexpected character: '<'");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }

                                            if (i + 1 < conditionChain.Length && conditionChain[i + 1] == '=')
                                            {
                                                i += 1;
                                                current.type = ConditionType.LESS_OR_EQUAL;
                                            }
                                            else if (i + 1 < conditionChain.Length && !Regex.Match(conditionChain[i + 1].ToString(), "[0-9\\-]").Success)
                                            {
                                                Console.Error.WriteLine("Unexpected character: '" + conditionChain[i + 1] + "'");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }
                                            else
                                                current.type = ConditionType.LESS_THAN;

                                            isName = false;
                                            isValue = true;
                                            break;
                                        }
                                    case '>':
                                        {
                                            if (!isName)
                                            {
                                                Console.Error.WriteLine("Unexpected character: '>'");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }

                                            if (current.memory == "")
                                            {
                                                Console.Error.WriteLine("Unexpected character: '>'");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }

                                            if (i + 1 < conditionChain.Length && conditionChain[i + 1] == '=')
                                            {
                                                i += 1;
                                                current.type = ConditionType.GREATER_OR_EQUAL;
                                            }
                                            else if (i + 1 < conditionChain.Length && !Regex.Match(conditionChain[i + 1].ToString(), "[0-9\\-]").Success)
                                            {
                                                Console.Error.WriteLine("Unexpected character: '" + conditionChain[i + 1] + "'");
                                                if (file != null)
                                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                                return false;
                                            }
                                            else
                                                current.type = ConditionType.GREATER_THAN;

                                            isName = false;
                                            isValue = true;
                                            break;
                                        }
                                    default:
                                        {
                                            if (isName)
                                                current.memory += ch;
                                            else if (isValue)
                                                currentVal += ch;
                                            break;
                                        }
                                }
                            }

                            // Finish up
                            if (isName)
                            {
                                Console.Error.WriteLine("Unterminated condition: " + current.memory);
                                if (file != null)
                                    Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                return false;
                            }
                            else if (isValue && current.checks.Count == 0)
                            {
                                isName = true;
                                isValue = false;
                                try
                                {
                                    if (current.memory != null)
                                        current.value = int.Parse(currentVal);
                                }
                                catch
                                {
                                    Console.Error.WriteLine("Invalid value: " + currentVal);
                                    if (file != null)
                                        Console.Error.WriteLine("    At [" + file + "]:" + ln);
                                    return false;
                                }
                                currentVal = "";
                            }
                            break;
                        }
                    default:
                        {
                            Console.Error.WriteLine("Unrecognized command: " + command + " (" + commandFull + ")");
                            if (file != null)
                                Console.Error.WriteLine("    At [" + file + "]:" + ln);
                            return false;
                        }
                }
                return true;
            }

            if (!built)
            {
                Console.Error.WriteLine("No files were built!");
                if (file != null)
                    Console.Error.WriteLine("    At " + file);
                return false;
            }

            // Return
            return true;
        }

        private static void WriteInt(int i, Stream strm)
        {
            byte[] d = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(d);
            strm.Write(d);
        }

        private static void WriteString(string str, Stream strm)
        {
            byte[] d = Encoding.UTF8.GetBytes(str);
            WriteInt(d.Length, strm);
            strm.Write(d);
        }
    }
}