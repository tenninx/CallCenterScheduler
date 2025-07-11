using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
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

            var queryResult = ParseInput(input.ToString());

            if (!queryResult.IsValidInput)
            {
                Console.WriteLine("Error Message: " + queryResult.ErrorMessage + "\n");
                continue;
            }

            queryResult = LinkPrerequisites();

            if (!queryResult.IsValidInput)
            {
                Console.WriteLine("Error Message: " + queryResult.ErrorMessage + "\n");
                continue;
            }

            Init();

            Schedule();

            var result = GenerateResult();

            Console.WriteLine("Result: " + result);
            Console.WriteLine();
        }
    }

    static void Init()
    {
        WorkingEnvironment.Workers = new List<Worker>();
        WorkingEnvironment.FinishedGroups = new List<Group>();
        WorkingEnvironment.WaitedTime = 0;
    }

    /// <summary>
    /// Parse text input from console into data objects
    /// </summary>
    /// <param name="p_strInput">Text input from console</param>
    /// <returns>Parsed data object</returns>
    static QueryResult ParseInput(string p_strInput)
    {
        p_strInput = p_strInput.Replace("\\,", ",").Replace("\\;", ";").Trim();

        if (p_strInput.Contains("-"))
        {
            string[] settings = p_strInput.Substring(0, p_strInput.IndexOf("-")).Split(",");
            if (settings.Length != 3)
                return GenerateResult(false, "Invalid settings input. Expected format: G,C,N. Either specify all or none for using defaults.");

            int number;

            if (!int.TryParse(settings[0].Trim(), out number))
                return GenerateResult(false, "Invalid settings input for G. Only a numeric value is accepted.");
            WorkingEnvironment.NumOfTopGroups = number < 0 ? 0 : number;

            if (!int.TryParse(settings[1].Trim(), out number))
                return GenerateResult(false, "Invalid settings input for C. Only a numeric value is accepted.");
            WorkingEnvironment.NumOfTopCategories = number;

            if (!int.TryParse(settings[2].Trim(), out number))
                return GenerateResult(false, "Invalid settings input for N. Only a numeric value is accepted.");
            WorkingEnvironment.NumOfWorkers = number == 0 ? int.MaxValue : number;
        }

        p_strInput = p_strInput.Substring(p_strInput.IndexOf("-") + 1).Trim();

        string[] strGroups = p_strInput.Split(";");

        List<Group> objGroups = new List<Group>();

        foreach (var strGroup in strGroups)
        {
            string[] strGroupDetails = strGroup.Trim().Split(',');
            List<Group> objPrereqs = new List<Group>();

            if (strGroupDetails.Length < 3)
                return GenerateResult(false, "Invalid group of customers input at '" + strGroup + "'.");

            if (!int.TryParse(strGroupDetails[2], out int time))
                return GenerateResult(false, "Invalid required time input at '" + String.Join(",", strGroupDetails[0], strGroupDetails[1]) + "'.");

            if (strGroupDetails.Length > 3 && (strGroupDetails.Length - 3) % 2 != 0)
                return GenerateResult(false, "Invalid prerequisites input at '" + String.Join(",", strGroupDetails[0], strGroupDetails[1]) + "'.");
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

        WorkingEnvironment.ListOfGroups = objGroups;

        return GenerateResult(true);
    }

    private static QueryResult LinkPrerequisites()
    {
        for (int i = 0; i < WorkingEnvironment.ListOfGroups.Count; i++)
        {
            for (int j = 0; j < WorkingEnvironment.ListOfGroups[i].PrerequisiteGroups.Count; j++)
            {
                var foundGroup = WorkingEnvironment.ListOfGroups.FirstOrDefault(g => g.Name == WorkingEnvironment.ListOfGroups[i].PrerequisiteGroups[j].Name && g.Category == WorkingEnvironment.ListOfGroups[i].PrerequisiteGroups[j].Category);

                if (foundGroup == null)
                    return GenerateResult(false, "Prerequisite '" + String.Join(",", WorkingEnvironment.ListOfGroups[i].PrerequisiteGroups[j].Category, WorkingEnvironment.ListOfGroups[i].PrerequisiteGroups[j].Name) + " is not found.");

                if (foundGroup.PrerequisiteGroups.Count > 0)
                    return GenerateResult(false, "Nested prerequisites at '" + String.Join(",", WorkingEnvironment.ListOfGroups[i].Category, WorkingEnvironment.ListOfGroups[i].Name) + "' are not supported.");

                WorkingEnvironment.ListOfGroups[i].PrerequisiteGroups[j] = foundGroup;
            }

            WorkingEnvironment.ListOfGroups[i].PrerequisiteGroups = WorkingEnvironment.ListOfGroups[i].PrerequisiteGroups.OrderByDescending(p => p.RequiredTime).ToList();
        }

        return GenerateResult(true);
    }

    static void Schedule()
    {
        Queue<Group> p_objQueueOfGroups = new Queue<Group>(WorkingEnvironment.ListOfGroups.OrderBy(l => l.PrerequisiteGroups.Count));

        while (true)
        {
            var worker = GetWorker();

            var group = GetNextGroup(p_objQueueOfGroups, worker);
            if (group == null)
                break;

            AssignWorker(worker, group);
        }
    }

    static Worker GetWorker()
    {
        Worker worker;

        if (WorkingEnvironment.Workers.Count < WorkingEnvironment.NumOfWorkers)
        {
            worker = new Worker() { ID = WorkingEnvironment.Workers.Count };
            WorkingEnvironment.Workers.Add(worker);
        }
        else
        {
            worker = WorkingEnvironment.Workers.OrderBy(w => w.NextAvailableTime).Take(1).First();
            worker.WorkingOn = null;
        }

        return worker;
    }

    private static void AssignWorker(Worker p_objWorker, Group p_objGroup)
    {
        p_objWorker.WorkingOn = p_objGroup;

        if (p_objWorker.NextAvailableTime < p_objGroup.MinTimeBeforeStart)
        {
            WorkingEnvironment.WaitedTime += p_objGroup.MinTimeBeforeStart - p_objWorker.NextAvailableTime;
            p_objWorker.NextAvailableTime = p_objGroup.MinTimeBeforeStart;
        }

        p_objGroup.StartTime = p_objWorker.NextAvailableTime;
        p_objWorker.NextAvailableTime += p_objGroup.RequiredTime;

        p_objGroup.IsCompleted = true;
        p_objWorker.FinishedGroups.Add(p_objGroup);
        WorkingEnvironment.FinishedGroups.Add(p_objGroup);
    }

    private static Group? GetNextGroup(Queue<Group> p_objQueueOfGroups, Worker p_objWorker)
    {
        while (p_objQueueOfGroups.Count > 0)
        {
            Group objCurrentGroup = p_objQueueOfGroups.Dequeue();
            if (objCurrentGroup.IsCompleted)
                continue;

            if (objCurrentGroup.PrerequisiteGroups.Count > 0)
            {
                var result = GetPrerequisiteGroups(objCurrentGroup, p_objWorker);

                if (!result.IsValidInput)
                {
                    Console.WriteLine("Error Message: " + result.ErrorMessage);
                    return null;
                }

                if (((List<Group>)result.Result).Count > 0)
                {
                    p_objQueueOfGroups.Enqueue(objCurrentGroup);
                    return ((List<Group>)result.Result).First();
                }
            }

            return objCurrentGroup;
        }

        return null;
    }

    private static QueryResult GetPrerequisiteGroups(Group p_objGroup, Worker p_objWorker)
    {
        var finishedGroup = WorkingEnvironment.FinishedGroups.Find(x => x == p_objGroup.PrerequisiteGroups.First());
        if (finishedGroup != null)
            p_objGroup.MinTimeBeforeStart = finishedGroup.StartTime + finishedGroup.RequiredTime;
        else
        {
            int minTime = p_objWorker.NextAvailableTime + p_objGroup.PrerequisiteGroups.First().RequiredTime;
            if (p_objGroup.MinTimeBeforeStart < minTime)
                p_objGroup.MinTimeBeforeStart = minTime;
        }

        return GenerateResult(true, p_objGroup.PrerequisiteGroups.Where(p => !p.IsCompleted).ToList());
    }

    static string GenerateResult()
    {
        SortedSet<string> groupNames = new SortedSet<string>();
        var temp = WorkingEnvironment.ListOfGroups.OrderByDescending(g => g.RequiredTime).ToList();
        HashSet<string> categories = new HashSet<string>();

        for (int i = 0; i < temp.Count; i++)
        {
            if (groupNames.Count >= WorkingEnvironment.NumOfTopGroups) break;
            if (!categories.Contains(temp[i].Category) && categories.Count < WorkingEnvironment.NumOfTopGroups)
                categories.Add(temp[i].Category);
            else if (!categories.Contains(temp[i].Category) && categories.Count == WorkingEnvironment.NumOfTopCategories)
                continue;
            groupNames.Add(temp[i].Name);
        }

        var highestTime = WorkingEnvironment.Workers.OrderByDescending(w => w.NextAvailableTime).First();

        var topGroups = string.Empty;
        if (groupNames.Count > 0)
            topGroups = "," + String.Join(",", groupNames);

        return highestTime.NextAvailableTime.ToString() + topGroups;
    }

    #region QueryResult Generation Methods
    static QueryResult GenerateResult(bool p_isValid)
    {
        return GenerateResult(p_isValid, null, null);
    }

    static QueryResult GenerateResult(bool p_isValid, object? p_objResult = null)
    {
        return GenerateResult(p_isValid, null, p_objResult);
    }

    static QueryResult GenerateResult(bool p_isValid, string? p_errorMessage = null)
    {
        return GenerateResult(p_isValid, p_errorMessage, null);
    }

    static QueryResult GenerateResult(bool p_isValid, string? p_errorMessage = null, object? p_result = null)
    {
        return new QueryResult
        {
            IsValidInput = p_isValid,
            ErrorMessage = p_errorMessage,
            Result = p_result
        };
    }
    #endregion
}

