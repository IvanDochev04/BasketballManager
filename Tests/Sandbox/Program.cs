namespace Sandbox
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    using BasketballManager.Data;
    using BasketballManager.Data.Common;
    using BasketballManager.Data.Common.Repositories;
    using BasketballManager.Data.Models;
    using BasketballManager.Data.Repositories;
    using BasketballManager.Data.Seeding;
    using BasketballManager.Services.Data;
    using BasketballManager.Services.Messaging;

    using CommandLine;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.WriteLine($"{typeof(Program).Namespace} ({string.Join(" ", args)}) starts working...");
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider(true);

            // Seed data on application startup
            using (var serviceScope = serviceProvider.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
                new ApplicationDbContextSeeder().SeedAsync(dbContext, serviceScope.ServiceProvider).GetAwaiter().GetResult();
            }

            using (var serviceScope = serviceProvider.CreateScope())
            {
                serviceProvider = serviceScope.ServiceProvider;

                return Parser.Default.ParseArguments<SandboxOptions>(args).MapResult(
                    opts => SandboxCode(opts, serviceProvider).GetAwaiter().GetResult(),
                    _ => 255);
            }
        }

        private static async Task<int> SandboxCode(SandboxOptions options, IServiceProvider serviceProvider)
        {
            var sw = Stopwatch.StartNew();

            var settingsService = serviceProvider.GetService<ISettingsService>();
            Console.WriteLine($"Count of settings: {settingsService.GetCount()}");
            Console.WriteLine("Level: ");
            var level = int.Parse(Console.ReadLine());
            Console.WriteLine("Position: ");
            var position = Console.ReadLine();
            var attributeService = serviceProvider.GetService<IAttributesService>();
            var attributes = await attributeService.AddAttributesAsync(position, level);
            Console.WriteLine(attributes.Id);
            Console.WriteLine(attributes.Level);
            Console.WriteLine(attributes.Position);
            Console.WriteLine(attributes.Shooting);
            Console.WriteLine(attributes.Rebounding);
            Console.WriteLine(attributes.Assisting);
            Console.WriteLine(attributes.Stealing);
            Console.WriteLine(attributes.Blocking);
            Console.WriteLine();
            Console.WriteLine(sw.Elapsed);
            return await Task.FromResult(0);
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false, true)
                .AddEnvironmentVariables()
                .Build();

            services.AddSingleton<IConfiguration>(configuration);

            services.AddDbContext<ApplicationDbContext>(
                options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                    .UseLoggerFactory(new LoggerFactory()));

            services.AddDefaultIdentity<ApplicationUser>(IdentityOptionsProvider.GetIdentityOptions)
                .AddRoles<ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddScoped(typeof(IDeletableEntityRepository<>), typeof(EfDeletableEntityRepository<>));
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddScoped<IDbQueryRunner, DbQueryRunner>();
            services.AddScoped<IAttributesService, AttributesService>();

            // Application services
            services.AddTransient<IEmailSender, NullMessageSender>();
            services.AddTransient<ISettingsService, SettingsService>();
        }
    }
}
