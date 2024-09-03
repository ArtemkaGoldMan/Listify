using Microsoft.EntityFrameworkCore;
using BaseLibrary.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ServerLibrary.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ListOfContent> ListOfContents { get; set; }
        public DbSet<Content> Contents { get; set; }
        public DbSet<ListOfTags> ListOfTags { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ContentTag> ContentTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure primary keys
            modelBuilder.Entity<User>().HasKey(u => u.UserID);
            modelBuilder.Entity<ListOfContent>().HasKey(lc => lc.ListOfContentID);
            modelBuilder.Entity<Content>().HasKey(c => c.ContentID);
            modelBuilder.Entity<ListOfTags>().HasKey(lt => lt.ListOfTagsID);
            modelBuilder.Entity<Tag>().HasKey(t => t.TagID);
            modelBuilder.Entity<ContentTag>().HasKey(ct => new { ct.ContentID, ct.TagID });

            // Configure relationships
            modelBuilder.Entity<User>()
                .HasOne(u => u.ListOfContent)
                .WithOne(lc => lc.User)
                .HasForeignKey<ListOfContent>(lc => lc.UserID);

            modelBuilder.Entity<ListOfContent>()
                .HasMany(lc => lc.Contents)
                .WithOne(c => c.ListOfContent)
                .HasForeignKey(c => c.ListOfContentID);

            modelBuilder.Entity<ListOfTags>()
                .HasMany(lt => lt.Tags)
                .WithOne(t => t.ListOfTags)
                .HasForeignKey(t => t.ListOfTagsID);

            modelBuilder.Entity<ContentTag>()
                .HasOne(ct => ct.Content)
                .WithMany(c => c.ContentTags)
                .HasForeignKey(ct => ct.ContentID);

            modelBuilder.Entity<ContentTag>()
                .HasOne(ct => ct.Tag)
                .WithMany(t => t.ContentTags)
                .HasForeignKey(ct => ct.TagID);

        }
    }
}
