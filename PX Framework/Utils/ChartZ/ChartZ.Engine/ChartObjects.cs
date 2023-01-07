namespace ChartZ.Engine
{

    /// <summary>
    /// Chart condition
    /// </summary>
    public class ChartCondition
    {
        private ChartChain chain;
        private ChartCondition parent;
        private ConditionModeType mode;

        private int memory;
        private ConditionType type;
        private int value;

        private List<ChartCondition> checks = new List<ChartCondition>();

        public ChartCondition(ChartChain chain, ChartCondition? parent, ConditionModeType mode, int memory, ConditionType type, int value, List<ChartCondition> checks)
        {
            this.chain = chain;
            if (parent != null)
                this.parent = parent;
            this.mode = mode;
            this.memory = memory;
            this.type = type;
            this.value = value;
            this.checks = checks;
        }

        internal void SetParent(ChartCondition parent)
        {
            this.parent = parent;
        }

        public string MemoryKey
        {
            get
            {
                return chain.Tags[memory];
            }
        }

        public ConditionType Type
        {
            get
            {
                return type;
            }
        }

        public int Value
        {
            get
            {
                return value;
            }
        }

        public ChartCondition[] Checks
        {
            get
            {
                return checks.ToArray();
            }
        }

        public ConditionModeType Mode
        {
            get
            {
                return mode;
            }
        }

        public ChartCondition Parent
        {
            get
            {
                return parent;
            }
        }
    }

    /// <summary>
    /// Condition mode type
    /// </summary>
    public enum ConditionModeType
    {
        AND,
        OR
    }

    /// <summary>
    /// Condition type
    /// </summary>
    public enum ConditionType
    {
        EQUAL,
        NOT_EQUAL,

        GREATER_THAN,
        LESS_THAN,

        GREATER_OR_EQUAL,
        LESS_OR_EQUAL
    }

    /// <summary>
    /// Chart segment
    /// </summary>
    public class ChartSegment
    {
        private static Random rnd = new Random();
        private ChartChain Chain;

        private int _command = -1;
        private int _header = -1;
        private int _type = -1;
        private int _jump = -1;
        private int[] _payload = new int[0];
        private List<ChartCondition> _conditions = new List<ChartCondition>();
        private int[] _branches = new int[0];

        /// <summary>
        /// Instantiates the segment object
        /// </summary>
        /// <param name="parent">Parent chain</param>
        /// <param name="command">Command ID</param>
        /// <param name="payload">Payload IDs</param>
        /// <param name="header">Header ID</param>
        /// <param name="type">Type ID</param>
        /// <param name="conditions">Condition objects</param>
        /// <param name="jump">Jump ID</param>
        /// <param name="branches">Branch IDs</param>
        public ChartSegment(ChartChain parent, int command, int[] payload, int header = -1, int type = -1, List<ChartCondition> conditions = null, int jump = -1, int[]? branches = null)
        {
            Chain = parent;
            _command = command;
            _type = type;
            _header = header;
            _jump = jump;
            if (payload != null)
                _payload = payload;
            if (conditions != null)
                _conditions = conditions;
            if (branches != null)
                _branches = branches;
        }

        /// <summary>
        /// Retrieves the command ID
        /// </summary>
        public string Command
        {
            get
            {
                return Chain.Tags[_command];
            }
        }

        /// <summary>
        /// Retrieves the type ID or null if not set
        /// </summary>
        public string? Type
        {
            get
            {
                if (_type == -1)
                    return null;
                return Chain.Tags[_type];
            }
        }

        /// <summary>
        /// Retrieves the header or null if not set
        /// </summary>
        public string? Header
        {
            get
            {
                if (_header == -1)
                    return null;
                return Chain.Tags[_header];
            }
        }

        /// <summary>
        /// Retrieves the jump target segment or null if not set
        /// </summary>
        public ChartSegment? Jump
        {
            get
            {
                if (_jump == -1)
                    return null;
                return Chain.Segments[_jump];
            }
        }

        /// <summary>
        /// Retrieves command payload
        /// </summary>
        public string[] Payload
        {
            get
            {
                return _payload.Select(t => Chain.Tags[t]).ToArray();
            }
        }

        /// <summary>
        /// Retrieves the branches
        /// </summary>
        public ChartSegment[] Branches
        {
            get
            {
                return Chain.Segments.Where(t => _branches.Contains(Chain.Segments.IndexOf(t))).ToArray();
            }
        }

        /// <summary>
        /// Retrieves command conditions
        /// </summary>
        public ChartCondition[] Conditions
        {
            get
            {
                return _conditions.ToArray();
            }
        }

        /// <summary>
        /// Runs the chart object
        /// </summary>
        public bool Run(int matches = 0)
        {
            bool result = true;

            // Find handler
            if (Command != "jump" && Command != "memory" && Command != "random" && Command != "intelligent" && Command != "intelligentrandom")
            {
                bool found = false;
                foreach (ChartCommand handler in Chain._handlers.Where(t => t.CommandID == Command || t.CommandID == "*"))
                {
                    found = true;
                    if (!handler.Handle(Chain, this))
                        result = false;
                    else
                        break;
                }
                if (!found)
                    throw new ArgumentException("No command handler for '" + Command + "'");
            }
            else if (Command == "memory")
            {
                string scope = Payload[0];
                string memory = Payload[1];
                string d = Payload[2];
                if (d.StartsWith("+") || d.StartsWith("-"))
                    d = d.Substring(1);
                int value = int.Parse(d);
                if (Payload[2].StartsWith("+"))
                    if (scope == "local")
                        value += Chain.LocalMemory.GetValueOrDefault(Chain.TagsReverse[memory], 0);
                    else
                        value += Chain.GlobalMemory.GetValueOrDefault(Chain.TagsReverse[memory], 0);
                else if (Payload[2].StartsWith("-"))
                    if (scope == "local")
                        value = Chain.LocalMemory.GetValueOrDefault(Chain.TagsReverse[memory], 0) - value;
                    else
                        value = Chain.GlobalMemory.GetValueOrDefault(Chain.TagsReverse[memory], 0) - value;
                if (scope != "global" && scope != "local")
                    throw new ArgumentException("Invalid chart condition scope: " + scope);
                if (!Chain.TagsReverse.ContainsKey(memory))
                    throw new ArgumentException("Invalid chart condition memory ID: " + memory);
                if (scope == "local")
                    Chain.LocalMemory[Chain.TagsReverse[memory]] = value;
                else
                    Chain.GlobalMemory[Chain.TagsReverse[memory]] = value;
            }

            // Check jump
            if (Jump != null)
                Jump.Run();

            // Branch randomization
            if (Command == "random")
            {
                result = false;
                List<ChartSegment> attempts = new List<ChartSegment>();
                while (attempts.Count != Branches.Count())
                {
                    ChartSegment seg = Branches[rnd.Next(0, Branches.Count())];
                    if (!attempts.Contains(seg))
                        attempts.Add(seg);
                    int _matches = CheckConditions(seg);
                    if (_matches > 0)
                    {
                        if (seg.Run(_matches))
                        {
                            result = true;
                            break;
                        }
                    }
                }
                return result;
            }

            // Intelligent branch selection (most matches first)
            if (Command == "intelligent")
            {
                ChartSegment? last = null;
                int lastMatchCount = 0;

                foreach(ChartSegment seg in Branches)
                {
                    if (seg.Type == "pathway")
                    {
                        int _matches = CheckConditions(seg);
                        if (_matches > 0 && _matches > lastMatchCount)
                        {
                            lastMatchCount = _matches;
                            last = seg;
                        }
                    }
                }
                if (last != null)
                    return last.Run(lastMatchCount);
                return false;
            }

            // Basically the random method with intelligent method combined
            if (Command == "intelligentrandom")
            {
                ChartSegment? last = null;
                int lastMatchCount = 0;

                List<ChartSegment> attempts = new List<ChartSegment>();
                while (attempts.Count != Branches.Count())
                {
                    ChartSegment seg = Branches[rnd.Next(0, Branches.Count())];
                    if (!attempts.Contains(seg))
                        attempts.Add(seg);
                    if (seg.Type == "pathway")
                    {
                        int _matches = CheckConditions(seg);
                        if (_matches > 0 && _matches > lastMatchCount)
                        {
                            lastMatchCount = _matches;
                            last = seg;
                        }
                    }
                }
                if (last != null)
                    return last.Run(lastMatchCount);
                return false;

            }

            // Run pathways
            foreach (ChartSegment seg in Branches)
            {
                if (seg.Type == "pathway")
                {
                    int _matches = CheckConditions(seg);
                    if (_matches > 0)
                    {
                        seg.Run(_matches);
                    }
                }
            }

            int CheckConditions(ChartSegment seg)
            {
                return CheckConditions2(seg.Conditions);
            }

            int CheckConditions2(ChartCondition[] conditions) {
                int matches = 0;
                int cMatches = 0;

                bool lastValue = true;
                foreach (ChartCondition current in conditions) { 
                    bool success = false;
                    if (current.Checks.Length != 0)
                    {
                        int add = CheckConditions2(current.Checks);
                        cMatches += add;
                        if (add > 0)
                            success = true;
                    } else {
                        int memVal = 0;
                        if (Chain.LocalMemory.ContainsKey(Chain.TagsReverse[current.MemoryKey]))
                            memVal = Chain.LocalMemory[Chain.TagsReverse[current.MemoryKey]];
                        else if (Chain.GlobalMemory.ContainsKey(Chain.TagsReverse[current.MemoryKey]))
                            memVal = Chain.GlobalMemory[Chain.TagsReverse[current.MemoryKey]];

                        switch (current.Type) {
                            case ConditionType.EQUAL: {
                                    if (memVal == current.Value)
                                        success = true;
                                    break;
                                }
                            case ConditionType.NOT_EQUAL: {
                                    if (memVal != current.Value)
                                        success = true;
                                    break;
                                }
                            case ConditionType.GREATER_THAN: {
                                    if (memVal > current.Value)
                                        success = true;
                                    break;
                                }
                            case ConditionType.GREATER_OR_EQUAL: {
                                    if (memVal >= current.Value)
                                        success = true;
                                    break;
                                }
                           case ConditionType.LESS_THAN: {
                                    if (memVal > current.Value)
                                        success = true;
                                    break;
                                }
                            case ConditionType.LESS_OR_EQUAL: {
                                    if (memVal >= current.Value)
                                        success = true;
                                    break;
                                }
                        }

                        if (success)
                            cMatches++;
                    }

                    if (current.Mode == ConditionModeType.AND && (!success || !lastValue)) {
                        cMatches = 0;
                    } else if (current.Mode == ConditionModeType.OR && success) {
                        matches += cMatches;
                        cMatches = 0;
                    }

                    if (lastValue || current.Mode != ConditionModeType.AND)
                        lastValue = success;
                }

                matches += cMatches;
                return matches;
            }
            return result;
        }
    }

}