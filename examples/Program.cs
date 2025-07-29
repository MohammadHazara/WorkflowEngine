using WorkflowEngine.Examples;

Console.WriteLine("🔧 WorkflowEngine API Examples");
Console.WriteLine("===============================");
Console.WriteLine();

while (true)
{
    Console.WriteLine("Choose an example to run:");
    Console.WriteLine("1. Complete API-to-File-to-Upload Workflow");
    Console.WriteLine("2. Simple API-to-File Workflow");
    Console.WriteLine("3. Performance Test Example");
    Console.WriteLine("4. Exit");
    Console.WriteLine();
    Console.Write("Enter your choice (1-4): ");

    var choice = Console.ReadLine();
    Console.WriteLine();

    try
    {
        switch (choice)
        {
            case "1":
                await ApiWorkflowExamples.RunCompleteApiWorkflowExample();
                break;
            case "2":
                await ApiWorkflowExamples.RunSimpleApiWorkflowExample();
                break;
            case "3":
                await ApiWorkflowExamples.RunPerformanceTestExample();
                break;
            case "4":
                Console.WriteLine("👋 Goodbye!");
                return;
            default:
                Console.WriteLine("❌ Invalid choice. Please enter 1-4.");
                continue;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"💥 Error: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("Press any key to continue...");
    Console.ReadKey();
    Console.Clear();
}