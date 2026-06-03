using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TursoEFCoreDemo.Configuration;
using TursoEFCoreDemo.Models;

namespace TursoEFCoreDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .AddTursoConfiguration()
                .Build();

            var appSettings = configuration.GetAppSettings();
            Console.WriteLine($"Starting application in {appSettings.Environment} environment...");

            Console.WriteLine("正在连接 Turso 数据库...");

            try
            {
                using var context = new AppDbContext(configuration);

                // 可选：确保数据库创建/连接有效，生产环境通常用 Migrations
                // await context.Database.EnsureCreatedAsync();

                // 执行查询
                var tours = await context.Tours.ToListAsync();

                Console.WriteLine($"成功查询到 {tours.Count} 条数据:");

                foreach (var tour in tours)
                {
                    Console.WriteLine(tour);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"数据库查询异常: {ex.Message}");

                if (appSettings.EnableDetailedErrors)
                {
                    Console.WriteLine($"详细错误信息: {ex}");
                }
            }
        }
    }
}