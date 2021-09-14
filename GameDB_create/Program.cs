using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDB_create
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                await Create();
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
        }

        static async Task Create()
        {
            using (var ctx = new GameContext())
            {
                bool created = await ctx.Database.EnsureCreatedAsync();
                Console.WriteLine(created == true ? "database created" : "database exists");
            }
        }
    }
}
