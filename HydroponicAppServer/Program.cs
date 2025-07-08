using Microsoft.EntityFrameworkCore;
using HydroponicAppServer;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;

// Đăng ký các service MQTT và cache cảm biến
using HydroponicAppServer.MQTT;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });

// Kết nối DbContext với SQL Server, lấy chuỗi kết nối từ appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký cache cảm biến singleton
builder.Services.AddSingleton<IMqttSensorCache, MqttSensorCache>();

// Đăng ký BackgroundService ghi dữ liệu cảm biến mỗi 30 phút
builder.Services.AddHostedService<SensorDataTimedLogger>();

// ✅ Đăng ký service lắng nghe MQTT và cập nhật cache cảm biến
builder.Services.AddHostedService<MqttListenerService>();  // ← ĐÃ MỞ LẠI

// Cấu hình Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Thêm cấu hình CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Cấu hình proxy/ngrok headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// HTTPS redirect nếu dùng ngrok
// app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
