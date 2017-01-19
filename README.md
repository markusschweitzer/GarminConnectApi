# GarminConnectApi

JSON based API for https://connect.garmin.com/ in C#.

This will result in an authenticated WebClient which is able to call GarminConnect JSON Urls.

### Example

```csharp
using (var gc = new GarminConnection())
{
    if (!gc.Authenticate("username", "password", "http://somereferer.com"))
    {
        Console.WriteLine("Authentication not successfull");       
    }
    else
    {
        Console.WriteLine(string.Format("Garmin Client authenticated. DisplayName='{0}'", gc.DisplayName));
        
         var gda = new GarminDataAccess(gc);
         
         var activities = gda.GetLatestActivities();
    }
}
```
