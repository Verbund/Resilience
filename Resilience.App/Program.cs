using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.CircuitBreaker;

namespace Resilience.App
{
    class Program
    {
        private HttpClient client;

        private Program(string[] args)
        {
            client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"));
        }

        static void Main(string[] args)
        {
            var program = new Program(args);

            //program.RunRetry();
            //program.RunFallback();
            program.RunCircuitBreaker();
            //program.RunWrap();
        }

        public void RunRetry()
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, context) =>
                {
                    Console.WriteLine($"Retry of {context.ExecutionKey} at {timeSpan}");
                })
                .WithPolicyKey("HttpRetryPolicy");

            for (int i = 0; i < 2; i++)
            {
                try
                {

                    var returnValue =
                        retryPolicy.ExecuteAsync<String>(
                            () => client.GetStringAsync("http://localhost:5000/api/values/5"),
                            new Context($"Execution number {i + 1}."));

                    var result = returnValue.Result;

                    Console.WriteLine(result);

                    Console.WriteLine("Press any key ...");
                    Console.ReadKey();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Exception!!!");
                }
            }
        }

        public void RunCircuitBreaker()
        {
            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking: 2, durationOfBreak: TimeSpan.FromSeconds(3),
                onBreak: (exception, timeSpan, context) => { Console.WriteLine($"Break of {context.PolicyKey} at {timeSpan}"); },
                onReset: context => { Console.WriteLine($"Reset of {context.PolicyKey}"); })
                .WithPolicyKey("HttpCircuitBreakerPolicy");

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var returnValue =
                        circuitBreakerPolicy.ExecuteAsync<String>(
                            () => client.GetStringAsync("http://localhost:5000/api/values/5"),
                            new Context($"Execution number {i + 1}."));

                    var result = returnValue.Result;

                    Console.WriteLine(result);
                }
                catch (BrokenCircuitException e)
                {
                    Console.Error.WriteLine("Circuit broken");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Exception!!!");
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            Console.WriteLine("Press any key ...");
            Console.ReadKey();
        }

        public void RunFallback()
        {
            var fallbackPolicy = Policy<string>
                .Handle<Exception>()
                .FallbackAsync<string>(fallbackValue: "fallback")
                .WithPolicyKey("HttpFallbackPolicy");

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    var returnValue =
                        fallbackPolicy.ExecuteAsync(
                            () => client.GetStringAsync("http://localhost:5000/api/values/5"),
                            new Context($"Execution number {i + 1}."));

                    var result = returnValue.Result;

                    Console.WriteLine(result);

                    Console.WriteLine("Press any key ...");
                    Console.ReadKey();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Exception!!!");
                }
            }
        }

        public void RunWrap()
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, context) =>
                    {
                        Console.WriteLine($"Retry of {context.ExecutionKey} at {timeSpan}");
                    })
                .WithPolicyKey("HttpRetryPolicy");

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking: 2, durationOfBreak: TimeSpan.FromSeconds(2),
                    onBreak: (exception, timeSpan, context) => { Console.WriteLine($"Break of {context.PolicyKey} at {timeSpan}"); },
                    onReset: context => { Console.WriteLine($"Reset of {context.PolicyKey}"); })
                .WithPolicyKey("HttpCircuitBreakerPolicy");

            var wrapPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy).WithPolicyKey("HttpWrapPolicy");

            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var returnValue =
                        wrapPolicy.ExecuteAsync<String>(
                            () => client.GetStringAsync("http://localhost:5000/api/values/5"),
                            new Context($"Execution number {i + 1}."));

                    var result = returnValue.Result;

                    Console.WriteLine(result);
                }
                catch (BrokenCircuitException e)
                {
                    Console.Error.WriteLine("Circuit broken");
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Exception!!!");
                }

                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            Console.WriteLine("Press any key ...");
            Console.ReadKey();
        }
    }
}
