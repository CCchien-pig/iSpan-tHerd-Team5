using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FlexBackend.Admin.Models;

public partial class TherdStoreDbContext : DbContext
{
    public TherdStoreDbContext(DbContextOptions<TherdStoreDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CntMedium> CntMedia { get; set; }

    public virtual DbSet<CntPage> CntPages { get; set; }

    public virtual DbSet<CntPageBlock> CntPageBlocks { get; set; }

    public virtual DbSet<CntPageTag> CntPageTags { get; set; }

    public virtual DbSet<CntPageType> CntPageTypes { get; set; }

    public virtual DbSet<CntPurchase> CntPurchases { get; set; }

    public virtual DbSet<CntSchedule> CntSchedules { get; set; }

    public virtual DbSet<CntShareClick> CntShareClicks { get; set; }

    public virtual DbSet<CntTag> CntTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CntMedium>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("PK__CNT_Medi__B2C2B5CFCB8B3D2F");

            entity.ToTable("CNT_Media", tb => tb.HasComment("媒體資源"));

            entity.Property(e => e.MediaId).HasComment("媒體 ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("上傳時間");
            entity.Property(e => e.FileId).HasComment("檔案 ID");
            entity.Property(e => e.PageBlockId).HasComment("對應區塊 ID");

            entity.HasOne(d => d.PageBlock).WithMany(p => p.CntMedia)
                .HasForeignKey(d => d.PageBlockId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CNT_Media_PageBlockId");
        });

        modelBuilder.Entity<CntPage>(entity =>
        {
            entity.HasKey(e => e.PageId).HasName("PK__CNT_Page__C565B104FAC4AEAA");

            entity.ToTable("CNT_Page", tb => tb.HasComment("頁面"));

            entity.HasIndex(e => e.CreatedDate, "IX_CNT_Page_CreatedDate");

            entity.HasIndex(e => new { e.IsDeleted, e.Status }, "IX_CNT_Page_IsDeleted_Status");

            entity.HasIndex(e => e.PageTypeId, "IX_CNT_Page_PageTypeId");

            entity.HasIndex(e => e.PublishedDate, "IX_CNT_Page_PublishedDate");

            entity.HasIndex(e => e.Status, "IX_CNT_Page_Status");

            entity.Property(e => e.PageId).HasComment("頁面 ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("建立時間");
            entity.Property(e => e.IsDeleted).HasComment("是否在垃圾桶");
            entity.Property(e => e.IsPaidContent).HasComment("是否為付費內容");
            entity.Property(e => e.PageTypeId).HasComment("分類 ID");
            entity.Property(e => e.PreviewLength).HasComment("試閱字元數或行數");
            entity.Property(e => e.PublishedDate).HasComment("發布時間");
            entity.Property(e => e.RevisedDate).HasComment("更新時間");
            entity.Property(e => e.SeoId).HasComment("Seo 設定");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("draft")
                .HasComment("頁面狀態");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasComment("頁面標題");

            entity.HasOne(d => d.PageType).WithMany(p => p.CntPages)
                .HasForeignKey(d => d.PageTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CNT_Page_PageTypeId");
        });

        modelBuilder.Entity<CntPageBlock>(entity =>
        {
            entity.HasKey(e => e.PageBlockId).HasName("PK__CNT_Page__E339E4700FB9BB25");

            entity.ToTable("CNT_PageBlock", tb => tb.HasComment("頁面區塊"));

            entity.Property(e => e.PageBlockId).HasComment("區塊 ID");
            entity.Property(e => e.BlockType)
                .HasMaxLength(50)
                .HasComment("區塊類型");
            entity.Property(e => e.Content).HasComment("附文本內容");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("建立時間");
            entity.Property(e => e.ImgId).HasComment("區塊圖片");
            entity.Property(e => e.OrderSeq).HasComment("區塊順序");
            entity.Property(e => e.PageId).HasComment("頁面 ID");
            entity.Property(e => e.RevisedDate).HasComment("更新時間");

            entity.HasOne(d => d.Page).WithMany(p => p.CntPageBlocks)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CNT_PageBlock_PageId");
        });

        modelBuilder.Entity<CntPageTag>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.TagId }).HasName("UQ_CNT_PageTag_PageId_TagId");

            entity.ToTable("CNT_PageTag", tb => tb.HasComment("頁面與標籤關聯表"));

            entity.Property(e => e.PageId).HasComment("頁面 ID");
            entity.Property(e => e.TagId).HasComment("標籤 ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("建立時間");

            entity.HasOne(d => d.Page).WithMany(p => p.CntPageTags)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CNT_PageTag_PageId");

            entity.HasOne(d => d.Tag).WithMany(p => p.CntPageTags)
                .HasForeignKey(d => d.TagId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CNT_PageTag_TagId");
        });

        modelBuilder.Entity<CntPageType>(entity =>
        {
            entity.HasKey(e => e.PageTypeId).HasName("PK__CNT_Page__33FA9A45E0B1AD20");

            entity.ToTable("CNT_PageType", tb => tb.HasComment("頁面分類"));

            entity.HasIndex(e => e.TypeName, "UQ_CNT_PageType_TypeName").IsUnique();

            entity.Property(e => e.PageTypeId).HasComment("分類 ID");
            entity.Property(e => e.TypeName)
                .HasMaxLength(255)
                .HasComment("分類名稱");
        });

        modelBuilder.Entity<CntPurchase>(entity =>
        {
            entity.HasKey(e => e.PurchaseId).HasName("PK__CNT_Purc__6B0A6BBECD4FE8A5");

            entity.ToTable("CNT_Purchase", tb => tb.HasComment("文章/課程購買紀錄"));

            entity.HasIndex(e => e.CreatedDate, "IX_CNT_Purchase_CreatedDate");

            entity.HasIndex(e => e.PageId, "IX_CNT_Purchase_PageId");

            entity.HasIndex(e => e.UserNumberId, "IX_CNT_Purchase_UserNumberId");

            entity.HasIndex(e => new { e.UserNumberId, e.PageId }, "UQ_CNT_Purchase_UserNumberId_PageId").IsUnique();

            entity.Property(e => e.PurchaseId).HasComment("購買紀錄 ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("建立時間");
            entity.Property(e => e.IsPaid).HasComment("是否購買（0=未購買，1=已購買）");
            entity.Property(e => e.PageId).HasComment("文章/課程 ID");
            entity.Property(e => e.RevisedDate).HasComment("異動時間");
            entity.Property(e => e.UnitPrice)
                .HasComment("單價")
                .HasColumnType("decimal(20, 2)");
            entity.Property(e => e.UserNumberId).HasComment("會員 ID");

            entity.HasOne(d => d.Page).WithMany(p => p.CntPurchases)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CNT_Purchase_Page");
        });

        modelBuilder.Entity<CntSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__CNT_Sche__9C8A5B49A0DA5C8B");

            entity.ToTable("CNT_Schedule", tb => tb.HasComment("頁面排程"));

            entity.HasIndex(e => e.PageId, "IX_CNT_Schedule_PageId");

            entity.HasIndex(e => e.ScheduledDate, "IX_CNT_Schedule_ScheduledDate");

            entity.HasIndex(e => e.Status, "IX_CNT_Schedule_Status");

            entity.Property(e => e.ScheduleId).HasComment("排程 ID");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .HasComment("排程動作");
            entity.Property(e => e.PageId).HasComment("頁面 ID");
            entity.Property(e => e.ScheduledDate).HasComment("排程時間");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasComment("排程狀態");

            entity.HasOne(d => d.Page).WithMany(p => p.CntSchedules)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CNT_Schedule_PageId");
        });

