using System;
using ServerLibrary.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static void Main(string[] args)
    {
        var factory = new AppDbContextFactory();
        using var dbContext = factory.CreateDbContext(args);

        // Example query to verify database connectivity
        try
        {
            var userCount = dbContext.Users.Count();
            Console.WriteLine($"Number of users in the database: {userCount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }

        Console.WriteLine("DbContext created successfully.");
    }
}
