using CalculatorASPNet.Data;
using CalculatorASPNet.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CalculatorDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CalculatorASPNet")
        ?? throw new InvalidOperationException("Connection string 'CalculatorASPNet' not found.")));

builder.Services.AddScoped<CalculatorService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CalculatorDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();