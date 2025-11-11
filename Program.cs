using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using OrquestadorGeoPredio.Data;
using OrquestadorGeoPredio.Repositories;
using OrquestadorGeoPredio.Services;
using OrquestadorGeoPredio.Services.Factories;

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ Lee la cadena de conexión desde appsettings.json (usa User Secrets en desarrollo si prefieres)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2️⃣ Registra el DbContext con resiliencia ante fallos transitorios y soporte para NetTopologySuite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Habilita soporte para tipos espaciales (geometry, geography)
        sqlOptions.UseNetTopologySuite();

        // 🔹 Habilita reintentos automáticos ante errores transitorios de conexión
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,                    // Número máximo de reintentos
            maxRetryDelay: TimeSpan.FromSeconds(10), // Tiempo máximo entre reintentos
            errorNumbersToAdd: null              // Usa los errores transitorios por defecto de SQL Server
        );
    });
});

// 3️⃣ Registro de dependencias de la capa de datos y lógica
builder.Services.AddScoped<ICrTerrenoRepository, CrTerrenoRepository>();
builder.Services.AddScoped<CrTerrenoFactory>();       // Mapea DTO -> Entidad
builder.Services.AddScoped<OcrCrTerrenoService>();    // Servicio que llama al OCR y crea terreno

// 4️⃣ Cliente HTTP que se usará para enviar archivos al servicio OCR externo (Python)
//builder.Services.AddHttpClient();
builder.Services.AddHttpClient("OCR", client =>
{
    client.Timeout = TimeSpan.FromMinutes(8); // Espera hasta 5 minutos
});

// 5️⃣ Controladores y Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 6️⃣ Construcción del pipeline de la aplicación
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
