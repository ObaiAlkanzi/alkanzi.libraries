using System.Text;
using Alkanzi.Erp.Api.Infrastructure;
using Alkanzi.Erp.Application.Abstractions;
using Alkanzi.Erp.DataAccess;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings:Postgres is not configured.");

// The same context, Identity stores and ports the MVC app composes. The API is another
// delivery mechanism over one application, not a second copy of it.
builder.Services.AddErpDataAccess<HttpAuditUserProvider>(connectionString);
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<TokenService>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

// Refused at startup rather than at the first request: a missing key would otherwise surface
// as a confusing 500 during a login attempt, and a weak one would quietly weaken every token.
// 32 bytes is the minimum for HMAC-SHA256 to carry its nominal strength.
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is missing or shorter than 32 bytes. Set it with: " +
        "dotnet user-secrets set \"Jwt:SigningKey\" \"<a long random string>\" --project apps/Alkanzi.Erp.Api");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,

            // No grace period on expiry. The default allows five minutes of clock skew, which
            // silently extends every token's life; server clocks here are synchronised.
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// The AngularJS front end is served from the MVC app on a different port, which makes every
// call cross-origin. Named and explicit rather than AllowAnyOrigin, because credentials
// cannot be sent to a wildcard origin.
const string CorsPolicy = "ErpClients";
builder.Services.AddCors(o => o.AddPolicy(CorsPolicy, p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5210"])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);

// Authentication before authorization: reversed, every [Authorize] would see an anonymous caller.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
