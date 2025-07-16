using System;
using System.Collections.Generic;
using System.Linq;

namespace CallCenterSchedulerNS
{
    class Program
    {
        /// <summary>
        /// Main method to start the call center scheduler application.
        /// </summary>
        public static void Main()
        {
            string? input;
            while (true)
            {
                Console.WriteLine(@"Input format: {G},{C},{N}-{input_data} as in:
G: Top number of groups (default is 3). Enter 0 to return only the completion time. Negative not accepted.
C: Top number of categories (default is 3). Enter 0 to return only the completion time. Negative not accepted.
N: Number of workers (default is 2). Enter 0 for unlimited number of workers. Negative not accepted.
input_data: Semicolon-separated data in the form ""category,group,time"" followed by zero or more prerequisites of the form "",category,group"", e.g. Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King
You need to specify all G, C and N at once or none by specifying {input_data} directly to use the default settings.
Example input with settings: 2,2,0-Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King
Example input using defaults: Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King
Leave it empty and press enter to terminate. Enter your input:");

                input = Console.ReadLine();

                if (string.IsNullOrEmpty(input)) break;

                CallCenterScheduler callCenterScheduler = new CallCenterScheduler();

                var queryResult = callCenterScheduler.ParseInput(input.ToString());

                if (!queryResult.IsValidInput)
                {
                    Console.WriteLine("Error Message: " + queryResult.ErrorMessage + "\n");
                    continue;
                }

                queryResult = callCenterScheduler.Start();
                if (!queryResult.IsValidInput)
                {
                    Console.WriteLine("Error Message: " + queryResult.ErrorMessage + "\n");
                    continue;
                }

                var result = callCenterScheduler.GenerateResult();

                Console.WriteLine("Result: " + result);
                Console.WriteLine();
            }
        }
    }

    public class CallCenterScheduler
    {
        WorkingStorage Global;

        /// <summary>
        /// Default constructor for the CallCenterScheduler class. Initializes the working environment.
        /// </summary>
        public CallCenterScheduler()
        {
            Init();
        }

        /// <summary>
        /// Initialize a new instance of the CallCenterScheduler class with given settings.
        /// </summary>
        /// <param name="p_intTopGroups">The maximum number of top groups to be considered. Must be greater than or equal to 0; values less than 0
        /// will default to 0.</param>
        /// <param name="p_intTopCategories">The maximum number of top categories to be considered. Must be greater than or equal to 0; values less than
        /// 0 will default to 0.</param>
        /// <param name="p_intWorker">The number of workers to be allocated. Values less than or equal to 0 will default to unlimited.</param>
        public CallCenterScheduler(int p_intTopGroups, int p_intTopCategories, int p_intWorker)
        {
            Init();
            Global.NumOfTopGroups = p_intTopGroups < 0 ? 0 : p_intTopGroups;
            Global.NumOfTopCategories = p_intTopCategories < 0 ? 0 : p_intTopCategories;
            Global.NumOfWorkers = p_intWorker <= 0 ? int.MaxValue : p_intWorker;
        }

        /// <summary>
        /// Initialize the working environment by resetting all static properties to their default values.
        /// </summary>
        public void Init()
        {
            Global = new WorkingStorage();

            Global.Workers = new List<Worker>();
            Global.FinishedGroups = new List<Group>();
            Global.WaitedTime = 0;
            Global.NumOfTopGroups = 3;
            Global.NumOfTopCategories = 3;
            Global.NumOfWorkers = 2;
        }

