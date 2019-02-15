using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace ef_core_include_clear
{
    class Program
    {
        static void Main()
        {
            using (var appDbContext = new AppDbContext())
            {
                appDbContext.Database.EnsureDeleted();
                appDbContext.Database.EnsureCreated();
                Console.WriteLine("The database has been reset.");
                appDbContext.Users.Add(new User() {
                    Name = "Tomas Hubelbauer",
                    Car = new Car() {
                        Model = "Tesla",
                        Make = "3",
                        Trips = new Trip[] {
                            new Trip() {
                                DateAndTime = DateTime.Now,
                                DistanceInKilometers = 10,
                            },
                            new Trip() {
                                DateAndTime = DateTime.Now,
                                DistanceInKilometers = 20,
                            },
                            new Trip() {
                                DateAndTime = DateTime.Now,
                                DistanceInKilometers = 30,
                            },
                        },
                    },
                });

                appDbContext.SaveChanges();
            }

            using (var appDbContext = new AppDbContext())
            {
                var settings = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                // Prime the object first so that lazy loaded properties do not load incrementally as we serialize
                JsonConvert.SerializeObject(appDbContext, settings);
                Console.WriteLine("Pre:");
                Console.WriteLine(JsonConvert.SerializeObject(appDbContext, Formatting.Indented, settings));
            }

            using (var appDbContext = new AppDbContext())
            {
                var user = appDbContext.Users.Include(u => u.Car).ThenInclude(c => c.Trips).SingleOrDefault(u => u.Id == 1);
                user.Car.Trips.Clear();
                appDbContext.SaveChanges();
            }

            using (var appDbContext = new AppDbContext())
            {
                var settings = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
                // Prime the object first so that lazy loaded properties do not load incrementally as we serialize
                JsonConvert.SerializeObject(appDbContext, settings);
                Console.WriteLine("Post:");
                Console.WriteLine(JsonConvert.SerializeObject(appDbContext, Formatting.Indented, settings));
            }
        }
    }

    [JsonObject(MemberSerialization.OptIn)] public class AppDbContext: DbContext
    {
        [JsonProperty] public DbSet<User> Users { get; set; }
        [JsonProperty] public DbSet<Car> Cars { get; set; }
        [JsonProperty] public DbSet<Trip> Trips { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer($@"Server=(localdb)\{nameof(ef_core_include_clear)};Database={nameof(ef_core_include_clear)};");
        }
    }

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
}
