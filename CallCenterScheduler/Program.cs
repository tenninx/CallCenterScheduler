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
        var result = objGroups.SelectManyRecursive(l => l.PrerequisiteGroups).Select(l => l.Name).ToList();

        objResult.IsValidInput = true;
        objResult.GroupOfCustomers = objGroups;

        return objResult;
    }

    static void Schedule(List<Group> p_objListOfGroups, int N)
    {
        Workers = InitWorkers(N);

        Queue<Group> p_objQueueOfGroups = new Queue<Group>(p_objListOfGroups);
        //Queue<Group> p_objQueueOfGroups = new Queue<Group>(p_objListOfGroups.OrderBy(g => g.PrerequisiteGroups.Count));

        while (p_objQueueOfGroups.Count > 0)
        {
            Group objCurrentGroup;
            do
            {
                objCurrentGroup = p_objQueueOfGroups.Dequeue();
            } while (objCurrentGroup.IsCompleted);

            //int maxPrereqTime = 0;
            //GetWaitingTime(p_objListOfGroups, p_objQueueOfGroups, objCurrentGroup, ref maxPrereqTime);
            //objCurrentGroup.MinTimeBeforeStart = maxPrereqTime;

            StartCalling(p_objListOfGroups, p_objQueueOfGroups, objCurrentGroup);
            //objCurrentGroup.MinTimeBeforeStart = minStartTime;
        }
    }

    private static void GetWaitingTime(List<Group> p_objListOfGroups, Queue<Group> p_objQueueOfGroups, Group p_objCurrentGroup, ref int p_intTime)
    {
        var objPrereqs = p_objListOfGroups.OrderByDescending(a => a.RequiredTime).Join(p_objCurrentGroup.PrerequisiteGroups, e => new { e.Name, e.Category }, d => new { d.Name, d.Category }, (tbl1, tbl2) => tbl1);

        if (objPrereqs.Count() > 0)
        {
            p_intTime += objPrereqs.First().RequiredTime;

            GetWaitingTime(p_objListOfGroups, p_objQueueOfGroups, objPrereqs.First(), ref p_intTime);
        }
    }

    static void StartCalling(List<Group> p_objListOfGroups, Queue<Group> p_objQueueOfGroups, Group p_objGroup)
    {
        if (p_objGroup.MinTimeBeforeStart == 0)
        {
            int maxPrereqTime = 0;
            GetWaitingTime(p_objListOfGroups, p_objQueueOfGroups, p_objGroup, ref maxPrereqTime);
            Worker a = GetAvailableWorker(p_objGroup);
            p_objGroup.MinTimeBeforeStart = maxPrereqTime;
        }
        //else
        //    p_objGroup.MinTimeBeforeStart += CurrentTime - PreviousTime;

        var objPrereqs = p_objListOfGroups.Where(g => !g.IsCompleted).OrderByDescending(a => a.RequiredTime).Join(p_objGroup.PrerequisiteGroups, e => new { e.Name, e.Category }, d => new { d.Name, d.Category }, (tbl1, tbl2) => tbl1);

        if (objPrereqs.Count() > 0)
        {
            //p_intMaxPrereqTime += objPrereqs.First().RequiredTime;
            //p_objGroup.MinTimeBeforeStart += objPrereqs.First().RequiredTime;
            p_objQueueOfGroups.Enqueue(p_objGroup);
            StartCalling(p_objListOfGroups, p_objQueueOfGroups, objPrereqs.First());

            //p_intMaxPrereqTime += maxRequiredTime;
            //p_objGroup.MinTimeBeforeStart = CurrentTime + p_intMaxPrereqTime;
            //p_objGroup.MinTimeBeforeStart = CurrentTime + p_intMaxPrereqTime;

            //foreach (Group group in objPrereqs)
            //{
            //    // return prerequistes wait time
            //    StartCalling(p_objListOfGroups, p_objQueueOfGroups, group, ref p_intMaxPrereqTime);
            //    //group.MinTimeBeforeStart = CurrentTime + p_intMaxPrereqTime;
            //}

            //if (p_objGroup.MinTimeBeforeStart < p_intMaxPrereqTime)
            //    p_objGroup.MinTimeBeforeStart = p_intMaxPrereqTime;
            //return p_intMaxPrereqTime;
        }
        else
            AssignWorker(p_objGroup);

        //return 0;
    }

    //static int StartCalling(List<Group> p_objListOfGroups, Queue<Group> p_objQueueOfGroups, Group p_objGroup, ref int p_intMaxPrereqTime)
    //{
    //    var objPrereqs = p_objListOfGroups.Where(g => !g.IsCompleted).Join(p_objGroup.PrerequisiteGroups, e => new { e.Name, e.Category }, d => new { d.Name, d.Category }, (tbl1, tbl2) => tbl1);

    //    if (objPrereqs.Count() > 0)
    //    {
    //        var maxRequiredTime = objPrereqs.Max(a => a.RequiredTime);
    //        p_intMaxPrereqTime += maxRequiredTime;
    //        p_objGroup.MinTimeBeforeStart = CurrentTime + p_intMaxPrereqTime;
    //        //p_objGroup.MinTimeBeforeStart = CurrentTime + p_intMaxPrereqTime;
    //        p_objQueueOfGroups.Enqueue(p_objGroup);
    //        foreach (Group group in objPrereqs)
    //        {
    //            // return prerequistes wait time
    //            StartCalling(p_objListOfGroups, p_objQueueOfGroups, group, ref p_intMaxPrereqTime);
    //            //group.MinTimeBeforeStart = CurrentTime + p_intMaxPrereqTime;
    //        }

    //        return p_intMaxPrereqTime;
    //    }
    //    else
    //        AssignWorker(p_objGroup);

    //    return 0;
    //}

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

        var worker = Workers.OrderBy(w => w.NextAvailableTime).FirstOrDefault(w => w.NextAvailableTime <= CurrentTime);
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
}

public class ParsedInput
{
    public bool IsValidInput { get; set; }
    public string? ErrorMessage { get; set; }
    public List<Group> GroupOfCustomers { get; set; } = new List<Group>();
}