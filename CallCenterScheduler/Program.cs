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

            var parsedInput = ParseInput(input.ToString());

            if (!parsedInput.IsValidInput)
            {
                Console.WriteLine("Error Message: " + parsedInput.ErrorMessage);
            }
            else
            {
                Workers = new List<Worker>();
                FinishedGroups = new List<Group>();

                Schedule();

                var result = GenerateResult(numOfTopGroups, numOfTopCategories);

                Console.WriteLine("Result: " + result);
            }
        }
    }

    /// <summary>
    /// Parse text input from console into data objects
    /// </summary>
    /// <param name="p_strInput">Text input from console</param>
    /// <returns>Parsed data object</returns>
    static ParsedInput ParseInput(string p_strInput)
    {
        var objResult = new ParsedInput();

        string[] strGroups = p_strInput.Split(';');

        List<Group> objGroups = new List<Group>();

        foreach (var strGroup in strGroups)
        {
            string[] strGroupDetails = strGroup.Split(',');
            List<Group> objPrereqs = new List<Group>();

            if (strGroupDetails.Length < 3)
                return new ParsedInput { ErrorMessage = "Invalid group of customers input." };

            if (!int.TryParse(strGroupDetails[2], out int time))
                return new ParsedInput { ErrorMessage = "Invalid required time input." };

            if (strGroupDetails.Length > 3 && (strGroupDetails.Length - 3) % 2 != 0)
                return new ParsedInput { ErrorMessage = "Invalid prerequisites input." };
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

        // Check circular recursive
        //var result = objGroups.SelectManyRecursive(l => l.PrerequisiteGroups).Select(l => l.Name).ToList();

        objResult.IsValidInput = true;

        ListOfGroups = objGroups;

        return objResult;
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

                if (result.GroupOfCustomers.Count > 0)
                {
                    p_objQueueOfGroups.Enqueue(objCurrentGroup);
                    return result.GroupOfCustomers.First();
                }
            }

            return objCurrentGroup;
        }

        return null;
    }

    private static ParsedInput GetPrerequisiteGroups(Group p_objGroup, Worker p_objWorker)
    {
        ParsedInput result = new ParsedInput();

        var objPrereqs = ListOfGroups.Join(p_objGroup.PrerequisiteGroups, e => new { e.Name, e.Category }, d => new { d.Name, d.Category }, (tbl1, tbl2) => tbl1).OrderByDescending(j => j.RequiredTime).ToList();

        // No nested prerequisites
        if (objPrereqs.Where(p => p.PrerequisiteGroups.Count > 0).Count() > 0)
        {
            result.ErrorMessage = "Nested prerequisites are not supported.";
            return result;
        }

        var finishedGroup = FinishedGroups.Find(x => x == objPrereqs.First());
        if (finishedGroup != null)
        {
            p_objGroup.MinTimeBeforeStart = finishedGroup.StartTime + finishedGroup.RequiredTime;
        }
        else
        {
            int minTime = p_objWorker.NextAvailableTime + objPrereqs.First().RequiredTime;
            if (p_objGroup.MinTimeBeforeStart < minTime)
                p_objGroup.MinTimeBeforeStart = minTime;
        }

        result.IsValidInput = true;
        result.GroupOfCustomers = objPrereqs.Where(p => !p.IsCompleted).ToList();

        return result;
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
        FinishedGroups.Add(p_objGroup);
        // Add finish time
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

public static class Extensions
{
    public static IEnumerable<T> SelectManyRecursive<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> selector)
    {
        var result = source.SelectMany(selector);
        if (!result.Any())
        {
            return result;
        }
        return result.Concat(result.SelectManyRecursive(selector));
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

public class ParsedInput
{
    public bool IsValidInput { get; set; }
    public string? ErrorMessage { get; set; }
    public List<Group> GroupOfCustomers { get; set; } = new List<Group>();
}