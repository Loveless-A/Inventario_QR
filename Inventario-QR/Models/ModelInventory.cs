using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Inventario_QR.Models
{
    // ==========================================
    // NUEVAS TABLAS MAESTRAS
    // ==========================================

    [Table("positions")]
    public class Position
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("position")]
        public string PositionName { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;
    }

    [Table("dependences")]
    public class Dependence
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("dependence")]
        public string DependenceName { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;
    }

    [Table("categories")]
    public class Category
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("category")]
        public string CategoryName { get; set; }

        [Column("abbreviation")]
        public string Abbreviation { get; set; }

        [Column("comment")]
        public string Comment { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;
    }

    // ==========================================
    // ENTIDADES PRINCIPALES Y SUS RELACIONES
    // ==========================================

    [Table("persons")]
    public class Person
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        [Column("identification")]
        public string Identification { get; set; }

        [Column("numbers")]
        public string Numbers { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        // --- Relación con Cargo (Position) ---
        [Column("position_id")]
        public int? PositionId { get; set; }

        [ForeignKey("PositionId")]
        public Position Position { get; set; }

        // --- Relación con Área/Dependencia (Dependence) ---
        [Column("dependence_id")]
        public int? DependenceId { get; set; }

        [ForeignKey("DependenceId")]
        public Dependence Dependence { get; set; }
    }

    [Table("access")]
    public class Access
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user")]
        public string User { get; set; }

        [Column("password")]
        public string Password { get; set; }

        [Column("active")]
        public bool Active { get; set; }

        [Column("person_id")]
        public int PersonId { get; set; }

        [ForeignKey("PersonId")]
        public Person Person { get; set; }
    }

    [Table("product")]
    [Index(nameof(Code), IsUnique = true)]
    [Index(nameof(Qr), IsUnique = true)]
    public class Product
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("code")]
        public string Code { get; set; }

        [Column("product")]
        public string ProductName { get; set; }

        [Column("qr")]
        public string Qr { get; set; }

        [Column("active")]
        public bool Active { get; set; } = true;

        // --- Relación con Categoría (Category) ---
        [Column("category_id")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
    }

    [Table("characteristics")]
    public class Characteristic
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("person_id")]
        public int PersonId { get; set; }

        [ForeignKey("PersonId")]
        public Person Person { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Column("localization")]
        public string Localization { get; set; }

        [Column("date")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Column("active")]
        public bool Active { get; set; } = true;

        [Column("coment")]
        public string Coment { get; set; }
    }

    [Table("details")]
    public class Detail
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("product")]
        public string DetailName { get; set; }

        [Column("complement")]
        public bool Complement { get; set; }
    }

    [Table("product_details")]
    public class ProductDetail
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [Column("details_id")]
        public int DetailsId { get; set; }

        [ForeignKey("DetailsId")]
        public Detail Detail { get; set; }

        [Column("details")]
        public string DetailsValue { get; set; }

        [Column("amount")]
        public int Amount { get; set; } = 1;
    }
}