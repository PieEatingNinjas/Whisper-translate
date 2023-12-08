using WhisperTranslate.API;
using MinimalHelpers.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddMissingSchemas();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();


app.MapPost("/translate", async (IFormFile file, string to) =>
{
    var stream = new MemoryStream((int)file.Length);
    file.CopyTo(stream);

    return await new TranslationService().Translate(BinaryData.FromBytes(stream.ToArray()), to);
})
.WithName("TranslateAudio")
.WithOpenApi();

app.Run();
