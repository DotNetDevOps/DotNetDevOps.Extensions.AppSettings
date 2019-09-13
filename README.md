# DotNetDevOps.Extensions.AppSettings

Extensions for working with AppSettings on Azure WebApps / Functions

## DotNetDevOps.Extensions.AppSettings.UpdateAppSettingsFunction 
The function can be deployed as part of your argitecture to provie a way to update app settings of webapps/functions. 
It listens to a queue defined by its app setting name `queuename` for updates following the following schema:

```
    public class AppSettingUpdateModel
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Delete { get; set; }
        public string HostResourceId { get; set; }
    }
```

and it uses its managed identity instance to try do the update. So ensure that the function is given permission to update the resources requested.
