using Abp.Zero.EntityFrameworkCore;
using KiemTraGiuaKy.Authorization.Roles;
using KiemTraGiuaKy.Authorization.Users;
using KiemTraGiuaKy.MultiTenancy;
using Microsoft.EntityFrameworkCore;

using KiemTraGiuaKy.Exams;

namespace KiemTraGiuaKy.EntityFrameworkCore;

public class KiemTraGiuaKyDbContext : AbpZeroDbContext<Tenant, Role, User, KiemTraGiuaKyDbContext>
{
    /* Define a DbSet for each entity of the application */
    public DbSet<Exam> Exams { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionAnswer> QuestionAnswers { get; set; }

    public KiemTraGiuaKyDbContext(DbContextOptions<KiemTraGiuaKyDbContext> options)
        : base(options)
    {
    }
}
