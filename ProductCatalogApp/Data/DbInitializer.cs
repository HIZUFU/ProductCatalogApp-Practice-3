using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProductCatalogApp.Models;

namespace ProductCatalogApp.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var context = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var passwordHasher = scope.ServiceProvider
                .GetRequiredService<IPasswordHasher<AppUser>>();

            await context.Database.MigrateAsync();

            var adminRole = await context.Roles
                .FirstOrDefaultAsync(role => role.Name == "Admin");

            if (adminRole == null)
            {
                adminRole = new Role
                {
                    Name = "Admin"
                };

                context.Roles.Add(adminRole);
                await context.SaveChangesAsync();
            }

            var admin = await context.Users
                .FirstOrDefaultAsync(
                    user => user.Username == "admin");

            if (admin == null)
            {
                admin = new AppUser
                {
                    Username = "admin",
                    RoleId = adminRole.Id
                };

                admin.PasswordHash =
                    passwordHasher.HashPassword(
                        admin,
                        "Admin123!");

                context.Users.Add(admin);
                await context.SaveChangesAsync();
            }
            else if (admin.RoleId != adminRole.Id)
            {
                admin.RoleId = adminRole.Id;
                await context.SaveChangesAsync();
            }

            await SeedSensorsAsync(context);
        }

        private static async Task SeedSensorsAsync(
            ApplicationDbContext context)
        {
            if (await context.Sensors.AnyAsync())
            {
                return;
            }

            var blastFurnaceShop = new ProductionSite
            {
                Name = "Доменный цех",
                Description = "Производство чугуна"
            };

            var steelmakingShop = new ProductionSite
            {
                Name = "Конвертерный цех",
                Description = "Выплавка стали в кислородных конвертерах"
            };

            var blastFurnace = new Equipment
            {
                Name = "Доменная печь №1",
                Model = "ДП-2000",
                InventoryNumber = "ДЦ-0001",
                ProductionSite = blastFurnaceShop
            };

            var converter = new Equipment
            {
                Name = "Кислородный конвертер №1",
                Model = "КК-160",
                InventoryNumber = "КЦ-0001",
                ProductionSite = steelmakingShop
            };

            context.Sensors.AddRange(
                new Sensor
                {
                    Name = "Температура горячего дутья",
                    Unit = "°C",
                    MinValue = 1100,
                    MaxValue = 1250,
                    Equipment = blastFurnace
                },
                new Sensor
                {
                    Name = "Давление колошникового газа",
                    Unit = "кПа",
                    MinValue = 150,
                    MaxValue = 250,
                    Equipment = blastFurnace
                },
                new Sensor
                {
                    Name = "Температура колошника",
                    Unit = "°C",
                    MinValue = 150,
                    MaxValue = 300,
                    Equipment = blastFurnace
                },
                new Sensor
                {
                    Name = "Расход охлаждающей воды",
                    Unit = "м³/ч",
                    MinValue = 2500,
                    MaxValue = 3500,
                    Equipment = blastFurnace
                },
                new Sensor
                {
                    Name = "Температура жидкой стали",
                    Unit = "°C",
                    MinValue = 1580,
                    MaxValue = 1680,
                    Equipment = converter
                },
                new Sensor
                {
                    Name = "Расход кислорода на продувку",
                    Unit = "м³/мин",
                    MinValue = 450,
                    MaxValue = 600,
                    Equipment = converter
                },
                new Sensor
                {
                    Name = "Вибрация опоры цапфы",
                    Unit = "мм/с",
                    MinValue = 0,
                    MaxValue = 7.1,
                    Equipment = converter
                });

            await context.SaveChangesAsync();
        }
    }
}