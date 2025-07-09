using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
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
                var result = Schedule(3, 3, numOfWorkers, parsedInput.GroupOfCustomers);

                Console.WriteLine("Result: " + result);
            }
        }
    }

    // Sample Input: "Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King,Life,OR Other"

    static ParsedInput ParseInput(string p_strInput)
    {
        var objResult = new ParsedInput();

        string[] strGroups = p_strInput.Split(';');

        List<Group> objGroups = new List<Group>();

        foreach (var strGroup in strGroups)
        {
            string[] strGDetails = strGroup.Split(',');
            List<GroupBase> objPrereqs = new List<GroupBase>();

            if (strGDetails.Length < 3)
                return new ParsedInput { ErrorMessage = "Invalid group of customers input." };

            if (!int.TryParse(strGDetails[2], out int time))
                return new ParsedInput { ErrorMessage = "Invalid required time input." };

            if (strGDetails.Length > 3 && (strGDetails.Length - 3) % 2 != 0)
                return new ParsedInput { ErrorMessage = "Invalid prerequisites input." };
            else if (strGDetails.Length > 3)
            {
                for (int i = 3; i < strGDetails.Length; i++)
                {
                    objPrereqs.Add(new GroupBase
                    {
                        Category = strGDetails[i],
                        Name = strGDetails[++i]
                    });
                }
            }

            Group objGroup = new Group
            {
                Category = strGDetails[0],
                Name = strGDetails[1],
                RequiredTime = time,
                PrerequisiteGroups = objPrereqs
            };

            objGroups.Add(objGroup);
        }

        objResult.IsValidInput = true;
        objResult.GroupOfCustomers = objGroups;

        return objResult;
    }

    static string Schedule(int G, int C, int N, List<Group> p_objListOfGroups)
    {
        Worker[] workers = InitWorkers(N);

        foreach (Group objGroup in p_objListOfGroups)
        {
            if (!objGroup.IsCompleted)
                AssignWork(p_objListOfGroups, objGroup, workers);
        }

        var highestTime = workers.OrderByDescending(w => w.TimeSpent).First();

        return highestTime.TimeSpent.ToString();
    }

    static void AssignWork(List<Group> p_objListOfGroups, Group objGroup, Worker[] workers)
    {
        for (int i = 0; i < objGroup.PrerequisiteGroups.Count; i++)
        {
            var unfinishedGroup = p_objListOfGroups.FirstOrDefault(g => g.Name == objGroup.PrerequisiteGroups[i].Name
                                                                    && g.Category == objGroup.PrerequisiteGroups[i].Category
                                                                    && !g.IsCompleted);

            if (unfinishedGroup != null)
                AssignWork(p_objListOfGroups, unfinishedGroup, workers);
        }

        FinishWork(objGroup, GetAvailableWorker(workers));
    }

    static void FinishWork(Group p_objGroup, Worker p_objWorker)
    {
        p_objGroup.IsCompleted = true;
        p_objWorker.TimeSpent += p_objGroup.RequiredTime;
        p_objWorker.FinishedGroups.Add(p_objGroup);
    }

    static Worker[] InitWorkers(int p_intNumber)
    {
        Worker[] workers = new Worker[p_intNumber];
        for (int i = 0; i < p_intNumber; i++)
            workers[i] = new Worker();
        return workers;
    }

    static Worker GetAvailableWorker(Worker[] p_objWorkers)
    {
        return p_objWorkers.OrderBy(t => t.TimeSpent).First();
    }
}

public class Worker
{
    public bool IsWorking { get; set; }
    public int TimeSpent { get; set; }

    // For debugging purpose
    public List<Group> FinishedGroups = new List<Group>();
}

// Class for group of customers with conditions
public class Group : GroupBase
{
    public int RequiredTime { get; set; }
    public List<GroupBase> PrerequisiteGroups = new List<GroupBase>();
    public bool IsCompleted { get; set; }
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