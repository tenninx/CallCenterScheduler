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

The description is pretty clear. One important note to stress: the settings **{G},{C},{N}** must either be specified in the exact order, or none at all for using defaults. They cannot be specified individually. In addition, the settings and input data must be separated by a **dash (-)**. Note that if any group or category names contain a **dash (-)**, the input string should always contain the **{G},{C},{N}** settings prefix before the **{input_data}**. This is to prevent the application from incorrectly parsing the dashed names as settings.

# Conditions & Assumptions

Some conditions and assumptions are in place to ensure that the application runs seamlessly. They are as follows:

- Category and Name of the groups of customers cannot be duplicated in the input
	- There should always be only one record of the combination of category and name of a group of customers. For example, you cannot have two groups with "Medicare" category and "OR Lake" name in the semicolon-separated input. The combination of these two fields makes up its **primary key** of the record as in the relational database terminology. Though this can be solved, there is no point having two groups with exactly the same category-name combination.
- No downtime for a worker to start working on the next group of customers
	- It is assumed that a worker starts calling the next group of customers **once becomes available** immediately. In real life, this is impossible. Nonetheless, this can be implemented easily but it is not the main point of this scheduling application.

# Nested Prerequisites

Any category-group can have virtually unlimited number of prerequisites. These prerequisites can again have virtually any number of prerequisites. In this case, they are called **nested prerequisites**. Regardless of any combinations, the application will complete the scheduling without issues, as long as these prerequisites are not circular/cyclic, causing a deadlock. For example, a deadlock happens when A refers to B, and B refers back to A. The application has a built-in mechanism to break out of the deadlock with an error message.

This is an example of nested prerequisites and the internal calculation:

|Group of Customers|Prerequisites|Worker 1 Start Time|Worker 2 Start Time|Require Time| Finished Time
|--|--|--|--|--|--|
|A,a|A,b||<center>20</center>|<center>4</center>|<center>24</center>
|A,b|A,c,A,e|<center>16</center>||<center>4</center>|<center>20</center>
|A,c|A,d||<center>12</center>|<center>4</center>|<center>16</center>
|A,d|A,f,A,e|<center>8</center>||<center>4</center>|<center>12</center>
|A,e||<center>0<center>||<center>4</center>|<center>4</center>
|A,f|A,e||<center>4</center>|<center>4</center>|<center>8</center>


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