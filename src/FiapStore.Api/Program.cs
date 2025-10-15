using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configuração do pipeline HTTP
app.UseSwagger();
app.UseSwaggerUI();

// Health Check
app.MapHealthChecks("/health");

// Endpoint de informações do ambiente
app.MapGet("/info", () =>
{
    var info = new
    {
        Application = "FIAP Store API",
        Version = "1.0.0",
        Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        HostName = Environment.MachineName,
        Platform = Environment.OSVersion.Platform.ToString(),
        ProcessorCount = Environment.ProcessorCount,
        Timestamp = DateTime.UtcNow
    };
    return Results.Ok(info);
})
.WithName("GetInfo")
.WithOpenApi();

// Simulação de banco de dados em memória
var produtos = new List<Produto>
{
    new Produto(1, "Notebook Dell", "Notebook Dell Inspiron 15", 3500.00m, 10),
    new Produto(2, "Mouse Logitech", "Mouse sem fio Logitech MX Master", 350.00m, 50),
    new Produto(3, "Teclado Mecânico", "Teclado Mecânico RGB", 450.00m, 30),
    new Produto(4, "Monitor LG", "Monitor LG 27 polegadas 4K", 1800.00m, 15),
    new Produto(5, "Webcam Logitech", "Webcam Full HD 1080p", 400.00m, 25)
};

// GET /api/produtos - Lista todos os produtos
app.MapGet("/api/produtos", () =>
{
    app.Logger.LogInformation("Listando todos os produtos. Total: {Count}", produtos.Count);
    return Results.Ok(produtos);
})
.WithName("GetProdutos")
.WithOpenApi();

// GET /api/produtos/{id} - Busca produto por ID
app.MapGet("/api/produtos/{id:int}", (int id) =>
{
    var produto = produtos.FirstOrDefault(p => p.Id == id);
    
    if (produto is null)
    {
        app.Logger.LogWarning("Produto com ID {Id} não encontrado", id);
        return Results.NotFound(new { Message = $"Produto com ID {id} não encontrado" });
    }
    
    app.Logger.LogInformation("Produto encontrado: {Nome}", produto.Nome);
    return Results.Ok(produto);
})
.WithName("GetProdutoById")
.WithOpenApi();

// POST /api/produtos - Cria novo produto
app.MapPost("/api/produtos", ([FromBody] NovoProduto novoProduto) =>
{
    var novoId = produtos.Max(p => p.Id) + 1;
    var produto = new Produto(
        novoId,
        novoProduto.Nome,
        novoProduto.Descricao,
        novoProduto.Preco,
        novoProduto.Estoque
    );
    
    produtos.Add(produto);
    app.Logger.LogInformation("Produto criado: {Nome} (ID: {Id})", produto.Nome, produto.Id);
    
    return Results.Created($"/api/produtos/{produto.Id}", produto);
})
.WithName("CreateProduto")
.WithOpenApi();

// PUT /api/produtos/{id} - Atualiza produto
app.MapPut("/api/produtos/{id:int}", (int id, [FromBody] NovoProduto produtoAtualizado) =>
{
    var produto = produtos.FirstOrDefault(p => p.Id == id);
    
    if (produto is null)
    {
        app.Logger.LogWarning("Tentativa de atualizar produto inexistente. ID: {Id}", id);
        return Results.NotFound(new { Message = $"Produto com ID {id} não encontrado" });
    }
    
    var index = produtos.IndexOf(produto);
    produtos[index] = new Produto(
        id,
        produtoAtualizado.Nome,
        produtoAtualizado.Descricao,
        produtoAtualizado.Preco,
        produtoAtualizado.Estoque
    );
    
    app.Logger.LogInformation("Produto atualizado: {Nome} (ID: {Id})", produtoAtualizado.Nome, id);
    return Results.Ok(produtos[index]);
})
.WithName("UpdateProduto")
.WithOpenApi();

// DELETE /api/produtos/{id} - Remove produto
app.MapDelete("/api/produtos/{id:int}", (int id) =>
{
    var produto = produtos.FirstOrDefault(p => p.Id == id);
    
    if (produto is null)
    {
        app.Logger.LogWarning("Tentativa de deletar produto inexistente. ID: {Id}", id);
        return Results.NotFound(new { Message = $"Produto com ID {id} não encontrado" });
    }
    
    produtos.Remove(produto);
    app.Logger.LogInformation("Produto removido: {Nome} (ID: {Id})", produto.Nome, id);
    
    return Results.NoContent();
})
.WithName("DeleteProduto")
.WithOpenApi();

app.Logger.LogInformation("🚀 FIAP Store API iniciada com sucesso!");
app.Logger.LogInformation("📚 Swagger disponível em: /swagger");
app.Logger.LogInformation("❤️ Health Check disponível em: /health");

app.Run();

// Records para os modelos
record Produto(int Id, string Nome, string Descricao, decimal Preco, int Estoque);
record NovoProduto(string Nome, string Descricao, decimal Preco, int Estoque);
