# EF Core `Include` & `Clear`

This repositoru contains an experiment in seeing whether when pulling in an
entity through a `Include` call and `Clear`ing a set on it of associated
entities will work with the EF change tracker.

This is similar to my other experiment where I was exploring options of
clearing an entity's set of associated entities without using `Include`.

The model of this experiment is as follows:

- `User` has an associated entity `Car`
- `Car` has a set of associated entities `Trip`
- `Trip`

In our experiment, we are looking to see if we pull in `Car` by knowing its
owner `User` and clear its `Trip`s, whether EF Core change tracker will
understand this.

The code for that might look like this:

```csharp
void Main() {
    using (var appDbContext = new AppDbContext()) {
        var user appDbContext.Users.Include(u => u.Car).ThenInclude(c => c.Trips).SingleOrDefault(u => u.Id == 1);
        user.Car.Trips.Clear();
        appDbContext.SaveChanges();
    }
}
```

Will this code result in the car's trips being cleared?

Let's start by creating the application: `dotnet new console`. Next we install
the EF Core NuGet package as well as the SQL Server provider, because we will
use LocalDB for the demo. We also add Netwonsoft JSON for pretty-printing
entities.

```powershell
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Newtonsoft.Json
```

Uncheck other package sources in Visual Studio > Tools > Options > NuGet … in
case you are experiencing issues installing the packages from the official
package repository.

With the application created, let's create the database:
`sqllocaldb create ef_core_include_clear -s`. We call it this way so that the
database name matches the generated application `namespace` name, which itself
is derived from the repository directory name. This way we can use `nameof` in
the connection string. `-s` starts the database up after creation.

The application database context class will be really simple:

```csharp
using Microsoft.EntityFrameworkCore;

public class AppDbContext: DbContext
{
    // TODO: Add the DbSet properties here
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer($@"Server=(localdb)\{nameof(ef_core_include_clear)};Database={nameof(ef_core_include_clear)};");
    }
}
```

We can try connecting to the database and for good measure we'll make it so that
the database is dropped and recreated before each demo run so that the demo is
consistent across runs.

```csharp
static void Main()
{
    using (var appDbContext = new AppDbContext())
    {
        appDbContext.Database.EnsureDeleted();
        appDbContext.Database.EnsureCreated();
        Console.WriteLine("The database has been reset.");
    }
}
```

We can test that the database connect and the application runs without crashing
by issuing the `dotnet run` command.

Next up we will wire up the model classes and for each of them add a respective
`DbSet` property to the application database context class.

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Car Car { get; set; }
    public int CarId { get; set; }
}

public class Car
{
    public int Id { get; set; }
    public string Model { get; set; }
    public string Make { get; set; }
    public ICollection<Trip> Trips { get; set; }
}

public class Trip
{
    public int Id { get; set; }
    public DateTime DateAndTime { get; set; }
    public int DistanceInKilometers { get; set; }
    public Car Car { get; set; }
    public int CarId { get; set; }
}
```

We also add some seed data in the `Main` method and add another `using` block
for the app DB context (so that we get a fresh instance of the change tracker)
in which we print the database contents after a successful save for inspection.

The easiest way to print the database state is to serialize it as JSON. We will
serialize the whole `AppDbContext` class and we will mark it as
`[JsonObject(MemberSerialization.OptIn)]` and the `DbSet` properties as
`[JsonProperty]` so that no base class properties get serialized - we just care
about data.

Now's the time for the transaction which is the core of this experiment. We'll
duplicate the database-printing block and slide this one in between the other
two so that we print the database before and after the change.

```csharp
static void Main()
{
    // …pre
    using (var appDbContext = new AppDbContext())
    {
        var user = appDbContext.Users.Include(u => u.Car).ThenInclude(c => c.Trips).SingleOrDefault(u => u.Id == 1);
        user.Car.Trips.Clear();
        appDbContext.SaveChanges();
    }
    // …post
}
```

And the code works! Like in the other experiment, we still need to make the
relationships known, we cannot avoid the `Include` without using SQL directly
(which strips us of the ability to use the in memory database), but this is
confirmed to work.

## To-Do
