using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ChartZ.Engine {

    internal class ImplChartChain : ChartChain
    {
        protected override ChartChain NewInstance()
        {
            return new ImplChartChain();
        }
    }

    /// <summary>
    /// Chart chain object
    /// </summary>
    public abstract class ChartChain {

        protected static ChartChain Implementation = new ImplChartChain();

        /// <summary>
        /// Creates a new chart chain
        /// </summary>
        /// <returns>ChartChain instance</returns>
        public static ChartChain Create()
        {
            return Implementation.NewInstance();
        }

        /// <summary>
        /// Creates a new chart chain instance
        /// </summary>
        /// <returns>ChartChain instance</returns>
        protected abstract ChartChain NewInstance();

        private class ChartData {
            public int entry;
            public string[] tags;
            public List<Dictionary<string, object>> chart;
        }

        private int _entry = -1;
        private Dictionary<int, string> _tags = new Dictionary<int, string>();
        private Dictionary<string, int> _tagsV = new Dictionary<string, int>();
        private Dictionary<int, int> _localMemory = new Dictionary<int, int>();
        private Dictionary<int, int> _globalMemory = new Dictionary<int, int>();
        private List<ChartSegment> _segments = new List<ChartSegment>();
        internal List<ChartCommand> _handlers = new List<ChartCommand>();

        /// <summary>
        /// Registers a new chart command
        /// </summary>
        /// <param name="cmd">Command object</param>
        public void RegisterCommand(ChartCommand cmd)
        {
            _handlers.Add(cmd);
        }

        /// <summary>
        /// Clones this chart chain and links global memory
        /// </summary>
        /// <returns>Cloned chart chain</returns>
        public ChartChain Clone() {
            ChartChain chain = NewInstance();
            chain._entry = _entry;
            chain._tags = new Dictionary<int, string>(_tags);
            chain._globalMemory = _globalMemory;
            chain._localMemory = new Dictionary<int, int>(_localMemory);
            chain._segments = new List<ChartSegment>(_segments);
            chain._handlers = new List<ChartCommand>(_handlers);
            return chain;
        }

        /// <summary>
        /// Loads a chart json
        /// </summary>
        /// <param name="chart">Chart json</param>
        public void Load(string chart) {
            ChartData data = JsonConvert.DeserializeObject<ChartData>(chart);
            _entry = data.entry;
            int i = 0;
            foreach (string v in data.tags) {
                _tagsV[v] = i;
                _tags[i++] = v;
            }
            foreach (Dictionary<string, object> seg in data.chart) {
                int cmd = (int)(long)seg[_tagsV["command"].ToString()];
                int type = (int)(long)seg.GetValueOrDefault(_tagsV["type"].ToString(), -1l);
                int jump = (int)(long)seg.GetValueOrDefault(_tagsV["jump"].ToString(), -1l);
                int header = (int)(long)seg.GetValueOrDefault(_tagsV["header"].ToString(), -1l);
                int[] payload = new int[0];
                int[] branches = new int[0];
                int[] conditions = new int[0];
                if (seg.ContainsKey(_tagsV["payload"].ToString()))
                    payload = ((JArray)seg[_tagsV["payload"].ToString()]).Select(t => (int)t).ToArray();
                if (seg.ContainsKey(_tagsV["branches"].ToString()))
                    branches = ((JArray)seg[_tagsV["branches"].ToString()]).Select(t => (int)t).ToArray();
                if (seg.ContainsKey(_tagsV["conditions"].ToString()))
                    conditions = ((JArray)seg[_tagsV["conditions"].ToString()]).Select(t => (int)t).ToArray();

                // Read conditions
                int ind = 0;
                List<ChartCondition> chartConditions = ReadConditions();
                List<ChartCondition> ReadConditions(int limit = -1)
                {
                    int i2 = 0;
                    List<ChartCondition> chartConditions = new List<ChartCondition>();
                    while (ind < conditions.Length && (limit == -1 || i2 < limit))
                    {
                        ConditionModeType mode = Tags[conditions[ind++]] == "or" ? ConditionModeType.OR : ConditionModeType.AND;
                        int children = conditions[ind++];

                        if (children == 0)
                        {
                            // Read single condition
                            int type = conditions[ind++];
                            ConditionType cType = ConditionType.EQUAL;
                            switch (type)
                            {
                                case 0:
                                    cType = ConditionType.EQUAL;
                                    break;
                                case 1:
                                    cType = ConditionType.NOT_EQUAL;
                                    break;
                                case 2:
                                    cType = ConditionType.GREATER_THAN;
                                    break;
                                case 3:
                                    cType = ConditionType.GREATER_OR_EQUAL;
                                    break;
                                case 4:
                                    cType = ConditionType.LESS_THAN;
                                    break;
                                case 5:
                                    cType = ConditionType.LESS_OR_EQUAL;
                                    break;
                            }
                            int memory = conditions[ind++];
                            int value = conditions[ind++];
                            ChartCondition cond = new ChartCondition(this, null, mode, memory, cType, value, new List<ChartCondition>());
                            chartConditions.Add(cond);
                        }
                        else
                        {
                            // Read conditions
                            List<ChartCondition> childConditions = ReadConditions(children);
                            ChartCondition cond = new ChartCondition(this, null, mode, -1, ConditionType.EQUAL, -1, childConditions);
                            childConditions.ForEach(t => t.SetParent(cond));
                            chartConditions.Add(cond);
                        }

                        i2++;
                    }
                    return chartConditions;
                }

                ChartSegment segment = new ChartSegment(this, cmd, payload, header, type, chartConditions, jump, branches);
                Segments.Add(segment);
            }
        }

        /// <summary>
        /// Loads a chart from a binary stream
        /// </summary>
        /// <param name="chart">Chart stream</param>
        public void Load(Stream chart) {
            // Load tags
            string ReadString() {
                int l = ReadInt();
                byte[] d = new byte[l];
                chart.Read(d);
                return Encoding.UTF8.GetString(d);
            }
            int ReadInt() {
                byte[] d = new byte[4];
                chart.Read(d);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(d);
                return BitConverter.ToInt32(d, 0);
            }

            // Load entry
            _entry = ReadInt();

            // Load tags
            int l = ReadInt();
            for (int i = 0; i < l; i++) {
                _tags[i] = ReadString();
                _tagsV[_tags[i]] = i;
            }

            // Load chart
            l = ReadInt();
            for (int i = 0; i < l; i++) {
                int cmd = ReadInt();
                int type = ReadInt();
                int jump = ReadInt();
                int header = ReadInt();
                int[] payload = new int[ReadInt()];
                for (int i2 = 0; i2 < payload.Length; i2++)
                    payload[i2] = ReadInt();
                int[] branches = new int[ReadInt()];
                for (int i2 = 0; i2 < branches.Length; i2++)
                    branches[i2] = ReadInt();

                // Read conditions
                List<ChartCondition> conditions = ReadConditions(ReadInt());
                List<ChartCondition> ReadConditions(int length)
                {
                    List<ChartCondition> conditions = new List<ChartCondition>();
                    for (int i = 0; i < length; i++)
                    {
                        // Read child condition count
                        ConditionModeType mode = chart.ReadByte() == 1 ? ConditionModeType.AND : ConditionModeType.OR;
                        int children = ReadInt();
                        if (children == 0)
                        {
                            // Read single condition
                            int type = chart.ReadByte();
                            ConditionType cType = ConditionType.EQUAL;
                            switch (type)
                            {
                                case 0:
                                    cType = ConditionType.EQUAL;
                                    break;
                                case 1:
                                    cType = ConditionType.NOT_EQUAL;
                                    break;
                                case 2:
                                    cType = ConditionType.GREATER_THAN;
                                    break;
                                case 3:
                                    cType = ConditionType.GREATER_OR_EQUAL;
                                    break;
                                case 4:
                                    cType = ConditionType.LESS_THAN;
                                    break;
                                case 5:
                                    cType = ConditionType.LESS_OR_EQUAL;
                                    break;
                            }
                            int memory = ReadInt();
                            int value = ReadInt();
                            ChartCondition cond = new ChartCondition(this, null, mode, memory, cType, value, new List<ChartCondition>());
                            conditions.Add(cond);
                        }
                        else
                        {
                            // Read conditions
                            List<ChartCondition> childConditions = ReadConditions(children);
                            ChartCondition cond = new ChartCondition(this, null, mode, -1, ConditionType.EQUAL, -1, childConditions);
                            childConditions.ForEach(t => t.SetParent(cond));
                            conditions.Add(cond);
                        }
                    }
                    return conditions;
                }

                ChartSegment segment = new ChartSegment(this, cmd, payload, header, type, conditions, jump, branches);
                Segments.Add(segment);
            }
        }

        /// <summary>
        /// Retrieves local memory
        /// </summary>
        public Dictionary<int, int> LocalMemory {
            get {
                return _localMemory;
            }
        }

        /// <summary>
        /// Retrieves global memory
        /// </summary>
        public Dictionary<int, int> GlobalMemory {
            get {
                return _globalMemory;
            }
        }

        /// <summary>
        /// Retrieves tag memory
        /// </summary>
        public Dictionary<int, string> Tags {
            get {
                return _tags;
            }
        }

        /// <summary>
        /// Retrieves reverse tag memory
        /// </summary>
        public Dictionary<string, int> TagsReverse {
            get {
                return _tagsV;
            }
        }

        /// <summary>
        /// Retrieves chart segments
        /// </summary>
        public List<ChartSegment> Segments {
            get {
                return _segments;
            }
        }

        /// <summary>
        /// Retrieves the entry segment or null
        /// </summary>
        public ChartSegment? EntrySegment {
            get {
                if (_entry == -1)
                    return null;
                return _segments[_entry];
            }
        }

    }

}