        /// <summary>
        /// Parse the input string to extract settings and groups of customers.
        /// </summary>
        /// <param name="p_strInput">Text input from console</param>
        /// <returns>QueryResult</returns>
        public QueryResult ParseInput(string p_strInput)
        {
            p_strInput = p_strInput.Trim();

            if (p_strInput.Contains("-"))
            {
                string[] settings = p_strInput.Substring(0, p_strInput.IndexOf("-")).Split(',');
                if (settings.Length != 3)
                    return QueryResultGenerator.Generate(false, "Invalid settings input. Expected format: G,C,N. Either specify all or none for using defaults.");

                int number;

                if (!int.TryParse(settings[0].Trim(), out number))
                    return QueryResultGenerator.Generate(false, "Invalid settings input for G. Only a numeric value is accepted.");
                Global.NumOfTopGroups = number < 0 ? 0 : number;

                if (!int.TryParse(settings[1].Trim(), out number))
                    return QueryResultGenerator.Generate(false, "Invalid settings input for C. Only a numeric value is accepted.");
                Global.NumOfTopCategories = number;

                if (!int.TryParse(settings[2].Trim(), out number))
                    return QueryResultGenerator.Generate(false, "Invalid settings input for N. Only a numeric value is accepted.");
                Global.NumOfWorkers = number == 0 ? int.MaxValue : number;
            }

            p_strInput = p_strInput.Substring(p_strInput.IndexOf("-") + 1).Trim();

            string[] strGroups = SplitCharacters(p_strInput, ';');

            List<Group> objGroups = new List<Group>();

            foreach (var strGroup in strGroups)
            {
                string[] strGroupDetails = SplitCharacters(strGroup.Trim(), ',');
                List<Group> objPrereqs = new List<Group>();

                if (strGroupDetails.Length < 3)
                    return QueryResultGenerator.Generate(false, "Invalid group of customers input at '" + strGroup + "'.");

                bool isValidDouble = double.TryParse(strGroupDetails[2], out double time);
                if (!isValidDouble)
                    return QueryResultGenerator.Generate(false, "Invalid required time input at '" + String.Join(",", strGroupDetails[0], strGroupDetails[1]) + "'.");
                else if (time < 0)
                    return QueryResultGenerator.Generate(false, "Invalid required time input at '" + String.Join(",", strGroupDetails[0], strGroupDetails[1]) + "'.");

                if (strGroupDetails.Length > 3 && (strGroupDetails.Length - 3) % 2 != 0)
                    return QueryResultGenerator.Generate(false, "Invalid prerequisites input at '" + String.Join(",", strGroupDetails[0], strGroupDetails[1]) + "'.");
                else if (strGroupDetails.Length > 3)
                {
                    for (int i = 3; i < strGroupDetails.Length; i++)
                    {
                        objPrereqs.Add(new Group
                        {
                            Category = strGroupDetails[i],
                            Name = strGroupDetails[++i]
                        });
                    }
                }

                Group objGroup = new Group
                {
                    Category = strGroupDetails[0],
                    Name = strGroupDetails[1],
                    RequiredTime = time,
                    PrerequisiteGroups = objPrereqs
                };

                objGroups.Add(objGroup);
            }

            Global.ListOfGroups = objGroups;

            var result = CheckDuplicatedEntries();
            if (result != null)
                return QueryResultGenerator.Generate(false, "Duplicated entry '" + String.Join(",", result.Category, result.Name) + "' found in the input data. Each group of customers should be unique by its name and category.");

            return LinkPrerequisites();
        }

        /// <summary>
        /// Split a string into an array of substrings based on the specified delimiter character while ignoring escaped delimiters with a backslash.
        /// </summary>
        /// <param name="p_strInput">The input string</param>
        /// <param name="p_strSplitChar">The delimiter</param>
        /// <returns>Array of substrings by splitting the input string</returns>
        private string[] SplitCharacters(string p_strInput, char p_strSplitChar)
        {
            List<string> words = new List<string>();
            int currentIndex = 0, endIndex = 0;

            while (currentIndex < p_strInput.Length)
            {
                endIndex = p_strInput.IndexOf(p_strSplitChar, currentIndex);

                if (endIndex != -1)
                {
                    while (p_strInput[endIndex - 1].Equals('\\') && endIndex < p_strInput.Length)
                    {
                        endIndex = p_strInput.IndexOf(p_strSplitChar, endIndex + 1);
                        if (endIndex == -1)
                            break;
                    }
                }

                if (endIndex == -1)
                    endIndex = p_strInput.Length;

                words.Add(p_strInput.Substring(currentIndex, endIndex - currentIndex));

                currentIndex = endIndex + 1;
            }

            return words.ToArray();
        }

        /// <summary>
        /// Check for duplicated entries in the list of groups. Each group should be unique by its name and category.
        /// </summary>
        /// <returns>Duplicated group of customers</returns>
        private Group? CheckDuplicatedEntries()
        {
            var dupGroup = Global.ListOfGroups.GroupBy(x => new { x.Name, x.Category }).Where(g => g.Count() > 1).FirstOrDefault();
            if (dupGroup != null)
                return dupGroup.First();

            return null;
        }

