using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Management.AppService.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json.Linq;

namespace DotNetDevOps.Extensions.AppSettings.UpdateAppSettingsFunction
{

    public class AppSettingUpdateModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Delete { get; set; }
        public string HostResourceId { get; set; }
    }

    public static class UpdateAppSettingsFunction
    {

        private static async Task<WebSiteManagementClient> CreateWebSiteManagementClientAsync(string subscriptionId)
        {
            var tokenProvider = new AzureServiceTokenProvider();

            var accessToken = await tokenProvider.GetAccessTokenAsync("https://management.azure.com/");
            var tokenCredentials = new TokenCredentials(accessToken);
            var azureCredentials = new AzureCredentials(
           tokenCredentials,
           tokenCredentials,
           "common",
           AzureEnvironment.AzureGlobalCloud);

            var client = RestClient
            .Configure()
            .WithEnvironment(AzureEnvironment.AzureGlobalCloud)
            .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
            .WithCredentials(azureCredentials)
            .Build();

            var websiteClient = new WebSiteManagementClient(client)
            {
                SubscriptionId = subscriptionId
            };

            return websiteClient;
        }

        [FunctionName("UpdateAppSettingsFunction")]
        public static async Task Run([QueueTrigger("%queuename%")]AppSettingUpdateModel appSettingUpdate, ILogger log)
        {
            if (!string.IsNullOrWhiteSpace(appSettingUpdate.HostResourceId))
            {
                var client = await CreateWebSiteManagementClientAsync(appSettingUpdate.HostResourceId.Split('/').Skip(1).FirstOrDefault());

                var config = await client.WebApps.ListApplicationSettingsAsync(appSettingUpdate.HostResourceId.Split('/').Skip(3).FirstOrDefault(), appSettingUpdate.HostResourceId.Split('/').LastOrDefault());
                if (appSettingUpdate.Delete)
                {
                    config.Properties.Remove(appSettingUpdate.Name);
                }
                else
                {
                    config.Properties[appSettingUpdate.Name] = appSettingUpdate.Value;
                }

                await client.WebApps.UpdateApplicationSettingsAsync(appSettingUpdate.HostResourceId.Split('/').Skip(3).FirstOrDefault(), appSettingUpdate.HostResourceId.Split('/').LastOrDefault(), config);

            }

            log.LogInformation($"C# Queue trigger function processed: {JToken.FromObject(appSettingUpdate).ToString(Newtonsoft.Json.Formatting.Indented)}");
        }
    }
}