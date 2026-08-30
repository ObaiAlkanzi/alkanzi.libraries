using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Alkanzi.Erp.DataAccess.Search;

public class SearchDocumentConfiguration : IEntityTypeConfiguration<SearchDocument>
{
    public void Configure(EntityTypeBuilder<SearchDocument> e)
    {
        e.ToTable("search_documents");
        e.Property(x => x.EntityType).HasMaxLength(40).IsRequired();
        e.Property(x => x.Label).HasMaxLength(100).IsRequired();
        e.Property(x => x.Title).HasMaxLength(400).IsRequired();
        e.Property(x => x.Subtitle).HasMaxLength(400);

        e.HasIndex(x => new { x.EntityType, x.EntityId }).IsUnique();

        // Written as raw SQL rather than with HasGeneratedTsVectorColumn, because that helper
        // concatenates every column into one to_tsvector call, which makes all matches equally
        // relevant. Weighting each source separately is what lets ts_rank put a title hit above
        // an incidental keyword hit: A title, B subtitle, C the keyword blob.
        //
        // 'simple' rather than 'english': ERP content is mostly proper nouns, codes and
        // document numbers, where English stemming does more harm than good.
        e.Property(x => x.SearchVector)
         .HasColumnType("tsvector")
         .HasComputedColumnSql(
             "setweight(to_tsvector('simple', coalesce(title, '')), 'A') || " +
             "setweight(to_tsvector('simple', coalesce(subtitle, '')), 'B') || " +
             "setweight(to_tsvector('simple', coalesce(keywords, '')), 'C')",
             stored: true);

        e.HasIndex(x => x.SearchVector).HasMethod("GIN");
    }
}