        /// <summary>
        /// Link prerequisites of each group to the actual group objects in the list.
        /// </summary>
        /// <returns>QueryResult</returns>
        private QueryResult LinkPrerequisites()
        {
            for (int i = 0; i < Global.ListOfGroups.Count; i++)
            {
                for (int j = 0; j < Global.ListOfGroups[i].PrerequisiteGroups.Count; j++)
                {
                    var foundGroup = Global.ListOfGroups.FirstOrDefault(g => g.Name == Global.ListOfGroups[i].PrerequisiteGroups[j].Name && g.Category == Global.ListOfGroups[i].PrerequisiteGroups[j].Category);

                    if (foundGroup == null)
                        return QueryResultGenerator.Generate(false, "Prerequisite '" + String.Join(",", Global.ListOfGroups[i].PrerequisiteGroups[j].Category, Global.ListOfGroups[i].PrerequisiteGroups[j].Name) + " is not found.");

                    Global.ListOfGroups[i].PrerequisiteGroups[j] = foundGroup;
                }

                Global.ListOfGroups[i].PrerequisiteGroups = Global.ListOfGroups[i].PrerequisiteGroups.OrderByDescending(p => p.RequiredTime).ToList();
            }

            return QueryResultGenerator.Generate(true);
        }

        /// <summary>
        /// Start scheduling the groups of customers to workers based on their prerequisites and required time.
        /// </summary>
        /// <returns>QueryResult</returns>
        public QueryResult Start()
        {
            Queue<Group> p_objQueueOfGroups = new Queue<Group>(Global.ListOfGroups.OrderBy(l => l.PrerequisiteGroups.Count));

            while (true)
            {
                var queryResult = GetNextGroup(p_objQueueOfGroups);

                if (!queryResult.IsValidInput)
                    return queryResult;

                var group = (Group)queryResult.Result;

                if (group == null)
                    break;

                AssignWorker(group);
            }

            return QueryResultGenerator.Generate(true, null);
        }

        /// <summary>
        /// Get an available worker from the pool of workers with its total number specified in the settings.
        /// </summary>
        /// <returns>Worker</returns>
        private Worker GetWorker()
        {
            Worker worker;

            if (Global.Workers.Count < Global.NumOfWorkers)
            {
                worker = new Worker() { ID = Global.Workers.Count };
                Global.Workers.Add(worker);
            }
            else
            {
                worker = Global.Workers.OrderBy(w => w.NextAvailableTime).Take(1).First();
                worker.WorkingOn = null;
            }

            return worker;
        }

        /// <summary>
        /// Assign a worker to a group of customers, updating the worker's next available time and the group's start time.
        /// </summary>
        /// <param name="p_objGroup">An assigned group of customers</param>
        private void AssignWorker(Group p_objGroup)
        {
            Worker p_objWorker = GetWorker();

            p_objWorker.WorkingOn = p_objGroup;

            if (p_objWorker.NextAvailableTime < p_objGroup.MinTimeBeforeStart)
            {
                double waitedTime = p_objGroup.MinTimeBeforeStart - p_objWorker.NextAvailableTime;
                p_objWorker.WaitedTime += waitedTime;
                Global.WaitedTime += waitedTime;
                p_objWorker.NextAvailableTime = p_objGroup.MinTimeBeforeStart;
            }

            p_objGroup.StartTime = p_objWorker.NextAvailableTime;
            p_objWorker.NextAvailableTime += p_objGroup.RequiredTime;

            p_objGroup.IsCompleted = true;
            p_objWorker.FinishedGroups.Add(p_objGroup);
            Global.FinishedGroups.Add(p_objGroup);
        }

        /// <summary>
        /// Get the next group of customers from the queue, checking if it has prerequisites that need to be completed first.
        /// </summary>
        /// <param name="p_objQueueOfGroups">Queue of groups of customers</param>
        /// <returns>QueryResult</returns>
        private QueryResult GetNextGroup(Queue<Group> p_objQueueOfGroups)
        {
            while (p_objQueueOfGroups.Count > 0)
            {
                Group objCurrentGroup = p_objQueueOfGroups.Dequeue();
                if (objCurrentGroup.IsCompleted)
                    continue;

                for (int i = 0; i < objCurrentGroup.PrerequisiteGroups.Count; i++)
                {
                    var queryResult = GetPrerequisiteGroups(objCurrentGroup.PrerequisiteGroups[i], 0);
                    if (!queryResult.IsValidInput)
                        return queryResult;

                    var result = (Group)queryResult.Result;
                    var finishedTime2 = result.StartTime + result.RequiredTime;
                    if (objCurrentGroup.MinTimeBeforeStart < finishedTime2)
                        objCurrentGroup.MinTimeBeforeStart = finishedTime2;
                }

                return QueryResultGenerator.Generate(true, objCurrentGroup);
            }

            return QueryResultGenerator.Generate(true, null);
        }