        modelBuilder.Entity<CntShareClick>(entity =>
        {
            entity.HasKey(e => e.ShareClickId).HasName("PK__CNT_Shar__46D82BFCE99A3789");

            entity.ToTable("CNT_ShareClick", tb => tb.HasComment("分享點擊紀錄"));

            entity.HasIndex(e => e.CreatedDate, "IX_CNT_ShareClick_CreatedDate");

            entity.HasIndex(e => e.PageId, "IX_CNT_ShareClick_PageId");

            entity.HasIndex(e => e.VisitorToken, "IX_CNT_ShareClick_VisitorToken");

            entity.Property(e => e.ShareClickId).HasComment("分享點擊 ID");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("點擊時間");
            entity.Property(e => e.Ipaddress)
                .HasMaxLength(50)
                .HasComment("IP 紀錄")
                .HasColumnName("IPAddress");
            entity.Property(e => e.PageId).HasComment("文章 ID");
            entity.Property(e => e.UserNumberId).HasComment("會員 ID");
            entity.Property(e => e.VisitorToken)
                .HasMaxLength(255)
                .HasComment("訪客識別");

            entity.HasOne(d => d.Page).WithMany(p => p.CntShareClicks)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CNT_ShareClick_PageId");
        });

        modelBuilder.Entity<CntTag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("PK__CNT_Tag__657CF9AC3110F874");

            entity.ToTable("CNT_Tag", tb => tb.HasComment("標籤"));

            entity.HasIndex(e => e.TagName, "UQ_CNT_Tag_TagName").IsUnique();

            entity.Property(e => e.TagId).HasComment("標籤 ID");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasComment("是否啟用（0=否，1=是）");
            entity.Property(e => e.RevisedDate)
                .HasDefaultValueSql("(sysdatetime())")
                .HasComment("異動時間");
            entity.Property(e => e.Revisor)
                .HasMaxLength(50)
                .HasComment("異動人員");
            entity.Property(e => e.TagName)
                .HasMaxLength(255)
                .HasComment("標籤名稱");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
