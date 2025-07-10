using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static int CurrentTime;
    static Worker[] Workers;

    public static void Main()
    {
        // G
        int numOfTopGroups = 2;
        // C
        int numOfTopCategories = 2;
        // N
        int numOfWorkers = 3;

        string? input;
        while (true)
        {
            CurrentTime = 0;
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
                Schedule(parsedInput.GroupOfCustomers, numOfWorkers);

                var result = GenerateResult(numOfTopGroups, numOfTopCategories, parsedInput.GroupOfCustomers);

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
            List<GroupBase> objPrereqs = new List<GroupBase>();

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
                    objPrereqs.Add(new GroupBase
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

        objResult.IsValidInput = true;
        objResult.GroupOfCustomers = objGroups;

        return objResult;
    }

    static void Schedule(List<Group> p_objListOfGroups, int N)
    {
        Workers = InitWorkers(N);

        Queue<Group> p_objQueueOfGroups = new Queue<Group>(p_objListOfGroups.OrderBy(g => g.PrerequisiteGroups.Count));

        while (p_objQueueOfGroups.Count > 0)
        {
            Group objCurrentGroup;
            do
            {
                objCurrentGroup = p_objQueueOfGroups.Dequeue();
            } while (objCurrentGroup.IsCompleted);

            StartCalling(p_objListOfGroups, p_objQueueOfGroups, objCurrentGroup);
        }
    }

    static void StartCalling(List<Group> p_objListOfGroups, Queue<Group> p_objQueueOfGroups, Group p_objGroup)
    {
        var objPrereqs = p_objListOfGroups.Where(g => !g.IsCompleted).Join(p_objGroup.PrerequisiteGroups, e => new { e.Name, e.Category }, d => new { d.Name, d.Category }, (tbl1, tbl2) => tbl1);

        if (objPrereqs.Count() > 0)
        {
            int max = objPrereqs.Max(a => a.RequiredTime);
            p_objGroup.MinTimeBeforeStart = CurrentTime + max;
            p_objQueueOfGroups.Enqueue(p_objGroup);
            foreach (Group group in objPrereqs)
                StartCalling(p_objListOfGroups, p_objQueueOfGroups, group);
        }
        else
            AssignWorker(p_objGroup);
    }

    static void AssignWorker(Group p_objGroup)
    {
        Worker worker = GetAvailableWorker(p_objGroup);
        worker.TimeSpent += p_objGroup.RequiredTime;
        worker.FinishedGroups.Add(p_objGroup);

        p_objGroup.IsCompleted = true;
    }

    static Worker[] InitWorkers(int p_intNumber)
    {
        Worker[] workers = new Worker[p_intNumber];
        for (int i = 0; i < p_intNumber; i++)
        {
            workers[i] = new Worker();
            workers[i].ID = i;
        }
        return workers;
    }

    static Worker GetAvailableWorker(Group p_objGroup)
    {
        if (p_objGroup.MinTimeBeforeStart > CurrentTime)
            CurrentTime = p_objGroup.MinTimeBeforeStart;

        var worker = Workers.FirstOrDefault(w => w.NextAvailableTime <= CurrentTime);
        if (worker != null)
        {
            worker.NextAvailableTime = CurrentTime + p_objGroup.RequiredTime;

            return worker;
        }

        worker = Workers.OrderBy(t => t.NextAvailableTime).First();
        CurrentTime = worker.NextAvailableTime;
        worker.NextAvailableTime = CurrentTime + p_objGroup.RequiredTime;

        return worker;
    }

    static string GenerateResult(int G, int C, List<Group> p_objListOfGroups)
    {
        SortedSet<string> groupNames = new SortedSet<string>();
        var temp = p_objListOfGroups.OrderByDescending(g => g.RequiredTime).ToList();
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
    public int TimeSpent { get; set; }
    public int NextAvailableTime { get; set; }
    public List<Group> FinishedGroups = new List<Group>();
}

// Class for group of customers with conditions
public class Group : GroupBase
{
    public int RequiredTime { get; set; }
    public List<GroupBase> PrerequisiteGroups = new List<GroupBase>();
    public bool IsCompleted { get; set; }
    public int MinTimeBeforeStart { get; set; }
}

// Class for group of customers
public class GroupBase
{
    public string Category { get; set; }
    public string Name { get; set; }
}

public class ParsedInput
{
    public bool IsValidInput { get; set; }
    public string? ErrorMessage { get; set; }
    public List<Group> GroupOfCustomers { get; set; } = new List<Group>();
}