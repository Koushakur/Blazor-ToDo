using Microsoft.EntityFrameworkCore;

namespace Blazor_ToDo_App.Data;

public class UsersContext(IConfiguration configuration) : DbContext {
    protected readonly IConfiguration Configuration = configuration;
    public DbSet<User>? Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        optionsBuilder.UseSqlite(Configuration.GetConnectionString("UserDB"));
    }
}