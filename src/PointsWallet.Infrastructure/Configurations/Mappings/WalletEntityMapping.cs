using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PointsWallet.Domain.Models;

namespace PointsWallet.Infrastructure.Configurations.Mappings;

public class WalletEntityMapping : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(w => w.Id);
        
        builder.Property(w => w.Id)
            .ValueGeneratedNever();

        builder.Property(w => w.Points)
            .IsRequired();

        builder.Property(w => w.UserId)
            .IsRequired()
            .HasMaxLength(36);

        builder.Property(w => w.SymbolicName)
            .HasMaxLength(100);

        builder.Property(w => w.CreatedAt)
            .IsRequired();

        builder.Property(w => w.UpdatedAt)
            .IsRequired();

        builder.HasIndex(w => w.UserId);
    }
}