        /// <summary>
        /// Get the prerequisite groups for a given group, checking if they have been completed or if the worker can start them based on their next available time.
        /// </summary>
        /// <param name="p_objGroup">Group of customers to check prerequisites</param>
        /// <param name="p_intDepth">Current depth of recursion to prevent circular dependencies</param>
        /// <returns>QueryResult</returns>
        private QueryResult GetPrerequisiteGroups(Group p_objGroup, int p_intDepth)
        {
            if (p_intDepth > Global.MaxPrerequisiteDepth)
                return QueryResultGenerator.Generate(false, "Too many recursive calls detected (more than 255). Possible circular dependencies (deadlocks) in prerequisites.");

            Group group;
            double finishedTime;

            if (!p_objGroup.IsCompleted)
            {
                for (int i = 0; i < p_objGroup.PrerequisiteGroups.Count; i++)
                {
                    if (p_objGroup.PrerequisiteGroups[i].IsCompleted)
                    {
                        group = Global.FinishedGroups.Find(x => x == p_objGroup.PrerequisiteGroups[i]);
                        finishedTime = group.StartTime + group.RequiredTime;
                        if (p_objGroup.MinTimeBeforeStart < finishedTime)
                            p_objGroup.MinTimeBeforeStart = finishedTime;
                        continue;
                    }

                    var queryResult = GetPrerequisiteGroups(p_objGroup.PrerequisiteGroups[i], ++p_intDepth);
                    if (!queryResult.IsValidInput)
                        return queryResult;

                    group = (Group)queryResult.Result;

                    finishedTime = group.StartTime + group.RequiredTime;
                    if (p_objGroup.MinTimeBeforeStart < finishedTime)
                        p_objGroup.MinTimeBeforeStart = finishedTime;
                }

                AssignWorker(p_objGroup);
            }

            return QueryResultGenerator.Generate(true, p_objGroup);
        }

        /// <summary>
        /// Generate the final result string based on the completion time of the final group and the top groups/categories specified in the settings.
        /// </summary>
        /// <returns>Result in string format</returns>
        public string GenerateResult()
        {
            if (Global.Workers.Count == 0)
                return "Error Message: CallCenterScheduler should be started first. Execute Start() method before generating the result.";

            SortedSet<string> groupNames = new SortedSet<string>();
            var listOfGroups = Global.ListOfGroups.OrderByDescending(g => g.RequiredTime).ToList();
            HashSet<string> categories = new HashSet<string>();

            for (int i = 0; i < listOfGroups.Count; i++)
            {
                if (groupNames.Count >= Global.NumOfTopGroups) break;
                if (!categories.Contains(listOfGroups[i].Category) && categories.Count < Global.NumOfTopCategories)
                    categories.Add(listOfGroups[i].Category);
                else if (!categories.Contains(listOfGroups[i].Category) && categories.Count == Global.NumOfTopCategories)
                    continue;
                groupNames.Add(listOfGroups[i].Name);
            }

            var highestTime = Global.Workers.Max(obj => obj.NextAvailableTime);
            var highestGroups = Global.Workers.Where(obj => obj.NextAvailableTime == highestTime);

            string highestGroup;

            if (highestGroups.Count() == 1 || (highestGroups.Count() > 1 && (Global.NumOfTopGroups == 0 || Global.NumOfTopCategories == 0)))
            {
                highestGroup = GetTimeString(highestGroups.First().NextAvailableTime);

                var topGroups = string.Empty;
                if (groupNames.Count > 0)
                    topGroups = "," + String.Join(",", groupNames);

                return highestGroup + topGroups;
            }

            return "ERROR - tie (group)";
        }

        private string GetTimeString(double p_dblTime)
        {
            return p_dblTime.ToString("0.######");
        }

        /// <summary>
        /// Temporary storage class for working environment data. Should be reset after each input.
        /// </summary>
        private class WorkingStorage
        {
            /// <summary>
            /// List of workers in the call center. Each worker can handle multiple groups but once calling a group of customers, 
            /// they cannot be assigned to another group until the current one is completed.
            /// </summary>
            public List<Worker> Workers;

            /// <summary>
            /// List of groups of customers that are available for calling. This is initialized from the input data and contains 
            /// all groups with their categories, names, required times, and prerequisites.
            /// </summary>
            public List<Group> ListOfGroups;

