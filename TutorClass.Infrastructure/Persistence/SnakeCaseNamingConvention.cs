using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace TutorClass.Infrastructure.Persistence;

internal static class SnakeCaseNamingConvention
{
    public static void ApplySnakeCaseNamingConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ApplyTableName(entityType);
            ApplyColumnNames(entityType);
            ApplyConstraintNames(entityType);
        }
    }

    private static void ApplyTableName(IMutableEntityType entityType)
    {
        var tableName = entityType.GetTableName();
        if (!string.IsNullOrWhiteSpace(tableName))
        {
            entityType.SetTableName(ToSnakeCase(tableName));
        }
    }

    private static void ApplyColumnNames(IMutableEntityType entityType)
    {
        var tableName = entityType.GetTableName();
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return;
        }

        var storeObject = StoreObjectIdentifier.Table(tableName, entityType.GetSchema());
        foreach (var property in entityType.GetProperties())
        {
            var columnName = property.GetColumnName(storeObject);
            if (!string.IsNullOrWhiteSpace(columnName))
            {
                property.SetColumnName(ToSnakeCase(columnName));
            }
        }
    }

    private static void ApplyConstraintNames(IMutableEntityType entityType)
    {
        foreach (var key in entityType.GetKeys())
        {
            var keyName = key.GetName();
            if (!string.IsNullOrWhiteSpace(keyName))
            {
                key.SetName(ToSnakeCase(keyName));
            }
        }

        foreach (var foreignKey in entityType.GetForeignKeys())
        {
            var constraintName = foreignKey.GetConstraintName();
            if (!string.IsNullOrWhiteSpace(constraintName))
            {
                foreignKey.SetConstraintName(ToSnakeCase(constraintName));
            }
        }

        foreach (var index in entityType.GetIndexes())
        {
            var indexName = index.GetDatabaseName();
            if (!string.IsNullOrWhiteSpace(indexName))
            {
                index.SetDatabaseName(ToSnakeCase(indexName));
            }
        }
    }

    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (current == '_')
            {
                builder.Append(current);
                continue;
            }

            if (char.IsUpper(current) && ShouldAddSeparator(value, index))
            {
                builder.Append('_');
            }

            builder.Append(char.ToLower(current, CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static bool ShouldAddSeparator(string value, int index)
    {
        if (index == 0)
        {
            return false;
        }

        var previous = value[index - 1];
        if (previous == '_')
        {
            return false;
        }

        if (char.IsLower(previous) || char.IsDigit(previous))
        {
            return true;
        }

        return index + 1 < value.Length && char.IsLower(value[index + 1]);
    }
}
