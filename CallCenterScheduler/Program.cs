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

            var result = GenerateResult(WorkingEnvironment.NumOfTopGroups, WorkingEnvironment.NumOfTopCategories);

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
        foreach (var group in WorkingEnvironment.ListOfGroups)
        {
            for (int i = 0; i < group.PrerequisiteGroups.Count; i++)
            {
                var foundGroup = WorkingEnvironment.ListOfGroups.FirstOrDefault(g => g.Name == group.PrerequisiteGroups[i].Name && g.Category == group.PrerequisiteGroups[i].Category);

                if (foundGroup == null)
                    return GenerateResult(false, "Prerequisite '" + String.Join(",", group.PrerequisiteGroups[i].Category, group.PrerequisiteGroups[i].Name) + " is not found.");

                if (foundGroup.PrerequisiteGroups.Count > 0)
                    return GenerateResult(false, "Nested prerequisites at '" + String.Join(",", group.Category, group.Name) + "' are not supported.");

                group.PrerequisiteGroups[i] = foundGroup;
            }
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

        return GenerateResult(true, null, p_objGroup.PrerequisiteGroups.Where(p => !p.IsCompleted).ToList());
    }

    static string GenerateResult(int G, int C)
    {
        SortedSet<string> groupNames = new SortedSet<string>();
        var temp = WorkingEnvironment.ListOfGroups.OrderByDescending(g => g.RequiredTime).ToList();
        HashSet<string> categories = new HashSet<string>();

        for (int i = 0; i < temp.Count; i++)
        {
            if (groupNames.Count >= G) break;
            if (!categories.Contains(temp[i].Category) && categories.Count < C)
                categories.Add(temp[i].Category);
            else if (!categories.Contains(temp[i].Category) && categories.Count == C)
                continue;
            groupNames.Add(temp[i].Name);
        }

        var highestTime = WorkingEnvironment.Workers.OrderByDescending(w => w.NextAvailableTime).First();

        var topGroups = string.Empty;
        if (groupNames.Count > 0)
            topGroups = "," + String.Join(",", groupNames);

        return highestTime.NextAvailableTime.ToString() + topGroups;
    }

    #region QueryResult Methods
    static QueryResult GenerateResult(bool p_isValid)
    {
        return GenerateResult(p_isValid, null, null);
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
    public static List<Worker> Workers;
    public static List<Group> FinishedGroups;
    public static List<Group> ListOfGroups;
    public static int NumOfTopGroups = 3;
    public static int NumOfTopCategories = 1;
    public static int NumOfWorkers = 2;
    public static int WaitedTime;
}

public class Worker
{
    public int ID { get; set; }
    public Group? WorkingOn { get; set; }
    public int TimeSpent { get; set; }
    public int NextAvailableTime { get; set; }
    public List<Group> FinishedGroups = new List<Group>();
}

// Class for group of customers with conditions
public class Group
{
    public string Category { get; set; }
    public string Name { get; set; }
    public int RequiredTime { get; set; }
    public List<Group> PrerequisiteGroups = new List<Group>();
    public bool IsCompleted { get; set; }
    public int MinTimeBeforeStart { get; set; }
    public int StartTime { get; set; }
}

public class QueryResult
{
    public bool IsValidInput { get; set; }
    public string? ErrorMessage { get; set; }
    public object? Result { get; set; }
}