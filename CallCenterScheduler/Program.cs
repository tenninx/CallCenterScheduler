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
                var result = Schedule(3, 3, numOfWorkers, parsedInput.GroupOfCustomers);

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

    static string Schedule(int G, int C, int N, List<Group> p_objListOfGroups)
    {
        Workers = InitWorkers(N);

        foreach (Group objGroup in p_objListOfGroups)
        {
            if (!objGroup.IsCompleted)
                StartCalling(p_objListOfGroups, objGroup);
        }

        var highestTime = Workers.OrderByDescending(w => w.NextAvailableTime).First();

        return highestTime.NextAvailableTime.ToString();
    }

    static void StartCalling(List<Group> p_objListOfGroups, Group p_objGroup)
    {
        for (int i = 0; i < p_objGroup.PrerequisiteGroups.Count; i++)
        {
            var unfinishedGroup = p_objListOfGroups.FirstOrDefault(g => g.Name == p_objGroup.PrerequisiteGroups[i].Name
                                                                    && g.Category == p_objGroup.PrerequisiteGroups[i].Category
                                                                    && !g.IsCompleted);

            if (unfinishedGroup != null)
                StartCalling(p_objListOfGroups, unfinishedGroup);
        }

        AssignWorker(p_objGroup);
    }

    static void AssignWorker(Group p_objGroup)
    {
        Worker worker = GetAvailableWorker(p_objGroup.RequiredTime);
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

    static Worker GetAvailableWorker(int p_intTimeNeeded)
    {
        var worker = Workers.FirstOrDefault(w => w.NextAvailableTime <= CurrentTime);
        if (worker != null)
        {
            worker.NextAvailableTime = CurrentTime + p_intTimeNeeded;

            return worker;
        }

        worker = Workers.OrderBy(t => t.NextAvailableTime).First();
        CurrentTime = worker.NextAvailableTime;
        worker.NextAvailableTime = CurrentTime + p_intTimeNeeded;

        return worker;
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