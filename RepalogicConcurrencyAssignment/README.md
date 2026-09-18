# Assignment 1 – Asynchronous Race Condition

## Problem

The Order Management System (OMS) publishes order events such as `Create`, `Update`, and `Cancel` to an asynchronous message queue.

Multiple workers process messages concurrently to achieve high throughput. However, messages for the same `OrderId` must always be processed sequentially.

For example:
Order 1 - Create
Order 1 - Cancel

`Cancel` must not start before `Create` has completed.

At the same time, messages for different orders should be processed in parallel.

---

## Approach

The solution uses **3 in-memory Channels** as partitions.

Each message is routed to a worker based on its `OrderId`:

OrderId → Worker

Order 1 → Worker 1
Order 2 → Worker 2
Order 3 → Worker 3

The routing logic is:

int workerIndex = (message.OrderId - 1) % _channels.Length;

Because all messages for the same `OrderId` always go to the same channel, they are processed sequentially by the same worker.

Different orders can be processed concurrently by different workers.

### Example

Worker 1:
Order 1 - Create
Order 1 - Cancel

Worker 2:
Order 2 - Create
Order 2 - Update

Worker 3:
Order 3 - Create

Therefore, `Order 1 - Cancel` cannot execute before `Order 1 - Create` finishes, while Orders 2 and 3 can be processed in parallel.

---

## Concurrency Model

The application uses:

* `Channel<T>` for asynchronous in-memory message queues
* 3 worker tasks for parallel processing
* `async/await` for non-blocking processing
* `ConcurrentDictionary` for thread-safe failed-order tracking
* `Task.WhenAll` to wait for all workers to complete

Processing time is simulated using:

await Task.Delay(Random.Shared.Next(100, 501));

This simulates the delay of the legacy Inventory System.

---

## Failure Handling

If processing a message fails, the corresponding `OrderId` is marked as failed using a thread-safe `ConcurrentDictionary`.

Subsequent messages for that order are skipped.

For example:

Order 1 - Create → FAILED
Order 1 - Cancel → SKIPPED

This prevents a dependent operation such as `Cancel` from being sent after a failed `Create`.

In a production system, failed messages could additionally be retried or moved to a dead-letter queue.

---

## Scalability

The current implementation uses 3 workers as required by the assignment.

The number of workers can be increased by changing:

var dispatcher = new MessageDispatcher(3);

The partitioning approach allows different orders to be processed concurrently while maintaining ordering for messages belonging to the same order.

The trade-off is that multiple different orders can map to the same worker, which may reduce parallelism for some OrderId distributions, but ordering remains guaranteed.

---

## Expected Result

The console output should demonstrate that:

* Order 1 `Create` completes before Order 1 `Cancel` starts.
* Order 2 and Order 3 can be processed concurrently with Order 1.
* Order 2 `Create` completes before Order 2 `Update`.
* All workers finish before the application exits.

---

## Project Structure

RepalogicConcurrencyAssignment
│
├── Models
│   └── Message.cs
│
├── Services
│   └── MessageDispatcher.cs
│
├── Program.cs
└── README.md

## Technologies

* C#
* .NET 6+
* TPL (`Task`, `Task.WhenAll`)
* `System.Threading.Channels`
* `ConcurrentDictionary`
* `async/await`