            /// <summary>
            /// List of groups that have been completed by any workers. This is used to track the progress of the call center 
            /// operations.
            /// </summary>
            public List<Group> FinishedGroups;

            /// <summary>
            /// Number of top groups to return in the result. If set to 0, only the completion time is returned.
            /// </summary>
            public int NumOfTopGroups = 3;

            /// <summary>
            /// Number of top categories for the top groups to return in the result. If set to 0, only the completion time is 
            /// returned.
            /// </summary>
            public int NumOfTopCategories = 3;

            /// <summary>
            /// Number of workers in the call center. If set to 0, it means unlimited number of workers can be used.
            /// </summary>
            public int NumOfWorkers = 2;

            /// <summary>
            /// The maximum depth of prerequisites that can be processed. Any more than this depth will be deemed a deadlock 
            /// and the application will break.
            /// </summary>
            public int MaxPrerequisiteDepth = 255;

            /// <summary>
            /// Total time that workers had to wait for prerequisites to be completed before they could start processing their 
            /// assigned groups. For debugging purpose.
            /// </summary>
            public double WaitedTime;
        }
    }

    /// <summary>
    /// Class for generating a QueryResult object with the specified parameters.
    /// </summary>
    public static class QueryResultGenerator
    {
        public static QueryResult Generate(bool p_isValid)
        {
            return Generate(p_isValid, null, null);
        }

        public static QueryResult Generate(bool p_isValid, object? p_objResult = null)
        {
            return Generate(p_isValid, null, p_objResult);
        }

        public static QueryResult Generate(bool p_isValid, string? p_errorMessage = null)
        {
            return Generate(p_isValid, p_errorMessage, null);
        }

        /// <summary>
        /// Generate a QueryResult object with the specified parameters.
        /// </summary>
        /// <param name="p_isValid">A flag to determine if the query was valid</param>
        /// <param name="p_errorMessage">A error message to return</param>
        /// <param name="p_result">An object, if any, resulted from the query</param>
        /// <returns>QueryResult</returns>
        public static QueryResult Generate(bool p_isValid, string? p_errorMessage = null, object? p_result = null)
        {
            return new QueryResult
            {
                IsValidInput = p_isValid,
                ErrorMessage = p_errorMessage,
                Result = p_result
            };
        }
    }

    /// <summary>
    /// Worker class representing a worker in the call center.
    /// </summary>
    public class Worker
    {
        /// <summary>
        /// Unique identifier for the worker. For debugging purpose.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Group of customers that the worker is currently calling. For debugging purpose.
        /// </summary>
        public Group? WorkingOn { get; set; }

        /// <summary>
        /// Next available time for the worker to call the next group of customers.
        /// </summary>
        public double NextAvailableTime { get; set; }

        /// <summary>
        /// List of groups that the worker has finished processing. For debugging purpose.
        /// </summary>
        public List<Group> FinishedGroups = new List<Group>();

        /// <summary>
        /// Total idling time of the worker waiting for the next group. For debugging purpose.
        /// </summary>
        public double WaitedTime;
    }

    /// <summary>
    /// Group class representing a group of customers for a worker to call.
    /// </summary>
    public class Group
    {
        /// <summary>
        /// Category of the group of customers.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Name of the group of customers.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Required time to process this group.
        /// </summary>
        public double RequiredTime { get; set; }

        /// <summary>
        /// List of prerequisite groups that must be completed before this group can be processed.
        /// </summary>
        public List<Group> PrerequisiteGroups = new List<Group>();

        /// <summary>
        /// Indicates whether the group has been completed by a worker.
        /// </summary>
        public bool IsCompleted { get; set; }

        /// <summary>
        /// Minimum time before this group can start processing, based on prerequisites.
        /// </summary>
        public double MinTimeBeforeStart { get; set; }

        /// <summary>
        /// Start time of the group processing, set when the worker is assigned to this group.
        /// </summary>
        public double StartTime { get; set; }
    }

    /// <summary>
    /// QueryResult class to encapsulate the result of a query.
    /// </summary>
    public class QueryResult
    {
        /// <summary>
        /// Indicates whether the query is successful.
        /// </summary>
        public bool IsValidInput { get; set; }

        /// <summary>
        /// Error message if the query is not valid.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Result of the query, can be any object type.
        /// </summary>
        public object? Result { get; set; }
    }
}