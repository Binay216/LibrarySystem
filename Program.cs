using LibrarySystem.Data;
using LibrarySystem.Models.Dto;
using LibrarySystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Reflection;

// QuestPDF requires an explicit licence declaration before any document is
// generated. Community is free for this project (a non-commercial student
// app under QuestPDF's revenue/team-size thresholds).
QuestPDF.Settings.License = LicenseType.Community;

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

// No existing controller has [ApiController] - only the new api/v1
// controllers (Controllers/Api) do - so this only changes behavior for
// those: a model-binding failure (e.g. an unparseable query param) gets the
// same {statusCode, message} shape as every other api/v1 error, instead of
// the default ProblemDetails response [ApiController] would otherwise produce.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        // ModelState's own ErrorMessage text for a binding failure (e.g. an
        // enum that doesn't parse) often doesn't name the field at all - just
        // "The value 'X' is not valid." - so the field name (the ModelState
        // key) is prepended explicitly rather than trusting the message text
        // to already include it. Every invalid field is listed, not just the
        // first - a request can fail to bind more than one parameter at once
        // (e.g. ?type=Bad&page=abc), and the caller needs to see all of them
        // in one response rather than fixing them one at a time.
        var fieldMessages = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .Select(kvp =>
            {
                var errorText = kvp.Value!.Errors
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "The value provided is invalid.";
                return $"{kvp.Key}: {errorText}";
            })
            .ToList();

        var message = fieldMessages.Count > 0
            ? string.Join(" | ", fieldMessages)
            : "The request was invalid.";

        return new BadRequestObjectResult(new ApiErrorDto(StatusCodes.Status400BadRequest, message));
    };
});

builder.Services.Configure<ApiKeyOptions>(builder.Configuration.GetSection(ApiKeyOptions.SectionName));
builder.Services.Configure<FinePolicyOptions>(builder.Configuration.GetSection(FinePolicyOptions.SectionName));
builder.Services.Configure<LibraryClockOptions>(builder.Configuration.GetSection(LibraryClockOptions.SectionName));
builder.Services.Configure<LibraryOptions>(builder.Configuration.GetSection(LibraryOptions.SectionName));
builder.Services.AddSingleton<ILibraryClock, LibraryClock>();
builder.Services.AddScoped<ILoanService, LoanService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<ICurrentBranchAccessor, CurrentBranchAccessor>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationSweepService, NotificationSweepService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IImportService, ImportService>();

// Swagger - services are always registered (cheap, harmless), but the UI
// middleware is only mapped in Development, below. "Try it out" needs the
// X-Api-Key header or every call just gets ApiKeyAuthFilterAttribute's 401,
// so the security definition/requirement here adds an Authorize button to
// the UI that attaches the header to every request once filled in.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Library System API",
        Version = "v1",
        Description = "Public, read-only catalogue API. Every endpoint requires an X-Api-Key header - click Authorize above and paste in the dev key from appsettings.json (ApiKey:Key)."
    });

    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFile))
    {
        options.IncludeXmlComments(xmlFile);
    }

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "API key for the api/v1 endpoints (see ApiKey:Key in appsettings.json)."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("ApiKey", document), new List<string>() }
    });
});

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

    // Swagger UI, Development only - at /swagger. Not exposed in production:
    // this is a demo/student project's dev tool, not something meant to be
    // publicly reachable once deployed.
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Library System API v1");
    });
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

// Attribute-routed API controllers (Controllers/Api) - explicit alongside the
// conventional MVC route above for clarity, now that the app has a formal
// api/v1 surface. Safe to have both: the framework dedupes by action, not by
// call site, so this doesn't double-register the conventional routes.
app.MapControllers();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
