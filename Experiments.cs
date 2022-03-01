using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using GrpcClockClient;
using k8s;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace NFVRPMock
{
    class Experiments
    {
        private const String alarmClockServer = "http://localhost:8080";
        private static ILogger l = null!;
        private const String resourceID = "/subscriptions/abc";


        static void Main(string[] args)
        {
            initLogger();
            l.LogInformation("Let's go!");

            var kubeClient = GetKubeClient();
            var list = kubeClient.ListNamespacedPod("kube-system");
            foreach (var item in list.Items)
            {
                Console.WriteLine(item.Metadata.Name);
            }

            MakeClock().GetAwaiter().GetResult();
            GetClockStatus().GetAwaiter().GetResult();
        }


        private static void initLogger()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Experiments", LogLevel.Debug)
                    .AddConsole();
            });
            l = loggerFactory.CreateLogger<Experiments>();
        }

        private static async Task MakeClock()
        {
            var split = resourceID.Split('/');
            CreateAlarmRequest alarmClockRequest = new CreateAlarmRequest
            {
                Version = "1.2",
                Subscription = split[1],
            };

            l.LogInformation("Requesting a new AlarmClock for resource: {0}", resourceID);

            var errorCount = 0;
            var maxErrorCount = 3;
            for (var i = 0; i <= 500; i++)
            {
                try
                {
                    using var channel = GrpcChannel.ForAddress(alarmClockServer);
                    var client = new Alarm.AlarmClient(channel);
                    var response = await client.CreateAlarmAsync(alarmClockRequest);
                    l.LogInformation("created: {0}", response.Message);
                    // Successful! Done!
                    return;
                }
                catch (Grpc.Core.RpcException)
                {
                    l.LogError("Error creating a alarmClock. The gRPC server at {0} is not running. Did you start it?", alarmClockServer);
                    if (errorCount++ > maxErrorCount)
                    {
                        return;
                    }
                    continue;
                }
            }
        }

        private static async Task GetClockStatus()
        {
            var errorCount = 0;
            var maxErrorCount = 10;
            for (var i = 0; i <= 500; i++)
            {
                try
                {
                    using var channel = GrpcChannel.ForAddress("http://localhost:8080");
                    var client = new Alarm.AlarmClient(channel);
                    var response = await client.GetStatusAsync(new StatusRequest { Subscription = "abc" });
                    l.LogInformation("[status check {0}] Response: {1}", i, response.Message);
                    errorCount = 0;
                    System.Threading.Thread.Sleep(3000);
                }
                catch (Grpc.Core.RpcException)
                {
                    l.LogError("Error getting status. The gRPC server at {0} is not running. Did you start it?", alarmClockServer);
                    if (errorCount++ > maxErrorCount)
                    {
                        return;
                    }
                    continue;
                }
            }
        }

        private static IKubernetes GetKubeClient() {
            // Load from the default kubeconfig on the machine.
            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            // Load from a specific file:
            //// var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(Environment.GetEnvironmentVariable("KUBECONFIG"));
            // Load from in-cluster configuration:
            //// var config = KubernetesClientConfiguration.InClusterConfig();
            return new Kubernetes(config);
        }
    }
}
