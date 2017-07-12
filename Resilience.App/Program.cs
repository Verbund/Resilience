using System;
using System.Net.Http;
using System.Threading.Tasks;
using Polly;

namespace Resilience.App
{
    class Program
    {
        private Program(string [] args)
        {
        }

        static void Main(string[] args)
        {
            var program = new Program(args);

            program.Run();
        }

        public void Run()
        {
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2 * i))
                .WithPolicyKey("HttpRetryPolicy");

            var returnValue = retryPolicy.ExecuteAsync<string>(() => CallApi(5), new Context("The execution."));

            Console.WriteLine(returnValue.Result);

            Console.ReadKey();
        }

        public Task<string> CallApi(int id)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"));

            var result = client.GetAsync($"http://localhost:5000/api/values/{id}");

            result.Result.EnsureSuccessStatusCode();

            return result.Result.Content.ReadAsStringAsync();
        }
    }
}
