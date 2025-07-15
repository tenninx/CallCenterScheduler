# Call Center Scheduler

This is a readme instruction document for Call Center Scheduler application. The console application is written in **C#** with no external dependencies and should compile with a standard .NET framework compiler. In the following sections, you will find more information regarding this application.

# File Structure

The application was written with Visual Studio Community 2022. There are two projects in the solution and the file structure is as follows:

 - **CallCenterScheduler**
	 - Program.cs
		 - *Independent file containing the source code for the main application. This file can be compiled using standard csc.exe compiler or [https://rextester.com/](https://rextester.com/) alone*
	 - CallCenterScheduler.csproj
		 - *Project file for CallCenterScheduler*
 - **CallCenterSchedulerTests**
	 - CallCenterSchedulerTests.cs
		 - *Unit tests to test CallCenterScheduler*
	 - CallCenterSchedulerTests.csproj
		 - *Project file for the unit tests*
	 - MSTestSettings.cs
		 - *Settings file for the unit tests*

<u>Program.cs</u> is the only file needed to run the application. All others are optional.

# How to Run

Once the file has been compiled into **CallCenterScheduler.exe**, navigate to the directory containing the file using Command Prompt or PowerShell, for example:

    cd D:\Application\CallCenterScheduler

At the directory, the console application can be executed by the EXE file itself, for example:

    PS D:\Application\CallCenterScheduler> .\CallCenterScheduler.exe

Upon execution, you will be greeted with a description explaining the input format:

> Input format: {G},{C},{N}-{input_data} as in:
> G: Top number of groups (default is 3). Enter 0 to return only the completion time. Negative not accepted.
> C: Top number of categories (default is 3). Enter 0 to return only the completion time. Negative not accepted.
> N: Number of workers (default is 2). Enter 0 for unlimited number of workers. Negative not accepted.
> input_data: Semicolon-separated data in the form "category,group,time" followed by zero or more prerequisites of the form ",category,group", e.g. Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King
> You need to specify all G, C and N at once or none by specifying {input_data} directly to use the default settings.
> Example input with settings: 2,2,0-Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King
> Example input using defaults: Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King
> Leave it empty and press enter to terminate. Enter your input:

The description is pretty clear. One important note to stress: the settings **{G},{C},{N}** must either be specified in the exact order, or none at all for using defaults. They cannot be specified individually. In addition, the settings and input data must be separated by a **dash (-)**.

# Conditions & Assumptions

Some conditions and assumptions are in place to ensure that the application runs seamlessly. They are as follows:

- Multiple groups of prerequisites are allowed but nested prerequisites are allowed only on one condition
	- For example: **V** can have prerequisites **X**, **Y** and **Z**. In this case, all **X**, **Y** and **Z** can also have prerequisites which **must have been finished**. This is to reduce complexity of the algorithm which may need much more time to design. By allowing nested prerequisites, **deadlocks** can happen. For example, when **M** refers to **N** and **N** refers back to **M** in their prerequisites. Currently, the application does not have a mechanism to prevent deadlocks. A way to deal with this is to have a pre-schedule tracing algorithm to find out the paths of all workers first before the actual scheduling algorithm takes place.
- Category and Name of the groups of customers cannot be duplicated in the input
	- There should always be only one record of the combination of category and name of a group of customers. For example, you cannot have two groups with "Medicare" category and "OR Lake" name in the semicolon-separated input. The combination of these two fields makes up its **primary key** of the record as in the relational database terminology. Though this can be solved, there is no point having two groups with exactly the same category-name combination.
- No downtime for a worker to start working on the next group of customers
	- It is assumed that a worker starts calling the next group of customers **once becomes available** immediately. In real life, this is impossible. Nonetheless, this can be implemented easily but it is not the main point of this scheduling application.

# Output

After a successful execution of the scheduling application, you can expect the output in the following format:

> Result: 115309,OR Other,WA King,WA Other

This is the expected result from the following sample input:

> Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King

## Completion Time

Since default settings **{3,3,2}** are used when no settings are given in the input, the first part of the output **115309** is the completion time of the final group using 2 workers **N**. The internal calculation is shown in the following table:

|Group of Customers|Prerequisites|Worker 1 Start Time|Worker 2 Start Time|Required Time|Finished Time
|--|--|--|--|--|--|
|Home,OR Jefferson||<center>0</center>||<center>5444</center>|<center>5444</center>
|Medicare,OR Lake|||<center>0</center>|<center>1304</center>|<center>1304</center>
|Medicare,WA King|||<center>1304</center>|<center>43061</center>|<center>44365</center>
|Life,OR Other|Medicare,OR Lake|<center>5444</center>||<center>12806</center>|<center>18250</center>
|Life,WA Other|Medicare,WA King|<center>***44365****</center>||<center>70944</center>|<center>115309</center>

Notice the * mark behind the **44365** value, the **Worker 1 Start Time** on **Life,WA Other** group was delayed. This was because the group required **Medicare,WA King** to complete beforehand. That group would take 43061 but was assigned to Worker 2 who worked on **Medicare,OR Lake** prior to that. As a result, Worker 2 could only start calling **Medicare,WA King** when **Medicare,OR Lake** was finished at 1304, making the completion time of **Medicare,WA King** 44365 (1304 + 43061). Subsequently, Worker 1 could start calling **Life,WA Other** at that time and complete it at 115309 (44365 + 70944).

Theoretically, Worker 2 could start calling **Life,WA Other** right away. Knowing this, the completion time of the final group remains the same. In my algorithm, Worker 1 who rested enough would call the next group.

## Top Groups in Top Categories

Top groups in top categories make up the next part of the output.

From the sample output: **OR Other,WA King,WA Other**

With reference to **{3,3,2}** settings, it means the top 3 groups **G** from the top 3 categories **C** will be displayed as output <u>in alphabetical order</u>. However, if more than one group has the same completion time (finishes at the same time), an error message *"ERROR - tie (group)"* will return.

When one of these values set to 0, only the completion time will return. This also ignores the tie error mentioned above. The selection puts higher priority in groups than categories. If the top 3 groups were all from 1 category, then the rest 2 categories would be skipped.

On the other hand, if **{3,1,N}** was inputted and only 2 groups in the top category, the result will show only 2 group names.