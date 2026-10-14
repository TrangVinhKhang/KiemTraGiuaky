using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace KiemTraGiuaKy.EntityFrameworkCore;

public static class KiemTraGiuaKyDbContextConfigurer
{
    public static void Configure(DbContextOptionsBuilder<KiemTraGiuaKyDbContext> builder, string connectionString)
    {
        builder.UseSqlServer(connectionString);
    }

    public static void Configure(DbContextOptionsBuilder<KiemTraGiuaKyDbContext> builder, DbConnection connection)
    {
        builder.UseSqlServer(connection);
    }
}
