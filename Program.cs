using LibrarySystem.Data;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddControllersWithViews();

builder.Services.Configure<FinePolicyOptions>(builder.Configuration.GetSection(FinePolicyOptions.SectionName));
builder.Services.Configure<LibraryClockOptions>(builder.Configuration.GetSection(LibraryClockOptions.SectionName));
builder.Services.Configure<LibraryOptions>(builder.Configuration.GetSection(LibraryOptions.SectionName));
builder.Services.AddSingleton<ILibraryClock, LibraryClock>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<ICurrentBranchAccessor, CurrentBranchAccessor>();
builder.Services.AddScoped<ITransferService, TransferService>();

var app = builder.Build();

// Automatic setup: apply pending migrations (creating the database if needed)
// and seed roles/accounts so a fresh clone works with F5 alone, no manual steps.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    await IdentitySeed.InitializeAsync(scope.ServiceProvider);
    SeedData.Initialize(context, scope.ServiceProvider.GetRequiredService<ILibraryClock>());

    // Refuse to start misconfigured rather than fail oddly later: the configured
    // branch code must match a branch that was actually seeded.
    var libraryOptions = scope.ServiceProvider.GetRequiredService<IOptions<LibraryOptions>>().Value;
    var seededBranchCodes = context.Branches.Select(b => b.Code).ToList();
    if (!seededBranchCodes.Contains(libraryOptions.BranchCode))
    {
        throw new InvalidOperationException(
            $"Configured branch code '{libraryOptions.BranchCode}' (Library:BranchCode in appsettings.json) does not match any seeded branch. Valid codes: {string.Join(", ", seededBranchCodes)}.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
