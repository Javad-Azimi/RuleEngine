using Blazored.Modal;
using Blazored.Toast;
using TadbirRuleEngine.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add HTTP client for API calls
builder.Services.AddHttpClient("TadbirApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:55033/api/");
});

// Add API services
builder.Services.AddScoped<ApiService>();

// Add Blazored components
builder.Services.AddBlazoredModal();
builder.Services.AddBlazoredToast();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();