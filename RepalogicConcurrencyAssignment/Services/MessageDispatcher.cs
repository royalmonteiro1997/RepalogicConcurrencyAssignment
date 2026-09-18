using RepalogicConcurrencyAssignment.Models;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace RepalogicConcurrencyAssignment.Services;

public class MessageDispatcher
{
    private readonly Channel<Message>[] _channels;

    // Stores OrderIds for which a previous message failed.
    // Subsequent messages for the same order will be skipped.
    private readonly ConcurrentDictionary<int, bool> _failedOrders = new();

    public MessageDispatcher(int workerCount)
    {
        _channels = new Channel<Message>[workerCount];

        for (int i = 0; i < workerCount; i++)
        {
            _channels[i] = Channel.CreateUnbounded<Message>();
        }
    }

    public async Task DispatchAsync(Message message)
    {
        // All messages for the same OrderId go to the same worker.
        // This guarantees sequential processing for that order.
        int workerIndex = (message.OrderId - 1) % _channels.Length;

        await _channels[workerIndex].Writer.WriteAsync(message);
    }

    public async Task StartWorkersAsync()
    {
        var workers = new List<Task>();

        for (int i = 0; i < _channels.Length; i++)
        {
            int workerId = i + 1;

            workers.Add(
                ProcessMessagesAsync(
                    workerId,
                    _channels[i].Reader));
        }

        // All workers run concurrently.
        await Task.WhenAll(workers);
    }

    private async Task ProcessMessagesAsync(
        int workerId,
        ChannelReader<Message> reader)
    {
        // Messages within one channel are processed sequentially.
        await foreach (var message in reader.ReadAllAsync())
        {
            // Skip messages if an earlier operation for this order failed.
            if (_failedOrders.ContainsKey(message.OrderId))
            {
                Console.WriteLine(
                    $"Worker {workerId} SKIPPED: " +
                    $"Order {message.OrderId} - {message.Event} " +
                    $"because a previous operation failed.");

                continue;
            }

            try
            {
                Console.WriteLine(
                    $"Worker {workerId} STARTED: " +
                    $"Order {message.OrderId} - {message.Event}");

                // Simulate legacy Inventory System processing time.
                int delay = Random.Shared.Next(100, 501);

                await Task.Delay(delay);

                Console.WriteLine(
                    $"Worker {workerId} COMPLETED: " +
                    $"Order {message.OrderId} - {message.Event}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"Worker {workerId} FAILED: " +
                    $"Order {message.OrderId} - {message.Event}");

                Console.WriteLine($"Error: {ex.Message}");

                // Mark the order as failed.
                // Later messages for this order will be skipped.
                _failedOrders.TryAdd(message.OrderId, true);
            }
        }
    }

    public void Complete()
    {
        // Tell all workers that no more messages will be added.
        foreach (var channel in _channels)
        {
            channel.Writer.Complete();
        }
    }
}