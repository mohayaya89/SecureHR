using Microsoft.EntityFrameworkCore;
using SecureHR.Domain.Entities;
using SecureHR.Domain.Enums;

namespace SecureHR.Infrastructure.Data
{
    public class SecureHRDbContext(DbContextOptions<SecureHRDbContext> options) : DbContext(options)
    {
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<PayrollRecord> PayrollRecords => Set<PayrollRecord>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Department Configuration
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasMany(d => d.Employees)
                    .WithOne(e => e.Department)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.Name)
                    .IsUnique();

                entity.ToTable("Department");
            });

            // Employee Configuration
            modelBuilder.Entity<Employee>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.JobTitle)
                    .IsRequired()
                    .HasMaxLength(100);

                // Derived from Id for display (EMP-0001); not a column
                entity.Ignore(e => e.EmployeeNumber);

                entity.Property(e => e.AnnualSalary)
                    .HasPrecision(18, 2);

                entity.Property(e => e.IsActive)
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
                entity.HasMany(e => e.PayrollRecords)
                    .WithOne(p => p.Employee)
                    .HasForeignKey(p => p.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.HasIndex(e => e.DepartmentId);

                entity.HasIndex(e => e.IsActive);

                entity.ToTable("Employee");
            });

            // PayrollRecord Configuration
            modelBuilder.Entity<PayrollRecord>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.GrossSalary)
                    .HasPrecision(18, 2);

                entity.Property(e => e.NetSalary)
                    .HasPrecision(18, 2);

                entity.Property(e => e.ProcessedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(p => p.Employee)
                    .WithMany(e => e.PayrollRecords)
                    .HasForeignKey(p => p.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(p => p.EmployeeId);

                entity.HasIndex(p => new { p.PeriodStart, p.PeriodEnd });

                // An employee can be paid at most once per period
                entity.HasIndex(p => new { p.EmployeeId, p.PeriodStart, p.PeriodEnd })
                    .IsUnique();

                entity.ToTable("PayrollRecord");
            });

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.PasswordHash)
                    .IsRequired()
                    .HasMaxLength(1024);

                entity.Property(e => e.Role)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.HasIndex(e => e.Username)
                    .IsUnique();

                entity.ToTable("User");
            });

            // AuditLog Configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UserId)
                    .HasMaxLength(50);

                entity.Property(e => e.Username)
                    .HasMaxLength(255);

                entity.Property(e => e.EntityName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.Action)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.ChangesJson)
                    .HasMaxLength(4000);

                entity.Property(e => e.Timestamp)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.UserId);

                entity.HasIndex(e => e.EntityName);

                entity.HasIndex(e => e.Timestamp);

                entity.ToTable("AuditLog");
            });
        }
    }
}

