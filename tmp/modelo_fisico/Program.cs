using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using TutorClass.Infrastructure.Persistence;
using TutorClass.Infrastructure.Chat;

using var db = new TutorClassDbContext(new DbContextOptionsBuilder<TutorClassDbContext>().UseNpgsql("Host=localhost;Database=metadata_only").Options);
var model = db.GetService<IDesignTimeModel>().Model;
var relational = model.GetRelationalModel();
var tables = relational.Tables.Select(t => new {
    name=t.Name, schema=t.Schema ?? "public",
    columns=t.Columns.Select(c => new {name=c.Name,type=c.StoreType,nullable=c.IsNullable,defaultSql=c.DefaultValueSql,defaultValue=c.DefaultValue,computed=c.ComputedColumnSql, properties=c.PropertyMappings.Select(p=>p.Property.DeclaringType.Name+"."+p.Property.Name).ToArray()}).ToArray(),
    primaryKey=new {name=t.PrimaryKey!.Name,columns=t.PrimaryKey.Columns.Select(c=>c.Name).ToArray()},
    foreignKeys=t.ForeignKeyConstraints.Select(f=>new {name=f.Name,columns=f.Columns.Select(c=>c.Name).ToArray(),principalTable=f.PrincipalTable.Name,principalColumns=f.PrincipalColumns.Select(c=>c.Name).ToArray(),onDelete=f.OnDeleteAction.ToString()}).ToArray(),
    indexes=t.Indexes.Select(i=>new {name=i.Name,columns=i.Columns.Select(c=>c.Name).ToArray(),unique=i.IsUnique,filter=i.Filter,descending=i.IsDescending}).ToArray(),
    checks=t.CheckConstraints.Select(c=>new {name=c.Name,sql=c.Sql}).ToArray(),
    uniqueConstraints=t.UniqueConstraints.Where(c=>c!=t.PrimaryKey).Select(c=>new {name=c.Name,columns=c.Columns.Select(x=>x.Name).ToArray()}).ToArray()
}).ToArray();
var mongo=new List<object>();
foreach(var name in new[]{"ChatMentoriaDocument","MensagemMentoriaDocument"}) {
    var type=typeof(MongoMentoriaChatStore).GetNestedType(name,System.Reflection.BindingFlags.NonPublic)!;
    var obj=Activator.CreateInstance(type)!;
    foreach(var p in type.GetProperties()) {
        if(p.PropertyType==typeof(string))p.SetValue(obj,p.Name=="Id" ? "0123456789abcdef01234567" : "exemplo");
        else if(p.PropertyType==typeof(Guid))p.SetValue(obj,Guid.Empty);
        else if(p.PropertyType==typeof(DateTime))p.SetValue(obj,DateTime.SpecifyKind(new DateTime(2026,9,16),DateTimeKind.Utc));
    }
    var bson=obj.ToBsonDocument(type);
    mongo.Add(new {name,fields=bson.Elements.Select(e=>new {name=e.Name,type=e.Value.BsonType.ToString(),subtype=e.Value.IsBsonBinaryData?e.Value.AsBsonBinaryData.SubType.ToString():null}).ToArray()});
}
var result=new {tables,mongo,pendingModelChanges=db.Database.HasPendingModelChanges(),migrations=db.Database.GetMigrations().ToArray()};
File.WriteAllText("output/modelo_fisico/schema_verificado.json",JsonSerializer.Serialize(result,new JsonSerializerOptions {WriteIndented=true}));
Console.WriteLine($"{tables.Length} tabelas; {tables.Sum(t=>t.columns.Length)} colunas; {tables.Sum(t=>t.foreignKeys.Length)} FKs; {tables.Sum(t=>t.indexes.Length)} índices. Mongo: {JsonSerializer.Serialize(mongo)}");
