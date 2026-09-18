using RepalogicConcurrencyAssignment.Models;
using RepalogicConcurrencyAssignment.Services;

var messages = new List<Message>
{
    new() { OrderId = 1, Event = "Create" },
    new() { OrderId = 2, Event = "Create" },
    new() { OrderId = 1, Event = "Cancel" },
    new() { OrderId = 3, Event = "Create" },
    new() { OrderId = 2, Event = "Update" }
};

var dispatcher = new MessageDispatcher(3);

// Start 3 workers.
var workersTask = dispatcher.StartWorkersAsync();

// Add messages to the dispatcher.
foreach (var message in messages)
{
    await dispatcher.DispatchAsync(message);
}

// Tell the workers that no more messages are coming.
dispatcher.Complete();

// Wait for all workers to finish.
await workersTask;

Console.WriteLine("All messages processed.");