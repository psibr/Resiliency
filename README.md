# Resiliency for .NET
A comprehensive, opinionated, and function-first resiliency library for C# and the broader .NET ecosystem.

### The basics
Resiliency adds a layer of handlers to an existing method call. 

We can demonstrate the problem, and the solutions that Resiliency can provide pretty simply.

#### The PING scenario
In this scenario we have a theoretical API that provides us a `Task PingAsync(CancellationToken c)` method to test that it is up, when it goes down, the call will fail with a HttpNotFoundException. The caveat being: we know this API goes down for little spurts of time, or network requests fail, as they commonly can in the cloud.

This example shows a simple first pass at handling a Ping result.

```csharp
class Program
{
    public static async Task Main()
    {
        do
        {
            try
            {
                await PingAsync(CancellationToken.None);

				break;
            }
            catch(HttpNotFoundException)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        } while(true);
        
        // We can safely know the API appeared to be up by this point.
    }
}
```

A more comprehensive example in Resiliency may look like this.

```csharp
class Program
{
    public static async Task Main()
    {
        await ResilientOperation.From(PingAsync)
            .WhenExceptionIs<HttpNotFoundException>(async (op, ex) =>
            {
                await op.RetryAfterAsync(TimeSpan.FromSeconds(5));
            })
            .InvokeAsync();
            
        // We can safely know the API appeared to be up by this point.
    }
}
```
