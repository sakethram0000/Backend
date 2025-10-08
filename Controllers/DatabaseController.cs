using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApi.Data;
using System.Data.Common;
using System.Collections.Generic;

namespace MyWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly AppDbContext _context;

    public DatabaseController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Check database status and record counts
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult> GetDatabaseStatus()
    {
        try
        {
            var status = new
            {
                DatabaseConnected = true,
                Tables = new
                {
                    Users = await _context.Users.CountAsync(),
                    Rules = await _context.Rules.CountAsync(),
                    Products = await _context.Products.CountAsync(),
                    Carriers = await _context.Carriers.CountAsync(),
                    Events = await _context.Events.CountAsync(),
                    Submissions = await _context.Submissions.CountAsync()
                },
                LastChecked = DateTime.UtcNow
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                DatabaseConnected = false,
                Error = ex.Message,
                LastChecked = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Diagnostic: return column names and types for a given table (Postgres information_schema compatible).
    /// Call like: GET /api/database/columns?table=users
    /// This is read-only and intended to help debug type mismatches (e.g. text vs timestamp).
    /// </summary>
    [HttpGet("columns")]
    public async Task<ActionResult> GetTableColumns([FromQuery] string table)
    {
        if (string.IsNullOrWhiteSpace(table))
            return BadRequest(new { error = "table parameter is required" });

        try
        {
            var conn = _context.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();

            // Use information_schema for Postgres; works on most SQL DBs with that view
            cmd.CommandText = @"SELECT column_name, data_type, udt_name
                FROM information_schema.columns
                WHERE table_name = @table
                ORDER BY ordinal_position";

            var param = cmd.CreateParameter();
            param.ParameterName = "@table";
            param.Value = table;
            cmd.Parameters.Add(param);

            var result = new List<object>();
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var col = reader.IsDBNull(0) ? null : reader.GetString(0);
                var dtype = reader.IsDBNull(1) ? null : reader.GetString(1);
                var udt = reader.IsDBNull(2) ? null : reader.GetString(2);
                result.Add(new { column = col, data_type = dtype, udt_name = udt });
            }

            return Ok(new { table = table, columns = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Add sample data to database
    /// </summary>
    [HttpPost("seed")]
    public async Task<ActionResult> SeedDatabase()
    {
        try
        {
            // Add sample rules if none exist
            if (!await _context.Rules.AnyAsync())
            {
                var sampleRules = new[]
                {
                    new Models.DbRule
                    {
                        RuleId = "rule-001",
                        Title = "Health Insurance Basic Rule",
                        Product = "Health Insurance",
                        NaicsCodes = "524114",
                        States = "CA;NY;TX",
                        Status = "Active",
                        Outcome = "Eligible",
                        Priority = "High",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    },
                    new Models.DbRule
                    {
                        RuleId = "rule-002", 
                        Title = "Motor Insurance Standard Rule",
                        Product = "Motor Insurance",
                        NaicsCodes = "524126",
                        States = "FL;TX;CA",
                        Status = "Active",
                        Outcome = "Eligible",
                        Priority = "Medium",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    },
                    new Models.DbRule
                    {
                        RuleId = "rule-003",
                        Title = "Commercial Insurance Rule",
                        Product = "Commercial Insurance", 
                        NaicsCodes = "524130",
                        States = "NY;NJ;CT",
                        Status = "Active",
                        Outcome = "Restricted",
                        Priority = "Low",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "System"
                    }
                };

                _context.Rules.AddRange(sampleRules);
            }

            // Add sample products if none exist
            if (!await _context.Products.AnyAsync())
            {
                var sampleProducts = new[]
                {
                    new Models.DbProduct
                    {
                        Id = "prod-001",
                        Name = "Health Insurance Premium",
                        Carrier = "ABC Health Corp",
                        PerOccurrence = 1000000,
                        Aggregate = 2000000,
                        MinAnnualRevenue = 0,
                        MaxAnnualRevenue = 5000000,
                        NaicsAllowed = "524114;621111",
                        CreatedAt = DateTime.UtcNow
                    },
                    new Models.DbProduct
                    {
                        Id = "prod-002",
                        Name = "Motor Insurance Standard",
                        Carrier = "XYZ Auto Insurance",
                        PerOccurrence = 500000,
                        Aggregate = 1000000,
                        MinAnnualRevenue = 0,
                        MaxAnnualRevenue = 2000000,
                        NaicsAllowed = "524126;441110",
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.Products.AddRange(sampleProducts);
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Database seeded successfully",
                SeedTime = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                Error = ex.Message,
                Message = "Failed to seed database"
            });
        }
    }
}