using CallCenterSchedulerNS;

namespace CallCenterSchedulerTests
{
    [TestClass]
    public sealed class CallCenterSchedulerTests
    {
        [TestMethod]
        public void TestParseInput_WithValidInput()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsTrue(result.IsValidInput);
        }

        [TestMethod]
        public void TestParseInput_WithValidBackslashEscapes()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput(@"Home,OR Jefferson\;2,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life\,Subcategory,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsTrue(result.IsValidInput);
        }

        [TestMethod]
        public void TestParseInput_WithValidNestedPrerequisites()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("Home,OR Jefferson,5444;Medicare,OR Lake,1304,Life,OR Other;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsTrue(result.IsValidInput);
        }

        [TestMethod]
        public void TestParseInput_WithInvalidInput1()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("Home,OR Jefferson,a;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsFalse(result.IsValidInput);
            Assert.AreEqual("Invalid required time input at 'Home,OR Jefferson'.", result.ErrorMessage);
        }

        [TestMethod]
        public void TestParseInput_WithInvalidInput2()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("Home,OR Jefferson,5444;MedicareOR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsFalse(result.IsValidInput);
            Assert.AreEqual("Invalid group of customers input at 'MedicareOR Lake,1304'.", result.ErrorMessage);
        }

        [TestMethod]
        public void TestParseInput_WithInvalidInput3()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King1");

            // Then
            Assert.IsFalse(result.IsValidInput);
            Assert.AreEqual("Prerequisite 'Medicare,WA King1 is not found.", result.ErrorMessage);
        }

        [TestMethod]
        public void TestParseInput_WithInvalidInput4()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare");

            // Then
            Assert.IsFalse(result.IsValidInput);
            Assert.AreEqual("Invalid prerequisites input at 'Life,WA Other'.", result.ErrorMessage);
        }

        [TestMethod]
        public void TestParseInput_WithInvalidInput6()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("Home,OR Jefferson,5444;Medicare,OR Lake,1304;Life,OR Other,3456;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsFalse(result.IsValidInput);
            Assert.AreEqual("Duplicated entry 'Life,OR Other' found in the input data. Each group of customers should be unique by its name and category.", result.ErrorMessage);
        }

        [TestMethod]
        public void TestParseInput_WithValidSettings()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("3,3,2-Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsTrue(result.IsValidInput);
        }

        [TestMethod]
        public void TestParseInput_WithInvalidSettings()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("3,32-Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsFalse(result.IsValidInput);
            Assert.AreEqual("Invalid settings input. Expected format: G,C,N. Either specify all or none for using defaults.", result.ErrorMessage);
        }

        [TestMethod]
        public void TestParseInput_WithInvalidSettingsG()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("a,3,2-Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsFalse(result.IsValidInput);
            Assert.AreEqual("Invalid settings input for G. Only a numeric value is accepted.", result.ErrorMessage);
        }

        [TestMethod]
        public void TestParseInput_WithInvalidSettingsC()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("2,a,2-Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsFalse(result.IsValidInput);
            Assert.AreEqual("Invalid settings input for C. Only a numeric value is accepted.", result.ErrorMessage);
        }

        [TestMethod]
        public void TestParseInput_WithInvalidSettingsN()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.ParseInput("2,3,a-Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");

            // Then
            Assert.IsFalse(result.IsValidInput);
            Assert.AreEqual("Invalid settings input for N. Only a numeric value is accepted.", result.ErrorMessage);
        }

        [TestMethod]
        public void TestStartValidWithSampleDefaults()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();
            scheduler.ParseInput("Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");
            scheduler.Start();

            // When
            var result = scheduler.GenerateResult();

            // Then
            Assert.AreEqual(result, "115309,OR Other,WA King,WA Other");
        }

        [TestMethod]
        public void TestStartValidWithSample210()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();
            scheduler.ParseInput("2,1,0-Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");
            scheduler.Start();

            // When
            var result = scheduler.GenerateResult();

            // Then
            Assert.AreEqual(result, "114005,OR Other,WA Other");
        }

        [TestMethod]
        public void TestStartValidWithSample221()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();
            scheduler.ParseInput("2,2,1-Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,OR Other,12806,Medicare,OR Lake;Life,WA Other,70944,Medicare,WA King");
            scheduler.Start();

            // When
            var result = scheduler.GenerateResult();

            // Then
            Assert.AreEqual(result, "133559,WA King,WA Other");
        }

        [TestMethod]
        public void TestStartValidWithSomeFinishedMultiplePrerequisites()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();
            scheduler.ParseInput("Life,OR Other,12806,Medicare,OR Lake;Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,WA Other,70944,Medicare,WA King,Medicare,OR Lake");
            scheduler.Start();

            // When
            var result = scheduler.GenerateResult();

            // Then
            Assert.AreEqual(result, "115309,OR Other,WA King,WA Other");
        }

        [TestMethod]
        public void TestStartValidWithNoFinishedMultiplePrerequisites()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();
            scheduler.ParseInput("Life,OR Other,12806,Medicare,OR Lake,Home,OR Jefferson;Home,OR Jefferson,5444;Medicare,OR Lake,1304;Medicare,WA King,43061;Life,WA Other,70944,Medicare,WA King");
            scheduler.Start();

            // When
            var result = scheduler.GenerateResult();

            // Then
            Assert.AreEqual(result, "115309,OR Other,WA King,WA Other");
        }

        [TestMethod]
        public void TestStartValidWithNestedPrerequisitesAndNoTopGroups()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();
            scheduler.ParseInput("0,0,2-c,0,1;c,0-1,1,c,0;c,0-2,1,c,0;c,0-1-1,1,c,0-1;c,0-1-2,1,c,0-1;c,0-2-1,1,c,0-2;c,0-2-2,1,c,0-2");
            scheduler.Start();

            // When
            var result = scheduler.GenerateResult();

            // Then
            Assert.AreEqual(result, "4");
        }

        [TestMethod]
        public void TestStartValidWithTieError()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();
            scheduler.ParseInput("2,2,0-A,a,4;A,b,3;B,b,1;B,c,4");
            scheduler.Start();

            // When
            var result = scheduler.GenerateResult();

            // Then
            Assert.AreEqual(result, "ERROR - tie (group)");
        }

        [TestMethod]
        public void GenerateResultWithoutStart()
        {
            // Given
            CallCenterScheduler scheduler = new CallCenterScheduler();

            // When
            var result = scheduler.GenerateResult();

            // Then
            Assert.AreEqual(result, "Error Message: CallCenterScheduler should be started first. Execute Start() method before generating the result.");
        }
    }
}