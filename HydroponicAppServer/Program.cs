using Microsoft.EntityFrameworkCore;
using HydroponicAppServer;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;

// Thêm các using cho MQTT
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve;
    });

// Kết nối DbContext với SQL Server, lấy chuỗi kết nối từ appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Đăng ký các dịch vụ MQTTnet (nếu muốn inject MQTT client cho service khác)
builder.Services.AddSingleton<IMqttFactory, MqttFactory>();
builder.Services.AddSingleton<IMqttClient>(sp =>
{
    var factory = sp.GetRequiredService<IMqttFactory>();
    return factory.CreateMqttClient();
});

// Nếu bạn có BackgroundService sử dụng MQTT (ví dụ MQTTGlobalListener)
// builder.Services.AddHostedService<HydroponicAppServer.Services.MQTTGlobalListener>();

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