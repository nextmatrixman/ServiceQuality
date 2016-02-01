# Start

## Database Instance

* Make sure you have LocalDB installed on your system.
* Make sure there is an instance called `ServiceQ` within LocalDB.
* [If not get `SqlLocalDB.MSI` now.](http://www.microsoft.com/en-us/download/details.aspx?id=29062 "Download it")
* (You may need to restart.)
* [Add the instance using this guide.](https://www.mssqltips.com/sqlservertip/2694/getting-started-with-sql-server-2012-express-localdb/ "Read more")
  * Eg: `SqlLocalDb create "ServiceQ" -s`
* Do not modify appsettings.json!

## Database Tables

You will need to have the `dnvm` command available on your system.

If you don't have it installed, [follow this giude](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html "Right now").

* Go to the `src/ServiceQuality` directory
* Run the command: `dnx ef database update`

You'll see the following as it generates the database.

```
Applying migration '00000000000000_CreateIdentitySchema'.
Applying migration
Done.
```

## Run the App

Simply press the green play button on Visual Studio!

Then navigate to /Service

You'll see a "Create" link, so create a new one.

Start modifying the `ServiceController` and anything else!