/// <summary>
/// Temporary storage class for working environment data. Should be reset after each input.
/// </summary>
public static class WorkingEnvironment
{
    /// <summary>
    /// List of workers in the call center. Each worker can handle multiple groups but once calling a group of customers, 
    /// they cannot be assigned to another group until the current one is completed.
    /// </summary>
    public static List<Worker> Workers;

    /// <summary>
    /// List of groups of customers that are available for calling. This is initialized from the input data and contains 
    /// all groups with their categories, names, required times, and prerequisites.
    /// </summary>
    public static List<Group> ListOfGroups;

    /// <summary>
    /// List of groups that have been completed by any workers. This is used to track the progress of the call center 
    /// operations.
    /// </summary>
    public static List<Group> FinishedGroups;

    /// <summary>
    /// Number of top groups to return in the result. If set to 0, only the completion time is returned.
    /// </summary>
    public static int NumOfTopGroups = 3;

    /// <summary>
    /// Number of top categories for the top groups to return in the result. If set to 0, only the completion time is 
    /// returned.
    /// </summary>
    public static int NumOfTopCategories = 3;

    /// <summary>
    /// Number of workers in the call center. If set to 0, it means unlimited number of workers can be used.
    /// </summary>
    public static int NumOfWorkers = 2;

    /// <summary>
    /// Total time that workers had to wait for prerequisites to be completed before they could start processing their 
    /// assigned groups. For debugging purpose.
    /// </summary>
    public static int WaitedTime;
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
    public int NextAvailableTime { get; set; }

    /// <summary>
    /// List of groups that the worker has finished processing. For debugging purpose.
    /// </summary>
    public List<Group> FinishedGroups = new List<Group>();
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
    public int RequiredTime { get; set; }

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
    public int MinTimeBeforeStart { get; set; }

    /// <summary>
    /// Start time of the group processing, set when the worker is assigned to this group.
    /// </summary>
    public int StartTime { get; set; }
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