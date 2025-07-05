using Microsoft.EntityFrameworkCore;
using HydroponicAppServer;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;

// Đăng ký các service MQTT và cache cảm biến
using HydroponicAppServer.MQTT;

// Add services to the container.
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

// Đăng ký các service MQTT nếu cần (ví dụ nếu có MQTT listener chạy nền thì đăng ký tương tự như SensorDataTimedLogger)
// builder.Services.AddHostedService<MqttListenerService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Thêm cấu hình Forwarded Headers cho proxy/ngrok
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Nếu dùng ngrok HTTPS thì giữ dòng này. Nếu chỉ dùng HTTP thì có thể comment đi.
// app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();