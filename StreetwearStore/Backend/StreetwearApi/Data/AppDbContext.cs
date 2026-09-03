using Microsoft.EntityFrameworkCore;
using StreetwearApi.Models;

namespace StreetwearApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Pedido> Pedidos => Set<Pedido>();
    public DbSet<ItemPedido> ItensPedido => Set<ItemPedido>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Usuario>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Pedido>()
            .HasMany(p => p.Itens)
            .WithOne()
            .HasForeignKey(i => i.PedidoId);

        // Alguns produtos de exemplo para a loja não começar vazia
        modelBuilder.Entity<Produto>().HasData(
            new Produto { Id = 1, Nome = "Camiseta Oversized Concrete", Descricao = "Camiseta oversized 100% algodão, estampa frente e verso.", Categoria = "Camiseta", Preco = 129.90m, ImagemUrl = "img/camiseta-concrete.jpg", TamanhosDisponiveis = "P,M,G,GG", EstoqueTotal = 50 },
            new Produto { Id = 2, Nome = "Moletom Canguru Blackout", Descricao = "Moletom flanelado com capuz e bolso canguru.", Categoria = "Moletom", Preco = 249.90m, ImagemUrl = "img/moletom-blackout.jpg", TamanhosDisponiveis = "P,M,G,GG,XG", EstoqueTotal = 30 },
            new Produto { Id = 3, Nome = "Boné Aba Reta Signal", Descricao = "Boné aba reta com bordado 3D.", Categoria = "Boné", Preco = 89.90m, ImagemUrl = "img/bone-signal.jpg", TamanhosDisponiveis = "Único", EstoqueTotal = 80 },
            new Produto { Id = 4, Nome = "Calça Cargo Asphalt", Descricao = "Calça cargo com bolsos laterais e cordão ajustável.", Categoria = "Calça", Preco = 219.90m, ImagemUrl = "img/calca-asphalt.jpg", TamanhosDisponiveis = "36,38,40,42,44", EstoqueTotal = 40 }
        );
    }
}
