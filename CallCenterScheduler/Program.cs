using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static List<Worker> Workers;
    static List<Group> FinishedGroups;
    static List<Group> ListOfGroups;
    static int NumOfWorkers;
    static int WaitedTime;

    public static void Main()
    {
        // G
        int numOfTopGroups = 2;
        // C
        int numOfTopCategories = 2;
        // N
        NumOfWorkers = 3;

        string? input;
        while (true)
        {
            Console.WriteLine("Enter the input in the following format: A string of semicolon-separated data in the form \"category,group,time\" followed by zero or more prerequisites of the form \",category,group\", empty input to terminate:");
            input = Console.ReadLine();

            if (string.IsNullOrEmpty(input)) break;

            var queryResult = ParseInput(input.ToString());

            if (!queryResult.IsValidInput)
            {
                Console.WriteLine("Error Message: " + queryResult.ErrorMessage);
                continue;
            }

            queryResult = LinkPrerequisites();

            if (!queryResult.IsValidInput)
            {
                Console.WriteLine("Error Message: " + queryResult.ErrorMessage);
                continue;
            }

            Workers = new List<Worker>();
            FinishedGroups = new List<Group>();

            Schedule();

            var result = GenerateResult(numOfTopGroups, numOfTopCategories);

            Console.WriteLine("Result: " + result);
        }
    }

    private static QueryResult LinkPrerequisites()
    {
        foreach (var group in ListOfGroups)
        {
            for (int i = 0; i < group.PrerequisiteGroups.Count; i++)
            {
                var foundGroup = ListOfGroups.FirstOrDefault(g => g.Name == group.PrerequisiteGroups[i].Name && g.Category == group.PrerequisiteGroups[i].Category);

                if (foundGroup == null)
                    return GenerateResult(false, "Prerequisite '" + String.Join(',', group.PrerequisiteGroups[i].Category, group.PrerequisiteGroups[i].Name) + " is not found.");

                if (foundGroup.PrerequisiteGroups.Count > 0)
                    return GenerateResult(false, "Nested prerequisites at " + String.Join(',', group.Category, group.Name) + " are not supported.");

                group.PrerequisiteGroups[i] = foundGroup;
            }
        }

        return GenerateResult(true);
    }

    /// <summary>
    /// Parse text input from console into data objects
    /// </summary>
    /// <param name="p_strInput">Text input from console</param>
    /// <returns>Parsed data object</returns>
    static QueryResult ParseInput(string p_strInput)
    {
        string[] strGroups = p_strInput.Split(';');

        List<Group> objGroups = new List<Group>();

        foreach (var strGroup in strGroups)
        {
            string[] strGroupDetails = strGroup.Split(',');
            List<Group> objPrereqs = new List<Group>();

            if (strGroupDetails.Length < 3)
                return GenerateResult(false, "Invalid group of customers input at '" + strGroup + "'.");

            if (!int.TryParse(strGroupDetails[2], out int time))
                return GenerateResult(false, "Invalid required time input at '" + String.Join(',', strGroupDetails[0], strGroupDetails[1]) + "'.");

            if (strGroupDetails.Length > 3 && (strGroupDetails.Length - 3) % 2 != 0)
                return GenerateResult(false, "Invalid prerequisites input at '" + String.Join(',', strGroupDetails[0], strGroupDetails[1]) + "'.");
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

        ListOfGroups = objGroups;

        return GenerateResult(true, null, objGroups);
    }

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

    static void Schedule()
    {
        Queue<Group> p_objQueueOfGroups = new Queue<Group>(ListOfGroups.OrderBy(l => l.PrerequisiteGroups.Count));

        while (true)
        {
            var worker = GetWorker();

            var group = GetNextGroup(p_objQueueOfGroups, worker);
            if (group == null)
                break;

            AssignWorker(worker, group);
        }
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
        var objPrereqs = ListOfGroups.Join(p_objGroup.PrerequisiteGroups, e => new { e.Name, e.Category }, d => new { d.Name, d.Category }, (tbl1, tbl2) => tbl1).OrderByDescending(j => j.RequiredTime).ToList();

        var finishedGroup = FinishedGroups.Find(x => x == objPrereqs.First());
        if (finishedGroup != null)
            p_objGroup.MinTimeBeforeStart = finishedGroup.StartTime + finishedGroup.RequiredTime;
        else
        {
            int minTime = p_objWorker.NextAvailableTime + objPrereqs.First().RequiredTime;
            if (p_objGroup.MinTimeBeforeStart < minTime)
                p_objGroup.MinTimeBeforeStart = minTime;
        }

        return GenerateResult(true, null, objPrereqs.Where(p => !p.IsCompleted).ToList());
    }

    static Worker GetWorker()
    {
        Worker worker;

        if (Workers.Count < NumOfWorkers)
        {
            worker = new Worker() { ID = Workers.Count };
            Workers.Add(worker);
        }
        else
        {
            worker = Workers.OrderBy(w => w.NextAvailableTime).Take(1).First();
            worker.WorkingOn = null;
        }

        return worker;
    }

    private static void AssignWorker(Worker p_objWorker, Group p_objGroup)
    {
        p_objWorker.WorkingOn = p_objGroup;

        if (p_objWorker.NextAvailableTime < p_objGroup.MinTimeBeforeStart)
        {
            WaitedTime += p_objGroup.MinTimeBeforeStart - p_objWorker.NextAvailableTime;
            p_objWorker.NextAvailableTime = p_objGroup.MinTimeBeforeStart;
        }

        p_objGroup.StartTime = p_objWorker.NextAvailableTime;
        p_objWorker.NextAvailableTime += p_objGroup.RequiredTime;

        p_objGroup.IsCompleted = true;
        p_objWorker.FinishedGroups.Add(p_objGroup);
        FinishedGroups.Add(p_objGroup);
    }

    static string GenerateResult(int G, int C)
    {
        SortedSet<string> groupNames = new SortedSet<string>();
        var temp = ListOfGroups.OrderByDescending(g => g.RequiredTime).ToList();
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

        var highestTime = Workers.OrderByDescending(w => w.NextAvailableTime).First();

        var topGroups = string.Empty;
        if (groupNames.Count > 0)
            topGroups = "," + String.Join(",", groupNames);

        return highestTime.NextAvailableTime.ToString() + topGroups;
    }
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