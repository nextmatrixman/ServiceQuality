# Start

## Database Instance

* Make sure you have installed LocalDB on your system.
* Make sure there is an instance called `ServiceQ` within LocalDB.
* [If not get `SqlLocalDB.MSI` now.](http://www.microsoft.com/en-us/download/details.aspx?id=29062 "Download it")
* (You may need to restart your computer.)
* [Add the instance using this guide.](https://www.mssqltips.com/sqlservertip/2694/getting-started-with-sql-server-2012-express-localdb/ "Read more")
  * Run the command: `SqlLocalDb create "ServiceQ" -s`
* Do not modify appsettings.json!

## Database Tables

* Make sure you have installed ASP.NET 5
* Make sure to install/upgrade the `dnvm` command

[Follow this giude](http://docs.asp.net/en/latest/getting-started/installing-on-windows.html "Right now") on details for both. You may need to restart after installing ASP.NET 5.

Afterwards, you can migrate the database.

* Using the command prompt go to the `src/ServiceQuality` directory
* Run the command: `dnx ef database update`

You'll see the following as it generates the database.

```
Applying migration '00000000000000_CreateIdentitySchema'.
Applying migration
Done.
```

## Run the App

* (Again) Make sure you have installed ASP.NET 5

Simply press the green play button on Visual Studio! If you do not see "IIS Express" as the option to run, go to the previous step and install ASP.NET 5.

After starting, the browser will open to the Service page.

You'll see a "Create" link near the top, so create a new service.

Start modifying the `ServiceController` and anything else!
