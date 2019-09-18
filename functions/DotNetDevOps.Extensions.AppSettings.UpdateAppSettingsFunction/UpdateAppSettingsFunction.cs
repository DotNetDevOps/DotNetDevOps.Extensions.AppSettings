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

        private static async Task<WebSiteManagementClient> CreateWebSiteManagementClientAsync(ILogger logger, string subscriptionId)
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
            logger.LogInformation("Created WebSiteManagementClient for {SubscriptionId}", subscriptionId);
            return websiteClient;
        }

        [FunctionName("UpdateAppSettingsFunction")]
        public static async Task Run([QueueTrigger("%queuename%")]AppSettingUpdateModel appSettingUpdate, ILogger log)
        {
            log.LogInformation("Processing appsetting update {data}", JToken.FromObject(appSettingUpdate).ToString());

            if (!string.IsNullOrWhiteSpace(appSettingUpdate.HostResourceId))
            {
                var client = await CreateWebSiteManagementClientAsync(log,appSettingUpdate.HostResourceId.Trim('/').Split('/').Skip(1).FirstOrDefault());
                var resourceGroupName = appSettingUpdate.HostResourceId.Trim('/').Split('/').Skip(3).FirstOrDefault();
                var websiteName = appSettingUpdate.HostResourceId.Trim('/').Split('/').LastOrDefault();

                log.LogInformation("Querying configuration for {resourceGroupName} and {websiteName}",resourceGroupName, websiteName);
                var config = await client.WebApps.ListApplicationSettingsAsync(resourceGroupName,websiteName );
                if (appSettingUpdate.Delete)
                {
                    config.Properties.Remove(appSettingUpdate.Name);
                }
                else
                {
                    config.Properties[appSettingUpdate.Name] = appSettingUpdate.Value;
                }

                await client.WebApps.UpdateApplicationSettingsAsync(resourceGroupName, websiteName, config);

            }

            log.LogInformation($"C# Queue trigger function processed: {JToken.FromObject(appSettingUpdate).ToString(Newtonsoft.Json.Formatting.Indented)}");
        }
    }
}
